using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MacroSocietyAPI.Models;
using MacroSocietyAPI.EmailServies;
using MacroSocietyAPI.Randoms;
using MacroSocietyAPI.Encryption;
using System.Text.Json;
using MacroSocietyAPI.ExtensionMethod;

namespace MacroSocietyAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly MacroSocietyDbContext _context;
        private readonly EmailService _emailService;
        private readonly CreateVerificationCode _createVerificationCode;

        public UsersController(MacroSocietyDbContext context, EmailService emailService, CreateVerificationCode createVerificationCode)
        {
            _context = context;
            _emailService = emailService;
            _createVerificationCode = createVerificationCode;
        }

        [HttpGet("byid/{userIdEncrypted}")]
        public async Task<ActionResult<UserDto>> GetUserById(string userIdEncrypted)
        {
            if (!IdHelper.TryDecryptId(userIdEncrypted, out int id, out string error))
                return BadRequest(error ?? "Неверный ID");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        /*[HttpGet("byid/{userIdEncrypted}")]
        public async Task<ActionResult<UserDto>> GetUserById([ModelBinder(BinderType = typeof(DecryptedIdBinder))] DecryptedId userId)
        {
            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound();

            return Ok(user);
        }*/


        [HttpPost("register")]
        public async Task<ActionResult<User>> RegisterUser([FromBody] User user, [FromQuery] string code)
        {
            if (string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Name))
                return BadRequest("Email и имя обязательны");

            var existingUser = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == user.Email);
            if (existingUser != null)
                return Conflict("Пользователь с таким email уже существует");

            var emailCode = await _context.EmailLoginCodes.FirstOrDefaultAsync(c =>
                c.Email == user.Email &&
                c.Code == code &&
                c.IsUsed == false &&
                c.ExpiresAt > DateTime.UtcNow);

            if (emailCode == null)
                return Unauthorized("Неверный или просроченный код");

            emailCode.IsUsed = true;
            _context.EmailLoginCodes.Update(emailCode);

            user.CreatedAt ??= DateTime.UtcNow;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var encryptedId = AesEncryptionService.Encrypt(user.Id.ToString());
            return CreatedAtAction(nameof(GetUserById), new { idEncrypted = encryptedId }, user);
        }


        [HttpPost("checkemail")]
        public async Task<IActionResult> SendVerificationCode([FromQuery] string email, [FromQuery] string state)
        {
            string decryptedEmail;
            try
            {
                decryptedEmail = AesEncryptionService.Decrypt(email);
            }
            catch
            {
                return BadRequest("Некорректный формат email");
            }

            // Проверка, зарегистрирован ли уже такой email
            var exists = await _context.Users.AnyAsync(u => u.Email == decryptedEmail);
            if (exists && state == "register")
                return Conflict("Email уже зарегистрирован");

            // Удалим старые коды (опционально, можно использовать хранимку периодически)
            await _context.Database.ExecuteSqlRawAsync("EXEC DeleteExpiredLoginCodes");

            int code = _createVerificationCode.RandomInt(6);
            var entry = new EmailLoginCode
            {
                Email = decryptedEmail,
                Code = code.ToString(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            };

            _context.EmailLoginCodes.Add(entry);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(decryptedEmail, "Код подтверждения регистрации", $"Ваш код: {code}");

            return Ok("Код подтверждения отправлен на email");
        }


        [HttpPost("login")]
        public async Task<ActionResult<User>> LoginWithCode([FromQuery] string email, [FromQuery] string code)
        {
            string decryptedEmail;
            try
            {
                decryptedEmail = AesEncryptionService.Decrypt(email);
            }
            catch
            {
                return BadRequest("Некорректный формат email");
            }

            var loginCode = await _context.EmailLoginCodes
                .FirstOrDefaultAsync(c =>
                    c.Email == decryptedEmail &&
                    c.Code == code &&
                    c.IsUsed == false &&
                    c.ExpiresAt > DateTime.UtcNow);

            if (loginCode == null)
                return Unauthorized("Неверный код или срок действия истек");

            loginCode.IsUsed = true;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == decryptedEmail);
            if (user == null)
                return NotFound("Пользователь не найден");

            await _context.SaveChangesAsync();
            return Ok(user); // Возвращаем пользователя
        }

        [HttpGet("allusers")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers([FromQuery] string myIdEncrypted)
        {            
            if (!IdHelper.TryDecryptId(myIdEncrypted, out int myId, out string error))
                return BadRequest(error ?? "Неверный ID");

            var friendIds = await _context.FriendLists
                .Where(f => f.UserId == myId)
                .Select(f => f.FriendId)
                .ToListAsync();

            var query = _context.Users
                .Where(u => u.Id != myId && !friendIds.Contains(u.Id));

            var users = await query.ToListAsync();

            return Ok(users);
        }

        [HttpPut("{idEncrypted}")]
        public async Task<IActionResult> UpdateProfile(string idEncrypted, [FromBody] User updated)
        {
            if (!IdHelper.TryDecryptId(idEncrypted, out int id, out string error))
                return BadRequest(error ?? "Неверный ID");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.Name = updated.Name;
            user.Email = updated.Email;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("stats/{userIdEncrypted}")]
        public async Task<ActionResult<UserStats>> GetUserStats(string userIdEncrypted)
        {
            if (!IdHelper.TryDecryptId(userIdEncrypted, out int userId, out string error))
                return BadRequest(error ?? "Неверный ID");

            var friendsCount = await _context.FriendLists.CountAsync(f => f.UserId == userId);
            var postsCount = await _context.Posts.CountAsync(p => p.UserId == userId);
            var communitiesCount = await _context.Communities.CountAsync(cm => cm.CreatorId == userId);

            var stats = new UserStats
            {
                FriendsCount = friendsCount,
                PostsCount = postsCount,
                CommunitiesCount = communitiesCount
            };

            return Ok(stats);
        }

        [HttpDelete("{idEncrypted}")]
        public async Task<IActionResult> DeleteUser(string idEncrypted)
        {
            if (!IdHelper.TryDecryptId(idEncrypted, out int id, out string error))
                return BadRequest(error ?? "Неверный ID");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
