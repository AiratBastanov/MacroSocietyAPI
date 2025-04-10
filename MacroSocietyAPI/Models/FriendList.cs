using System;
using System.Collections.Generic;

namespace MacroSocietyAPI.Models;

public partial class FriendList
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int FriendId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Friend { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
