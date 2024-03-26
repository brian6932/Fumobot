﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fumo.Database.DTO;

public class UserDTO
{
    [Key]
    public required string TwitchID { get; set; }

    public required string TwitchName { get; set; }

    [Column(TypeName = "jsonb")]
    public List<UsernameHistory> UsernameHistory { get; set; } = [];

    public DateTime DateSeen { get; set; }

    [Column(TypeName = "jsonb")]
    public List<Setting> Settings { get; set; } = [];

    public List<string> Permissions { get; set; } = [];
}

public record UsernameHistory(string Username, DateTime DateChanged);