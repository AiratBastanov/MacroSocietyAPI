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
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private readonly MacroSocietyDbContext _context;

        public PostsController(MacroSocietyDbContext context)
        {
            _context = context;
        }

        // Получить посты сообщества
        [HttpGet("community/{encryptedCommunityId}")]
        public async Task<ActionResult<IEnumerable<string>>> GetPosts(string encryptedCommunityId)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(encryptedCommunityId), out int communityId))
                return BadRequest("Неверный формат communityId");

            var posts = await _context.Posts
                .Where(p => p.CommunityId == communityId)
                .Include(p => p.User)
                .ToListAsync();

            var encryptedPosts = posts.Select(p =>
            {
                var json = JsonSerializer.Serialize(p);
                return AesEncryptionService.Encrypt(json);
            });

            return Ok(encryptedPosts);
        }

        // Добавить пост
        [HttpPost("add")]
        public async Task<IActionResult> AddPost([FromBody] string encryptedPost)
        {
            string json;
            try
            {
                json = AesEncryptionService.Decrypt(encryptedPost);
            }
            catch
            {
                return BadRequest("Ошибка расшифровки");
            }

            var post = JsonSerializer.Deserialize<Post>(json);
            if (post == null)
                return BadRequest("Некорректные данные поста");

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return Ok("Пост добавлен");
        }

        [HttpDelete("{encryptedPostId}")]
        public async Task<IActionResult> DeletePost(string encryptedPostId)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(encryptedPostId), out int postId))
                return BadRequest("Неверный формат postId");

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
                return NotFound("Пост не найден");

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok("Пост и связанные комментарии удалены");
        }

    }
}
