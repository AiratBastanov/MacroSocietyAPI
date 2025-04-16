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
            if (!IdHelper.TryDecryptId(userIdEncrypted, out int userId))
                return BadRequest("Неверный ID");

            var friends = await _context.FriendLists.GetFriendsWithDetailsAsync(userId);
            return Ok(friends);
        }

        [HttpGet("mutual")]
        public async Task<IActionResult> GetMutualFriends([FromQuery] string user1Id, [FromQuery] string user2Id)
        {
            if (!IdHelper.TryDecryptId(user1Id, out int u1) || !IdHelper.TryDecryptId(user2Id, out int u2))
                return BadRequest("Неверные ID");

            var mutual = await _context.FriendLists.GetMutualFriendsAsync(u1, u2);
            return Ok(mutual);
        }

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
