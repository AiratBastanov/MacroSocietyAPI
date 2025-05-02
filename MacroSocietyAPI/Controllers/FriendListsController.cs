using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MacroSocietyAPI.Models;
using MacroSocietyAPI.Encryption;
using MacroSocietyAPI.ExtensionMethod;

namespace MacroSocietyAPI.Controllers
{
    [ApiController]
    [Route("api/friends")]
    public class FriendListsController : Controller
    {
        private readonly MacroSocietyDbContext _context;

        public FriendListsController(MacroSocietyDbContext context)
        {
            _context = context;
        }

        [HttpGet("{userIdEncrypted}")]
        public async Task<ActionResult<IEnumerable<string>>> GetFriends(string userIdEncrypted)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(userIdEncrypted), out int userId))
                return BadRequest("Неверный формат ID");

            var friendIds = await _context.FriendLists
                .Where(f => f.UserId == userId)
                .Select(f => f.FriendId)
                .ToListAsync();

            return Ok(friendIds.Select(id => AesEncryptionService.Encrypt(id.ToString())));
        }

        [HttpGet("details/{userIdEncrypted}")]
        public async Task<IActionResult> GetFriendDetails(string userIdEncrypted)
        {
            if (!IdHelper.TryDecryptId(userIdEncrypted, out int userId, out string error))
                return BadRequest(error ?? "Неверный ID");

            var friends = await _context.FriendLists.GetFriendsWithDetailsAsync(userId);
            return Ok(friends);
        }

        [HttpGet("mutual")]
        public async Task<IActionResult> GetMutualFriends([FromQuery] string user1Id, [FromQuery] string user2Id)
        {
            List<string> errors = new();

            if (!IdHelper.TryDecryptId(user1Id, out int u1, out string error1))
                errors.Add($"Ошибка в user1Id: {error1 ?? "Неверный ID"}");

            if (!IdHelper.TryDecryptId(user2Id, out int u2, out string error2))
                errors.Add($"Ошибка в user2Id: {error2 ?? "Неверный ID"}");

            if (errors.Count > 0)
                return BadRequest(new { Errors = errors });

            var mutual = await _context.FriendLists.GetMutualFriendsAsync(u1, u2);
            return Ok(mutual);
        }

        /*[HttpGet("mutual")]
        public async Task<IActionResult> GetMutualFriends([FromQuery, ModelBinder(BinderType = typeof(DecryptedIdBinder))] DecryptedId user1Id,
                                                  [FromQuery, ModelBinder(BinderType = typeof(DecryptedIdBinder))] DecryptedId user2Id)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var mutual = await _context.FriendLists.GetMutualFriendsAsync(user1Id.Value, user2Id.Value);
            return Ok(mutual);
        }*/

        [HttpDelete]
        public async Task<IActionResult> RemoveFriend(string userIdEncrypted, string friendIdEncrypted)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(userIdEncrypted), out int userId) ||
                !int.TryParse(AesEncryptionService.Decrypt(friendIdEncrypted), out int friendId))
            {
                return BadRequest("Неверный формат ID");
            }

            var friendships = await _context.FriendLists
                .Where(f =>
                    (f.UserId == userId && f.FriendId == friendId) ||
                    (f.UserId == friendId && f.FriendId == userId))
                .ToListAsync();

            if (!friendships.Any())
                return NotFound("Дружба не найдена");

            _context.FriendLists.RemoveRange(friendships);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
