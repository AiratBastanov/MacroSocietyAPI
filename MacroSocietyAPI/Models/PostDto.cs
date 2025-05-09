namespace MacroSocietyAPI.Models
{
    public class PostDto
    {
        public string UserId { get; set; }         // зашифрованный
        public string CommunityId { get; set; }    // зашифрованный
        public string Content { get; set; }
    }
}
