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
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private readonly MacroSocietyDbContext _context;

        public PostsController(MacroSocietyDbContext context)
        {
            _context = context;
        }

        // Получить посты сообщества по id сообщества
        [HttpGet("community/{encryptedCommunityId}")]
        public async Task<IActionResult> GetPosts(string encryptedCommunityId)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(encryptedCommunityId), out int communityId))
                return BadRequest(new { error = "Неверный формат communityId" });

            // Получаем посты, сортируя их по убыванию времени создания
            var posts = await _context.Posts
                .Where(p => p.CommunityId == communityId)
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt) // Сортировка по времени создания
                .ToListAsync();

            var result = posts.Select(p => new
            {
                id = AesEncryptionService.Encrypt(p.Id.ToString()),
                userId = AesEncryptionService.Encrypt(p.UserId.ToString()),
                communityId = AesEncryptionService.Encrypt((p.CommunityId ?? 0).ToString()),
                content = p.Content,
                createdAt = p.CreatedAt?.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                username = p.User?.Name
            });

            return new JsonResult(result);
        }

        // Получить посты сообщества по id пользователя
        [HttpGet("user/{userIdEncrypted}")]
        public async Task<IActionResult> GetUserPosts(string userIdEncrypted)
        {
            if (!IdHelper.TryDecryptId(userIdEncrypted, out int userId, out string error))
                return BadRequest(error ?? "Неверный ID");

            var posts = await _context.Posts.GetPostsByUserAsync(userId);

            var result = posts.Select(p => new
            {
                id = AesEncryptionService.Encrypt(p.Id.ToString()),
                content = p.Content,
                createdAt = p.CreatedAt?.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                username = p.User?.Name
            });

            return new JsonResult(result);
        }

        // Добавить пост
        [HttpPost("add")]
        public async Task<IActionResult> AddPost([FromBody] PostDto postDto)
        {
            if (postDto == null)
                return BadRequest("Пустой объект");

            int userId, communityId;

            try
            {
                userId = int.Parse(AesEncryptionService.Decrypt(postDto.UserId));
                communityId = int.Parse(AesEncryptionService.Decrypt(postDto.CommunityId));
            }
            catch
            {
                return BadRequest("Ошибка расшифровки ID");
            }

            var utcNow = DateTime.UtcNow;

            var post = new Post
            {
                UserId = userId,
                CommunityId = communityId,
                Content = postDto.Content,
                CreatedAt = utcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var encryptedId = AesEncryptionService.Encrypt(post.Id.ToString());

            return Ok(new
            {
                postId = encryptedId,
                createdAt = utcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
            });
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
