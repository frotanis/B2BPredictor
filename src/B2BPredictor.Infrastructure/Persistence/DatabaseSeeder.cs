using B2BPredictor.Domain.Entities;
using B2BPredictor.Domain.Enums;
using B2BPredictor.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace B2BPredictor.Infrastructure.Persistence;

public sealed class DatabaseSeeder
{
    private readonly B2BPredictorDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseSeeder(B2BPredictorDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedUsersAsync(ct);
        await SeedClientsAsync(ct);
        await SeedOrdersAsync(ct);
    }

    private async Task SeedUsersAsync(CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(ct))
        {
            return;
        }

        var now = DateTime.UtcNow;
        _db.Users.AddRange(
            new AppUser
            {
                Id = Guid.NewGuid(),
                FullName = "System Administrator",
                Email = "admin@b2bpredictor.local",
                PasswordHash = _passwordHasher.Hash("Admin123!"),
                Role = UserRole.Admin,
                CreatedAtUtc = now
            },
            new AppUser
            {
                Id = Guid.NewGuid(),
                FullName = "Sales Manager",
                Email = "manager@b2bpredictor.local",
                PasswordHash = _passwordHasher.Hash("Manager123!"),
                Role = UserRole.Manager,
                CreatedAtUtc = now
            });

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedClientsAsync(CancellationToken ct)
    {
        if (await _db.Clients.AnyAsync(ct))
        {
            return;
        }

        var now = DateTime.UtcNow;
        _db.Clients.AddRange(
            new Client
            {
                Id = Guid.NewGuid(),
                Name = "Kyiv Industrial Group",
                Industry = "Manufacturing",
                ContactEmail = "procurement@kyiv-industrial.local",
                CreditRating = 82,
                CreatedAtUtc = now.AddDays(-180)
            },
            new Client
            {
                Id = Guid.NewGuid(),
                Name = "Lviv Logistics Partners",
                Industry = "Logistics",
                ContactEmail = "orders@lviv-logistics.local",
                CreditRating = 64,
                CreatedAtUtc = now.AddDays(-150)
            },
            new Client
            {
                Id = Guid.NewGuid(),
                Name = "Dnipro Agro Holding",
                Industry = "Agriculture",
                ContactEmail = "supply@dnipro-agro.local",
                CreditRating = 71,
                CreatedAtUtc = now.AddDays(-120)
            },
            new Client
            {
                Id = Guid.NewGuid(),
                Name = "Kharkiv Retail Network",
                Industry = "Retail",
                ContactEmail = "buyers@kharkiv-retail.local",
                CreditRating = 45,
                CreatedAtUtc = now.AddDays(-90)
            },
            new Client
            {
                Id = Guid.NewGuid(),
                Name = "Odesa Technology Solutions",
                Industry = "IT",
                ContactEmail = "finance@odesa-tech.local",
                CreditRating = 93,
                CreatedAtUtc = now.AddDays(-60)
            });

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedOrdersAsync(CancellationToken ct)
    {
        if (await _db.Orders.AnyAsync(ct))
        {
            return;
        }

        var clients = await _db.Clients.ToListAsync(ct);
        if (clients.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var random = new Random(42);
        var products = new[]
        {
            "Steel profiles",
            "Hydraulic pumps",
            "Control modules",
            "Packaging line",
            "Bearings set",
            "Sensor kit"
        };

        var orders = new List<Order>();

        foreach (var client in clients)
        {
            for (var index = 0; index < 6; index++)
            {
                var createdAt = now.AddDays(-random.Next(1, 150));
                var itemCount = random.Next(1, 5);
                var items = new List<OrderItem>();
                decimal total = 0;

                for (var itemIndex = 0; itemIndex < itemCount; itemIndex++)
                {
                    var quantity = random.Next(1, 4);
                    var unitPrice = Math.Round((decimal)(random.NextDouble() * 4000 + 100), 2);
                    total += quantity * unitPrice;
                    items.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductName = products[random.Next(products.Length)],
                        Quantity = quantity,
                        UnitPrice = unitPrice
                    });
                }

                var probability = Math.Round(random.NextDouble() * 100, 2);
                var riskLevel = probability >= 75
                    ? "Низький ризик"
                    : probability >= 45
                        ? "Помірний ризик"
                        : "Високий ризик";

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    Client = client,
                    Amount = total,
                    PaymentTermsDays = random.Next(0, 90),
                    ItemsCount = itemCount,
                    Status = (OrderStatus)random.Next(0, 4),
                    SuccessProbability = probability,
                    RiskLevel = riskLevel,
                    CreatedAtUtc = createdAt
                };

                foreach (var item in items)
                {
                    order.Items.Add(item);
                }

                var prediction = new PredictionHistory
                {
                    Id = Guid.NewGuid(),
                    SuccessProbability = probability,
                    IsLikelySuccessful = probability >= 50,
                    RiskLevel = riskLevel,
                    ModelVersion = "FastTree-v1.0",
                    CreatedAtUtc = createdAt.AddMinutes(5)
                };

                prediction.RiskFactors.Add(new PredictionRiskFactor
                {
                    Id = Guid.NewGuid(),
                    Description = riskLevel
                });

                order.Predictions.Add(prediction);
                orders.Add(order);
            }
        }

        _db.Orders.AddRange(orders);
        await _db.SaveChangesAsync(ct);
    }
}
