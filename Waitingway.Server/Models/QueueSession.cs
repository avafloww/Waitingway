using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Waitingway.Server.Models;

[Index(nameof(DataCenter), nameof(World))]
public class QueueSession
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    public int Id { get; set; }

    [Required] public Guid ClientId { get; set; }

    [Required] public string ClientSessionId { get; set; }

    [Required] public int DataCenter { get; set; }

    [Required] public Type SessionType { get; set; }

    /** only applicable for Login or WorldTravel session types */
    public int? World { get; set; }

    /** only applicable for Duty session type */
    public int? DutyContentId { get; set; }
    
    public List<QueueSessionData> DataPoints { get; set; }

    public enum Type
    {
        Login = 0,
        WorldTravel = 1,
        Duty = 2
    }
}