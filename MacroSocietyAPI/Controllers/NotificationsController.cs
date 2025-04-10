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
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly MacroSocietyDbContext _context;

        public NotificationsController(MacroSocietyDbContext context)
        {
            _context = context;
        }

        // Получить уведомления для пользователя
        [HttpGet("{encryptedUserId}")]
        public async Task<ActionResult<IEnumerable<string>>> GetNotifications(string encryptedUserId)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(encryptedUserId), out int userId))
                return BadRequest("Неверный формат userId");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var encrypted = notifications.Select(n =>
            {
                var json = JsonSerializer.Serialize(n);
                return AesEncryptionService.Encrypt(json);
            });

            return Ok(encrypted);
        }

        // Добавить уведомление
        [HttpPost("add")]
        public async Task<IActionResult> AddNotification([FromBody] string encryptedNotification)
        {
            string json;
            try
            {
                json = AesEncryptionService.Decrypt(encryptedNotification);
            }
            catch
            {
                return BadRequest("Ошибка расшифровки");
            }

            var notification = JsonSerializer.Deserialize<Notification>(json);
            if (notification == null)
                return BadRequest("Некорректные данные");

            notification.CreatedAt = DateTime.UtcNow;
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok("Уведомление добавлено");
        }
    }
}
