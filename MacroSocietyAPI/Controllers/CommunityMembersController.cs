using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MacroSocietyAPI.Models;
using MacroSocietyAPI.Encryption;
using System.Text.Json;
using MacroSocietyAPI.ExtensionMethod;

namespace MacroSocietyAPI.Controllers
{
    [ApiController]
    [Route("api/communitymember")]
    public class CommunityMembersController : Controller
    {
        private readonly MacroSocietyDbContext _context;

        public CommunityMembersController(MacroSocietyDbContext context)
        {
            _context = context;
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinCommunity([FromQuery] string encryptedUserId, [FromQuery] string encryptedCommunityId)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(encryptedUserId), out int userId) ||
                !int.TryParse(AesEncryptionService.Decrypt(encryptedCommunityId), out int communityId))
            {
                return BadRequest("Неверный формат идентификаторов");
            }

            if (await _context.CommunityMembers.AnyAsync(cm => cm.UserId == userId && cm.CommunityId == communityId))
            {
                return BadRequest("Вы уже участник сообщества");
            }

            var member = new CommunityMember
            {
                UserId = userId,
                CommunityId = communityId,
            };

            _context.CommunityMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok("Успешно присоединились");
        }

        [HttpGet("by-community/{communityId}")]
        public async Task<ActionResult<IEnumerable<string>>> GetMembersByCommunity(int communityId)
        {
            var members = await _context.CommunityMembers
                .Include(cm => cm.User)
                .Where(cm => cm.CommunityId == communityId)
                .ToListAsync();

            var encryptedUsers = members
                .Select(cm => AesEncryptionService.Encrypt(JsonSerializer.Serialize(cm.User)))
                .ToList();

            return Ok(encryptedUsers);
        }

        [HttpDelete("leave")]
        public async Task<IActionResult> LeaveCommunity([FromQuery] string encryptedUserId, [FromQuery] string encryptedCommunityId)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(encryptedUserId), out int userId) ||
                !int.TryParse(AesEncryptionService.Decrypt(encryptedCommunityId), out int communityId))
            {
                return BadRequest("Неверный формат идентификаторов");
            }

            var success = await _context.CommunityMembers.LeaveCommunityAsync(_context, userId, communityId);
            return success ? Ok("Успешно отписались") : NotFound("Участие в сообществе не найдено");
        }

        [HttpGet("user/{userIdEncrypted}")]
        public async Task<IActionResult> GetUserMemberships(string userIdEncrypted)
        {
            if (!IdHelper.TryDecryptId(userIdEncrypted, out int userId, out string error))
                return BadRequest(error ?? "Неверный ID");

            var memberships = await _context.CommunityMembers.GetUserMembershipsAsync(userId);
            return Ok(memberships);
        }

        public class LeaveCommunityRequest
        {
            public int userId { get; set; }
            public int communityId { get; set; }
        }
    }
}
