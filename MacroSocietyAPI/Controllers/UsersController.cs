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

namespace MacroSocietyAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : Controller
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

        // Получить пользователя по Id (зашифрованному)
        [HttpGet("{idEncrypted}")]
        public async Task<ActionResult<string>> GetUserById(string idEncrypted)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(idEncrypted), out int id))
                return BadRequest("Неверный формат ID");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            string json = JsonSerializer.Serialize(user);
            string encrypted = AesEncryptionService.Encrypt(json);
            return Ok(encrypted);
        }

        // Получить пользователя по имени
        [HttpGet]
        public async Task<ActionResult<User>> GetUserByLogin(string name)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Name == name);
            if (user == null)
                return NotFound("Пользователь не найден");

            return Ok(user);
        }

        // Регистрация пользователя
        [HttpPost("register")]
        public async Task<ActionResult<string>> RegisterUser([FromBody] string encryptedUserData)
        {
            string json;
            try
            {
                json = AesEncryptionService.Decrypt(encryptedUserData);
            }
            catch
            {
                return BadRequest("Ошибка расшифровки");
            }

            var user = JsonSerializer.Deserialize<User>(json);

            if (user == null)
                return BadRequest("Некорректные данные пользователя");

            if (string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Name))
                return BadRequest("Email и имя обязательны");

            if (await _context.Users.AnyAsync(u => u.Email == user.Email || u.Name == user.Name))
                return BadRequest(AesEncryptionService.Encrypt("Пользователь уже существует"));

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            string resultJson = JsonSerializer.Serialize(user);
            string encrypted = AesEncryptionService.Encrypt(resultJson);
            return Ok(encrypted);
        }

        // Отправить код подтверждения на email
        [HttpPost("checkemail")]
        public async Task<ActionResult> SendVerificationCode(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("Пользователь с таким email не найден");

            int verificationCode = _createVerificationCode.RandomInt(6);
            var codeEntry = new EmailLoginCode
            {
                Email = email,
                Code = verificationCode.ToString(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            };

            _context.EmailLoginCodes.Add(codeEntry);
            await _context.SaveChangesAsync();

            string bodyMessage = $"Проверочный код: {verificationCode}";
            await _emailService.SendEmailAsync(email, "Подтверждение входа", bodyMessage);

            return Ok("Код отправлен");
        }

        // Вход с кодом
        [HttpPost("login")]
        public async Task<ActionResult> LoginWithCode(string email, string code)
        {
            var loginCode = await _context.EmailLoginCodes
                .Where(c => c.Email == email && c.Code == code && c.IsUsed == false && c.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (loginCode == null)
                return Unauthorized("Неверный код или срок его действия истек");

            loginCode.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok("Вход успешен");
        }

        // Получить список пользователей (не друзья, не я)
        [HttpGet("allusers")]
        public async Task<IEnumerable<User>> GetUsers(string myIdEncrypted, string person = "", int chunkIndex = 1, int chunkSize = 10)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(myIdEncrypted), out int myId))
                return new List<User>();

            // Список ID друзей пользователя
            var friendIds = await _context.FriendLists
                .Where(f => f.UserId == myId)
                .Select(f => f.FriendId)
                .ToListAsync();

            // Исключаем самого себя и друзей
            var query = _context.Users
                .Where(u => u.Id != myId && !friendIds.Contains(u.Id));

            if (!string.IsNullOrEmpty(person))
            {
                query = query.Where(u => u.Name.Contains(person));
            }

            var result = await query
                .Skip((chunkIndex - 1) * chunkSize)
                .Take(chunkSize)
                .ToListAsync();

            return result;
        }

        // Обновить информацию о пользователе
        [HttpPut("{idEncrypted}")]
        public async Task<IActionResult> UpdateProfile(string idEncrypted, User updatedUser)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(idEncrypted), out int id))
                return BadRequest("Неверный формат ID");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.Name = updatedUser.Name;
            user.Email = updatedUser.Email;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Удалить пользователя
        [HttpDelete("{idEncrypted}")]
        public async Task<IActionResult> DeleteUser(string idEncrypted)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(idEncrypted), out int id))
                return BadRequest("Неверный формат ID");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
