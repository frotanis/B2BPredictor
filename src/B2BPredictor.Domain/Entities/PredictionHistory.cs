namespace B2BPredictor.Domain.Entities;

public class PredictionHistory
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public double SuccessProbability { get; set; }

    public bool IsLikelySuccessful { get; set; }

    public string RiskLevel { get; set; } = string.Empty;

    public string ModelVersion { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<PredictionRiskFactor> RiskFactors { get; } = new List<PredictionRiskFactor>();
}
