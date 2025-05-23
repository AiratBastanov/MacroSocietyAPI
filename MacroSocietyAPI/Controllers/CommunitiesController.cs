﻿using System;
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
    [Route("api/communities")]
    public class CommunitiesController : Controller
    {
        private readonly MacroSocietyDbContext _context;

        public CommunitiesController(MacroSocietyDbContext context)
        {
            _context = context;
        }

        [HttpGet("{communityIdEncrypted}")]
        public async Task<ActionResult<string>> GetCommunity(string communityIdEncrypted)
        {
            if (!IdHelper.TryDecryptId(communityIdEncrypted, out int communityId, out string error))
                return BadRequest(error ?? "Неверный ID");

            var community = await _context.Communities.FindAsync(communityId);
            if (community == null)
                return NotFound();

            string json = JsonSerializer.Serialize(community);
            string encrypted = AesEncryptionService.Encrypt(json);
            return Ok(encrypted);
        }

        /*[HttpPost("subscribe")]
        public async Task<IActionResult> SubscribeToCommunity([FromBody] string encryptedData)
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

            var subscription = JsonSerializer.Deserialize<CommunityMember>(json);
            if (subscription == null || subscription.UserId == 0 || subscription.CommunityId == 0)
                return BadRequest("Некорректные данные");

            var exists = await _context.CommunityMembers
                .AnyAsync(cm => cm.UserId == subscription.UserId && cm.CommunityId == subscription.CommunityId);

            if (exists)
                return BadRequest("Вы уже подписаны");

            _context.CommunityMembers.Add(subscription);
            await _context.SaveChangesAsync();

            return Ok("Подписка успешна");
        }*/

        [HttpPost("create")]
        public async Task<ActionResult<string>> CreateCommunity([FromBody] CommunityCreateDto dto)
        {
            int creatorId;
            try
            {
                string decrypted = AesEncryptionService.Decrypt(dto.CreatorId);
                creatorId = int.Parse(decrypted);
            }
            catch
            {
                return BadRequest("Ошибка расшифровки creatorId");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Название сообщества обязательно");

            // Проверка наличия сообщества с таким названием
            bool communityExists = await _context.Communities
                .AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower());

            if (communityExists)
                return BadRequest("Сообщество с таким названием уже существует");

            var community = new Community
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatorId = creatorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Communities.Add(community);
            await _context.SaveChangesAsync();

            _context.Communities.Add(community);
            await _context.SaveChangesAsync();

            // Добавляем создателя в участники
            var member = new CommunityMember
            {
                CommunityId = community.Id,
                UserId = creatorId,
                JoinedAt = DateTime.UtcNow,
                Role = "Сreator"
            };

            _context.CommunityMembers.Add(member);
            await _context.SaveChangesAsync();

            string encryptedId = AesEncryptionService.Encrypt(community.Id.ToString());
            return Ok(encryptedId);
        }

        [HttpGet("user/{userIdEncrypted}")]
        public async Task<IActionResult> GetUserCommunities(string userIdEncrypted)
        {
            if (!IdHelper.TryDecryptId(userIdEncrypted, out int userId, out string error))
                return BadRequest(error ?? "Неверный ID");

            var communities = await _context.Communities.GetUserCreatedCommunitiesAsync(userId);
            return Ok(communities);
        }

        [HttpGet("all-except-user/{encryptedUserId}")]
        public async Task<IActionResult> GetAllCommunitiesExceptUser(string encryptedUserId)
        {
            if (!IdHelper.TryDecryptId(encryptedUserId, out int userId, out string error))
                return BadRequest(error ?? "Неверный ID");

            var communities = await _context.Communities.GetAllCommunitiesExceptUserAsync(userId);
            return Ok(communities);
        }

        [HttpDelete("delete/{communityIdEncrypted}")]
        public async Task<IActionResult> DeleteCommunity(string communityIdEncrypted)
        {
            if (!IdHelper.TryDecryptId(communityIdEncrypted, out int communityId, out string error))
                return BadRequest(error ?? "Неверный ID");

            var community = await _context.Communities
                .Include(c => c.Posts)
                .FirstOrDefaultAsync(c => c.Id == communityId);

            if (community == null) return NotFound();

            _context.Posts.RemoveRange(community.Posts);
            _context.Communities.Remove(community);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("transfer/{communityId}")]
        public async Task<IActionResult> TransferOwnership(int communityId)
        {
            var community = await _context.Communities
                .Include(c => c.Posts)
                .Include(c => c.CommunityMembers)
                .FirstOrDefaultAsync(c => c.Id == communityId);

            if (community == null)
                return NotFound("Сообщество не найдено");

            // Проверка: есть ли другие участники кроме создателя
            var otherMembers = community.CommunityMembers
                .Where(m => m.UserId != community.CreatorId)
                .ToList();

            if (!otherMembers.Any())
                return BadRequest("Нет других участников для передачи прав");

            // Определяем самого активного по количеству постов
            var topUserId = community.Posts
                .Where(p => p.UserId != community.CreatorId)
                .GroupBy(p => p.UserId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            if (topUserId != 0)
            {
                community.CreatorId = topUserId;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }
    }
}
