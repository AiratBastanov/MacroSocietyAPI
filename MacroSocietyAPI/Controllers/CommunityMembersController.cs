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
        public async Task<IActionResult> JoinCommunity([FromBody] string encryptedData)
        {
            string json;
            try
            {
                json = AesEncryptionService.Decrypt(encryptedData);
            }
            catch
            {
                return BadRequest("Ошибка расшифровки");
            }

            var member = JsonSerializer.Deserialize<CommunityMember>(json);
            if (member == null || member.UserId <= 0 || member.CommunityId <= 0)
                return BadRequest("Некорректные данные");

            if (await _context.CommunityMembers.AnyAsync(cm => cm.UserId == member.UserId && cm.CommunityId == member.CommunityId))
                return BadRequest("Вы уже участник сообщества");

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
        public async Task<IActionResult> LeaveCommunity([FromBody] LeaveCommunityRequest request)
        {
            if (request == null || request.UserId <= 0 || request.CommunityId <= 0)
                return BadRequest("Некорректные данные");

            var success = await _context.CommunityMembers.LeaveCommunityAsync(request.UserId, request.CommunityId);
            return success ? Ok() : NotFound("Участие в сообществе не найдено");
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
            public int UserId { get; set; }
            public int CommunityId { get; set; }
        }
    }
}
