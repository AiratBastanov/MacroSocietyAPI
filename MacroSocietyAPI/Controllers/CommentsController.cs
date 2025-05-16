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
using System.Globalization;

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

        [HttpGet("post/{encryptedPostId}")]
        public async Task<IActionResult> GetCommentsForPost(string encryptedPostId)
        {
            try
            {
                if (!IdHelper.TryDecryptId(encryptedPostId, out int postId, out string error))
                {
                    Console.WriteLine($"Ошибка при получении комментариев: {error}");
                    return BadRequest(new { error = "Неверный формат postId" });
                }

                var comments = await _context.Comments
                    .Include(c => c.User)
                    .Where(c => c.PostId == postId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                var result = comments.Select(c => new
                {
                    id = AesEncryptionService.Encrypt(c.Id.ToString()),
                    postId = AesEncryptionService.Encrypt(c.PostId.ToString()),
                    userId = AesEncryptionService.Encrypt(c.UserId.ToString()),
                    content = c.Content,
                    createdAt = c.CreatedAt?.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                    username = c.User?.Name
                });

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                // Логирование, например:
                Console.WriteLine($"Ошибка при получении комментариев: {ex.Message}");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto dto)
        {
            try
            {
                if (!IdHelper.TryDecryptId(dto.PostId, out int postId, out string postError))
                {
                    return BadRequest(new { error = "Неверный формат postId или userId" });
                }

                var comment = new Comment
                {
                    PostId = postId,
                    UserId = dto.UserId,
                    Content = dto.Content,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    commentId = comment.Id,
                    createdAt = comment.CreatedAt?.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'") // ISO 8601
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании комментария: {ex.Message}");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }


        /*[HttpPost("add")]
        public async Task<IActionResult> AddComment([FromBody] Comment comment)
        {
            Console.WriteLine($"Ошибка при получении комментариев: {comment.PostId}");
            Console.WriteLine($"Ошибка при получении комментариев:");
            Console.WriteLine($"Ошибка при получении комментариев: {comment.UserId}");
            Console.WriteLine($"Ошибка при получении комментариев: {comment.Content}");

            if (comment == null || comment.UserId <= 0 || comment.PostId <= 0 || string.IsNullOrWhiteSpace(comment.Content))
                return BadRequest("Некорректные данные комментария");

            comment.CreatedAt = DateTime.UtcNow;

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var responseDto = new CommentCreatedResponseDto
            {
                Id = comment.Id,
                CreatedAt = comment.CreatedAt
            };

            return Ok(comment);
        }*/
    }
}
