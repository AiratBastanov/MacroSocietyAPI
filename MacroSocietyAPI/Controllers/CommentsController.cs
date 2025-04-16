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
    [Route("api/comments")]
    public class CommentsController : Controller
    {
        private readonly MacroSocietyDbContext _context;

        public CommentsController(MacroSocietyDbContext context)
        {
            _context = context;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddComment([FromBody] string encryptedComment)
        {
            string json;
            try
            {
                json = AesEncryptionService.Decrypt(encryptedComment);
            }
            catch
            {
                return BadRequest("Ошибка расшифровки");
            }

            var comment = JsonSerializer.Deserialize<Comment>(json);
            if (comment == null || comment.UserId <= 0 || comment.PostId <= 0 || string.IsNullOrWhiteSpace(comment.Content))
                return BadRequest("Некорректные данные комментария");

            comment.CreatedAt = DateTime.UtcNow;
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok("Комментарий добавлен");
        }

        [HttpGet("post/{postId}")]
        public async Task<ActionResult<IEnumerable<string>>> GetCommentsForPost(int postId)
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.PostId == postId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var encrypted = comments.Select(c => AesEncryptionService.Encrypt(JsonSerializer.Serialize(new
            {
                c.Id,
                c.Content,
                c.CreatedAt,
                UserName = c.User.Name
            })));

            return Ok(encrypted);
        }
    }
}
