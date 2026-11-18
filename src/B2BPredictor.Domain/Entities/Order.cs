using B2BPredictor.Domain.Enums;

namespace B2BPredictor.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public decimal Amount { get; set; }

    public int PaymentTermsDays { get; set; }

    public int ItemsCount { get; set; }

    public OrderStatus Status { get; set; }

    public string? Notes { get; set; }

    public double? SuccessProbability { get; set; }

    public string? RiskLevel { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<OrderItem> Items { get; } = new List<OrderItem>();

    public ICollection<PredictionHistory> Predictions { get; } = new List<PredictionHistory>();
}
