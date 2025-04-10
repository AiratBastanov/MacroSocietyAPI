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
    [Route("api/messages")]
    public class MessagesController : ControllerBase
    {
        private readonly MacroSocietyDbContext _context;

        public MessagesController(MacroSocietyDbContext context)
        {
            _context = context;
        }

        // Получить сообщения между двумя пользователями
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetMessages(string encryptedUser1Id, string encryptedUser2Id)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(encryptedUser1Id), out int user1Id) ||
                !int.TryParse(AesEncryptionService.Decrypt(encryptedUser2Id), out int user2Id))
                return BadRequest("Неверный формат идентификаторов");

            var messages = await _context.Messages
                .Where(m =>
                    (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                    (m.SenderId == user2Id && m.ReceiverId == user1Id))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var encrypted = messages.Select(m =>
            {
                var json = JsonSerializer.Serialize(m);
                return AesEncryptionService.Encrypt(json);
            });

            return Ok(encrypted);
        }

        // Отправить сообщение
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] string encryptedMessage)
        {
            string json;
            try
            {
                json = AesEncryptionService.Decrypt(encryptedMessage);
            }
            catch
            {
                return BadRequest("Ошибка расшифровки");
            }

            var message = JsonSerializer.Deserialize<Message>(json);
            if (message == null)
                return BadRequest("Некорректные данные");

            message.SentAt = DateTime.UtcNow;

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok("Сообщение отправлено");
        }
    }
}
