using System;
using System.Collections.Generic;

namespace MacroSocietyAPI.Models;

public partial class CommunityMember
{
    public int Id { get; set; }

    public int CommunityId { get; set; }

    public int UserId { get; set; }

    public DateTime? JoinedAt { get; set; }

    public string? Role { get; set; }

    public virtual Community Community { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
