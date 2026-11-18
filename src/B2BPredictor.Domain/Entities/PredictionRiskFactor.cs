namespace B2BPredictor.Domain.Entities;

public class PredictionRiskFactor
{
    public Guid Id { get; set; }

    public Guid PredictionHistoryId { get; set; }

    public PredictionHistory PredictionHistory { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
}
