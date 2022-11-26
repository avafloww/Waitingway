using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProtoBuf;

namespace Waitingway.Backend.Database.Models;

[Table("DiscordLinkInfo", Schema = "waitingway")]
[ProtoContract]
public class DiscordLinkInfo
{
    [Key] [ProtoMember(1)] public Guid ClientId { get; set; }

    [ProtoMember(2)] public ulong DiscordUserId { get; set; }

    [ProtoMember(3)] public DateTime LinkTime { get; set; } = DateTime.UtcNow;
}