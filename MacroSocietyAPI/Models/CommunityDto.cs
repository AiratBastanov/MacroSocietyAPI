namespace MacroSocietyAPI.Models
{
    public class CommunityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int CreatorId { get; set; }
        public bool IsMember { get; set; } // <--- флаг на подписку
    }
}
