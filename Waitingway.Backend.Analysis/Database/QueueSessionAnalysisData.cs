using Microsoft.ML.Data;
using Waitingway.Backend.Database.Models;
using Waitingway.Backend.Database.Queue;

namespace Waitingway.Backend.Analysis.Database;

public class QueueSessionAnalysisData
{
    [NoColumn] public int Id { get; set; }

    [NoColumn] public QueueSession.Type SessionType { get; set; }

    [NoColumn] public DateTime StartTime { get; set; }

    [NoColumn] public DateTime EndTime { get; set; }

    [NoColumn] public QueueSessionData.QueueEndReason EndReason { get; set; }
    
    public float DataCenter { get; set; }
    public float World { get; set; }
    
    public float StartPos { get; set; }

    public float QueueTime => (float) (EndTime - StartTime).TotalMinutes;

    [NoColumn] public TimeSpan NormalizedStartTime => new(StartTime.Hour, StartTime.Minute, StartTime.Second);

    public float TimeOfDay_Sin => (float) Math.Sin(2 * Math.PI * NormalizedStartTime.TotalSeconds / 86400);
    public float TimeOfDay_Cos => (float) Math.Cos(2 * Math.PI * NormalizedStartTime.TotalSeconds / 86400);

    [NoColumn] public int DayOfWeek => (int) StartTime.DayOfWeek;

    public float DayOfWeek_Sin => (float) Math.Sin(2 * Math.PI * DayOfWeek / 6);
    public float DayOfWeek_Cos => (float) Math.Sin(2 * Math.PI * DayOfWeek / 6);

    public static QueueSessionAnalysisData From(ClientQueue queue)
    {
        return new QueueSessionAnalysisData
        {
            DataCenter = queue.DbSession.DataCenter,
            World = queue.DbSession.World ?? 0,
            StartPos = queue.QueuePosition,
            StartTime = queue.LastUpdateReceived // todo: this is technically inaccurate
        };
    }
}