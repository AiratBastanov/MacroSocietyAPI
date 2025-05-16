namespace MacroSocietyAPI.Models
{
    public class CreateCommentDto
    {
        public string PostId { get; set; } = null!;
        public int UserId { get; set; } = 0!;
        public string Content { get; set; } = null!;
    }
}
