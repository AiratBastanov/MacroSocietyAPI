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

namespace MacroSocietyAPI.Controllers
{
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
    }
}
