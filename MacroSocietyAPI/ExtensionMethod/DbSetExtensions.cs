namespace MacroSocietyAPI.ExtensionMethod
{
    using MacroSocietyAPI.Models;
    using Microsoft.EntityFrameworkCore;
    using System.Linq;
    using System.Threading.Tasks;

    public static class DbSetExtensions
    {
        public static async Task<List<Community>> GetAllCommunitiesAsync(this DbSet<Community> communities)
        {
            return await communities.ToListAsync();
        }

        public static async Task<List<CommunityDto>> GetAllCommunitiesExceptUserAsync(this DbSet<Community> communities, int userId)
        {
            return await communities
                .Include(c => c.CommunityMembers)
                .Where(c => c.CreatorId != userId)
                .Select(c => new CommunityDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    CreatorId = c.CreatorId,
                    IsMember = c.CommunityMembers.Any(cm => cm.UserId == userId)
                })
                .ToListAsync();
        }

        public static async Task<List<Community>> GetUserCreatedCommunitiesAsync(this DbSet<Community> communities, int userId)
        {
            return await communities
                .Where(c => c.CreatorId == userId)
                .ToListAsync();
        }

        public static async Task<List<CommunityMember>> GetUserMembershipsAsync(this DbSet<CommunityMember> members, int userId)
        {
            return await members
                .Where(m => m.UserId == userId)
                .Include(m => m.Community)
                .ToListAsync();
        }

        public static async Task<bool> LeaveCommunityAsync(this DbSet<CommunityMember> members,MacroSocietyDbContext context,int userId,int communityId)
        {
            var membership = await members
                .FirstOrDefaultAsync(m => m.UserId == userId && m.CommunityId == communityId);

            if (membership != null)
            {
                members.Remove(membership);
                await context.SaveChangesAsync();
                return true;
            }

            return false;
        }


        public static async Task<List<User>> GetFriendsWithDetailsAsync(this DbSet<FriendList> friends, int userId)
        {
            return await friends
                .Where(f => f.UserId == userId)
                .Include(f => f.Friend)
                .Select(f => f.Friend)
                .ToListAsync();
        }

        public static async Task<List<User>> GetMutualFriendsAsync(this DbSet<FriendList> friends, int user1Id, int user2Id)
        {
            var user1Friends = await friends.Where(f => f.UserId == user1Id).Select(f => f.FriendId).ToListAsync();
            var user2Friends = await friends.Where(f => f.UserId == user2Id).Select(f => f.FriendId).ToListAsync();

            var mutualIds = user1Friends.Intersect(user2Friends);
            return await friends.Where(f => mutualIds.Contains(f.FriendId)).Select(f => f.Friend).Distinct().ToListAsync();
        }

        public static async Task<List<FriendRequest>> GetIncomingRequestsWithDetailsAsync(this DbSet<FriendRequest> requests, int userId)
        {
            return await requests
                .Where(r => r.ReceiverId == userId)
                .Include(r => r.Sender)
                .ToListAsync();
        }

        public static async Task<List<FriendRequest>> GetOutgoingRequestsAsync(this DbSet<FriendRequest> requests, int userId)
        {
            return await requests
                .Where(r => r.SenderId == userId)
                .Include(r => r.Receiver)
                .ToListAsync();
        }

        public static async Task<List<Message>> GetLatestMessagesPerChatAsync(this DbSet<Message> messages, int userId)
        {
            return await messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .GroupBy(m => new { m.SenderId, m.ReceiverId })
                .Select(g => g.OrderByDescending(m => m.SentAt).First())
                .ToListAsync();
        }

        public static async Task<List<Post>> GetPostsByUserAsync(this DbSet<Post> posts, int userId)
        {
            return await posts
                .Where(p => p.UserId == userId)
                .Include(p => p.Community)
                .ToListAsync();
        }
    }
}
