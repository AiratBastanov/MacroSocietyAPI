using System;
using System.Collections.Generic;

namespace MacroSocietyAPI.Models;

public partial class Post
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? CommunityId { get; set; }

    public string? Content { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual Community? Community { get; set; }

    public virtual User User { get; set; } = null!;
}
