namespace MacroSocietyAPI.Models
{
    public class CommunityCreateDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string CreatorId { get; set; } = null!; // Зашифрованный creatorId
    }
}
