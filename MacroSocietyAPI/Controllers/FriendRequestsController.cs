using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MacroSocietyAPI.Models;
using MacroSocietyAPI.Encryption;
using MacroSocietyAPI.ExtensionMethod;

namespace MacroSocietyAPI.Controllers
{
    [ApiController]
    [Route("api/friendrequests")]
    public class FriendRequestsController : Controller
    {
        private readonly MacroSocietyDbContext _context;

        public FriendRequestsController(MacroSocietyDbContext context)
        {
            _context = context;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendRequest(string senderIdEncrypted, string receiverIdEncrypted)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(senderIdEncrypted), out int senderId) ||
                !int.TryParse(AesEncryptionService.Decrypt(receiverIdEncrypted), out int receiverId))
            {
                return BadRequest("Неверный формат ID");
            }

            if (await _context.FriendRequests.AnyAsync(r => r.SenderId == senderId && r.ReceiverId == receiverId))
                return BadRequest("Заявка уже отправлена");

            _context.FriendRequests.Add(new FriendRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                SentAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok("Заявка отправлена");
        }

        [HttpGet("incoming/{userIdEncrypted}")]
        public async Task<ActionResult<IEnumerable<object>>> GetIncoming(string userIdEncrypted)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(userIdEncrypted), out int userId))
                return BadRequest("Неверный формат ID");

            var requests = await _context.FriendRequests
                .Include(r => r.Sender)
                .Include(r => r.Receiver)
                .Where(r => r.ReceiverId == userId)
                .ToListAsync();

            var result = requests.Select(r => new
            {
                id = r.Id,
                senderId = AesEncryptionService.Encrypt(r.SenderId.ToString()),
                receiverId = AesEncryptionService.Encrypt(r.ReceiverId.ToString()),
                sentAt = r.SentAt,
                status = r.Status,
                sender = new
                {
                    id = AesEncryptionService.Encrypt(r.Sender.Id.ToString()),
                    name = r.Sender.Name,
                    email = r.Sender.Email
                },
                receiver = new
                {
                    id = AesEncryptionService.Encrypt(r.Receiver.Id.ToString()),
                    name = r.Receiver.Name,
                    email = r.Receiver.Email
                }
            });

            return Ok(result);
        }

        [HttpPost("accept")]
        public async Task<IActionResult> AcceptRequest(string senderIdEncrypted, string receiverIdEncrypted)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(senderIdEncrypted), out int senderId) ||
                !int.TryParse(AesEncryptionService.Decrypt(receiverIdEncrypted), out int receiverId))
            {
                return BadRequest("Неверный формат ID");
            }

            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.SenderId == senderId && r.ReceiverId == receiverId);

            if (request == null)
                return NotFound("Заявка не найдена");

            _context.FriendRequests.Remove(request);

            _context.FriendLists.AddRange(new[]
            {
            new FriendList { UserId = senderId, FriendId = receiverId, CreatedAt = DateTime.UtcNow },
            new FriendList { UserId = receiverId, FriendId = senderId, CreatedAt = DateTime.UtcNow }
        });

            await _context.SaveChangesAsync();
            return Ok("Заявка принята");
        }

        [HttpPost("reject")]
        public async Task<IActionResult> RejectRequest(string senderIdEncrypted, string receiverIdEncrypted)
        {
            if (!int.TryParse(AesEncryptionService.Decrypt(senderIdEncrypted), out int senderId) ||
                !int.TryParse(AesEncryptionService.Decrypt(receiverIdEncrypted), out int receiverId))
            {
                return BadRequest("Неверный формат ID");
            }

            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(r => r.SenderId == senderId && r.ReceiverId == receiverId);

            if (request == null)
                return NotFound("Заявка не найдена");

            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();

            return Ok("Заявка отклонена");
        }

        [HttpGet("outgoing/{userIdEncrypted}")]
        public async Task<IActionResult> GetOutgoingRequests(string userIdEncrypted)
        {
            if (!IdHelper.TryDecryptId(userIdEncrypted, out int userId))
                return BadRequest("Неверный ID");

            var requests = await _context.FriendRequests.GetOutgoingRequestsAsync(userId);
            return Ok(requests);
        }

        [HttpGet("details/incoming/{userIdEncrypted}")]
        public async Task<IActionResult> GetIncomingRequestDetails(string userIdEncrypted)
        {
            if (!IdHelper.TryDecryptId(userIdEncrypted, out int userId))
                return BadRequest("Неверный ID");

            var incoming = await _context.FriendRequests.GetIncomingRequestsWithDetailsAsync(userId);
            return Ok(incoming);
        }
    }
}
