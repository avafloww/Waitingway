using System.ComponentModel.DataAnnotations;

namespace Waitingway.Backend.Database.Models;

public class DiscordLinkInfo
{
    [Key] public Guid ClientId { get; set; }
    public ulong DiscordUserId { get; set; }
    public DateTime LinkTime { get; set; } = DateTime.UtcNow;
}