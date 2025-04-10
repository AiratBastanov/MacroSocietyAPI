using System;
using System.Collections.Generic;

namespace MacroSocietyAPI.Models;

public partial class Community
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int CreatorId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CommunityMember> CommunityMembers { get; set; } = new List<CommunityMember>();

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
}
