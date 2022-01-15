using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Waitingway.Backend.Database.Models;

[Index(nameof(Time), nameof(Type))]
public class QueueSessionData
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] public QueueSession Session { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    public DateTime Time { get; set; }

    [Required] public DataType Type { get; set; }

    // only set when Type == End
    public QueueEndReason? EndReason { get; set; }

    // next attributes are set only when Type == Update
    public uint? QueuePosition { get; set; }
    public TimeSpan? GameTimeEstimate { get; set; }
    public TimeSpan? OurTimeEstimate { get; set; }

    public enum DataType
    {
        Start = 0,
        Update = 1,
        End = 2
    }
    
    public enum QueueEndReason
    {
        Unknown = 0,
        Success = 1,
        Error = 2,
        UserCancellation = 3
    }
}