using B2BPredictor.Application.Contracts;
using B2BPredictor.Domain.Entities;
using B2BPredictor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ContractOrderStatus = B2BPredictor.Application.Contracts.OrderStatus;
using DomainOrderStatus = B2BPredictor.Domain.Enums.OrderStatus;

namespace B2BPredictor.Infrastructure.Services;

public sealed class OrderService : IOrderService
{
    private readonly B2BPredictorDbContext _db;
    private readonly IPredictionService _predictionService;

    public OrderService(B2BPredictorDbContext db, IPredictionService predictionService)
    {
        _db = db;
        _predictionService = predictionService;
    }

    public async Task<PagedResult<OrderDto>> GetOrdersAsync(OrderFilterQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        IQueryable<Order> orders = _db.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(order => order.Client)
            .Include(order => order.Predictions)
            .ThenInclude(prediction => prediction.RiskFactors);

        if (query.Status.HasValue)
        {
            var status = ToDomainStatus(query.Status.Value);
            orders = orders.Where(order => order.Status == status);
        }

        if (query.ClientId.HasValue)
        {
            orders = orders.Where(order => order.ClientId == query.ClientId.Value);
        }

        orders = query.SortBy switch
        {
            "amount" => orders.OrderBy(order => order.Amount),
            "-amount" => orders.OrderByDescending(order => order.Amount),
            "clientName" => orders.OrderBy(order => order.Client.Name),
            "-clientName" => orders.OrderByDescending(order => order.Client.Name),
            "createdAt" => orders.OrderBy(order => order.CreatedAtUtc),
            _ => orders.OrderByDescending(order => order.CreatedAtUtc)
        };

        var totalCount = await orders.CountAsync(ct);
        var items = await orders
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<OrderDto>(
            items.Select(MapToDto).ToList(),
            totalCount,
            page,
            pageSize);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(candidate => candidate.Client)
            .Include(candidate => candidate.Predictions)
            .ThenInclude(prediction => prediction.RiskFactors)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, ct);

        return order is null ? null : MapToDto(order);
    }

    public async Task<ServiceResult<OrderDto>> CreateAndPredictAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);

        if (amount is <= 0 or > 100_000_000)
        {
            return ServiceResult.Failure<OrderDto>("Сума замовлення повинна бути в межах від 0.01 до 100000000.", ServiceErrorType.Validation);
        }

        if (request.PaymentTermsDays is < 0 or > 365)
        {
            return ServiceResult.Failure<OrderDto>("Термін оплати повинен бути в межах від 0 до 365 днів.", ServiceErrorType.Validation);
        }

        if (request.ItemsCount is < 1 or > 10_000)
        {
            return ServiceResult.Failure<OrderDto>("Кількість позицій повинна бути в межах від 1 до 10000.", ServiceErrorType.Validation);
        }

        if (request.Notes?.Length > 500)
        {
            return ServiceResult.Failure<OrderDto>("Примітка не повинна перевищувати 500 символів.", ServiceErrorType.Validation);
        }

        var client = await _db.Clients
            .SingleOrDefaultAsync(candidate => candidate.Id == request.ClientId, ct);

        if (client is null)
        {
            return ServiceResult.Failure<OrderDto>("Клієнта не знайдено.", ServiceErrorType.NotFound);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var previousSuccessfulOrders = await _db.Orders.CountAsync(
                order => order.ClientId == request.ClientId && order.Status == DomainOrderStatus.Completed,
                ct);

            var order = new Order
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Client = client,
                Amount = amount,
                PaymentTermsDays = request.PaymentTermsDays,
                ItemsCount = request.ItemsCount,
                Status = DomainOrderStatus.Pending,
                Notes = request.Notes,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Orders.Add(order);

            var prediction = await _predictionService.PredictAsync(
                new OrderPredictionInput(
                    amount,
                    client.Industry,
                    client.CreditRating,
                    request.PaymentTermsDays,
                    request.ItemsCount,
                    previousSuccessfulOrders),
                ct);

            order.SuccessProbability = prediction.SuccessProbability;
            order.RiskLevel = prediction.RiskLevel;

            var history = new PredictionHistory
            {
                Id = Guid.NewGuid(),
                SuccessProbability = prediction.SuccessProbability,
                IsLikelySuccessful = prediction.IsLikelySuccessful,
                RiskLevel = prediction.RiskLevel,
                ModelVersion = prediction.ModelVersion,
                CreatedAtUtc = DateTime.UtcNow
            };

            foreach (var riskFactor in prediction.RiskFactors)
            {
                history.RiskFactors.Add(new PredictionRiskFactor
                {
                    Id = Guid.NewGuid(),
                    Description = riskFactor
                });
            }

            order.Predictions.Add(history);

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return ServiceResult.Success(MapToDto(order));
        }
        catch (InvalidOperationException exception)
        {
            await transaction.RollbackAsync(ct);
            return ServiceResult.Failure<OrderDto>(exception.Message, ServiceErrorType.Validation);
        }
        catch (ArgumentException exception)
        {
            await transaction.RollbackAsync(ct);
            return ServiceResult.Failure<OrderDto>(exception.Message, ServiceErrorType.Validation);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            return ServiceResult.Failure<OrderDto>("Не вдалося зберегти замовлення.", ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult<OrderDto>> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(request.Status))
        {
            return ServiceResult.Failure<OrderDto>("Некоректний статус замовлення.", ServiceErrorType.Validation);
        }

        if (request.Notes?.Length > 500)
        {
            return ServiceResult.Failure<OrderDto>("Примітка не повинна перевищувати 500 символів.", ServiceErrorType.Validation);
        }

        var order = await _db.Orders
            .Include(candidate => candidate.Client)
            .Include(candidate => candidate.Predictions)
            .ThenInclude(prediction => prediction.RiskFactors)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, ct);

        if (order is null)
        {
            return ServiceResult.Failure<OrderDto>("Замовлення не знайдено.", ServiceErrorType.NotFound);
        }

        order.Status = ToDomainStatus(request.Status);
        order.Notes = request.Notes;

        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success(MapToDto(order));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _db.Orders.FindAsync(new object[] { id }, ct);
        if (order is null)
        {
            return false;
        }

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static OrderDto MapToDto(Order order)
    {
        var latestPrediction = order.Predictions
            .OrderByDescending(prediction => prediction.CreatedAtUtc)
            .FirstOrDefault();

        IReadOnlyList<string> riskFactors = latestPrediction is null
            ? Array.Empty<string>()
            : latestPrediction.RiskFactors.Select(riskFactor => riskFactor.Description).ToList();

        return new OrderDto(
            order.Id,
            order.ClientId,
            order.Client.Name,
            order.Amount,
            order.PaymentTermsDays,
            order.ItemsCount,
            ToContractStatus(order.Status),
            order.SuccessProbability,
            order.RiskLevel,
            riskFactors,
            order.Notes,
            order.CreatedAtUtc);
    }

    private static ContractOrderStatus ToContractStatus(DomainOrderStatus status)
    {
        return (ContractOrderStatus)(int)status;
    }

    private static DomainOrderStatus ToDomainStatus(ContractOrderStatus status)
    {
        return (DomainOrderStatus)(int)status;
    }
}
