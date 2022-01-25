using Microsoft.ML.Data;

namespace Waitingway.Backend.Analysis.Database;

public class QueueTimePrediction
{
    [ColumnName("Score")] public float QueueTime;

    [NoColumn] public TimeSpan TimeEstimate => TimeSpan.FromMinutes(QueueTime);
}