using System;
using System.Collections.Generic;

namespace MacroSocietyAPI.Models;

public partial class FriendRequest
{
    public int Id { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public DateTime? SentAt { get; set; }

    public string? Status { get; set; }

    public virtual User Receiver { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
