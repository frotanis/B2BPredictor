namespace B2BPredictor.Domain.Entities;

public class Client
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Industry { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public int CreditRating { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Order> Orders { get; } = new List<Order>();
}
