using System;
using System.Collections.Generic;

namespace MacroSocietyAPI.Models;

public partial class EmailLoginCode
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Code { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool? IsUsed { get; set; }
}
