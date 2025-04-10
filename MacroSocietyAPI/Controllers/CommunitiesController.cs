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
    [ApiController]
    [Route("api/communities")]
    public class CommunitiesController : Controller
    {
        private readonly MacroSocietyDbContext _context;

        public CommunitiesController(MacroSocietyDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetCommunity(int id)
        {
            var community = await _context.Communities.FindAsync(id);
            if (community == null)
                return NotFound();

            string json = JsonSerializer.Serialize(community);
            string encrypted = AesEncryptionService.Encrypt(json);
            return Ok(encrypted);
        }

        [HttpPost("create")]
        public async Task<ActionResult<string>> CreateCommunity([FromBody] string encryptedCommunity)
        {
            string json;
            try
            {
                json = AesEncryptionService.Decrypt(encryptedCommunity);
            }
            catch
            {
                return BadRequest("Ошибка расшифровки");
            }

            var community = JsonSerializer.Deserialize<Community>(json);
            if (community == null || string.IsNullOrEmpty(community.Name))
                return BadRequest("Некорректные данные сообщества");

            _context.Communities.Add(community);
            await _context.SaveChangesAsync();

            string result = JsonSerializer.Serialize(community);
            return Ok(AesEncryptionService.Encrypt(result));
        }
    }
}
