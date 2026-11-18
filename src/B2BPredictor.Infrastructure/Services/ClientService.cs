using System.ComponentModel.DataAnnotations;
using B2BPredictor.Application.Contracts;
using B2BPredictor.Domain.Entities;
using B2BPredictor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BPredictor.Infrastructure.Services;

public sealed class ClientService : IClientService
{
    private readonly B2BPredictorDbContext _db;

    public ClientService(B2BPredictorDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ClientDto>> GetAllAsync(
        string? search = null,
        string? sortBy = "name",
        CancellationToken ct = default)
    {
        IQueryable<Client> query = _db.Clients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(client =>
                EF.Functions.Like(client.Name, pattern) ||
                EF.Functions.Like(client.Industry, pattern) ||
                EF.Functions.Like(client.ContactEmail, pattern));
        }

        query = sortBy switch
        {
            "-name" => query.OrderByDescending(client => client.Name),
            "creditRating" => query.OrderBy(client => client.CreditRating),
            "-creditRating" => query.OrderByDescending(client => client.CreditRating),
            "createdAt" => query.OrderBy(client => client.CreatedAtUtc),
            "-createdAt" => query.OrderByDescending(client => client.CreatedAtUtc),
            _ => query.OrderBy(client => client.Name)
        };

        return await query
            .Select(client => new ClientDto(
                client.Id,
                client.Name,
                client.Industry,
                client.ContactEmail,
                client.CreditRating,
                client.Orders.Count,
                client.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<ClientDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Clients
            .AsNoTracking()
            .Where(client => client.Id == id)
            .Select(client => new ClientDto(
                client.Id,
                client.Name,
                client.Industry,
                client.ContactEmail,
                client.CreditRating,
                client.Orders.Count,
                client.CreatedAtUtc))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<ServiceResult<ClientDto>> CreateAsync(CreateClientRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationError = ValidateClient(request.Name, request.Industry, request.ContactEmail, request.CreditRating);
        if (validationError is not null)
        {
            return ServiceResult.Failure<ClientDto>(validationError, ServiceErrorType.Validation);
        }

        var email = request.ContactEmail.Trim().ToLowerInvariant();
        if (await _db.Clients.AnyAsync(client => client.ContactEmail == email, ct))
        {
            return ServiceResult.Failure<ClientDto>("Клієнт з таким email вже існує.", ServiceErrorType.Conflict);
        }

        var client = new Client
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Industry = request.Industry.Trim(),
            ContactEmail = email,
            CreditRating = request.CreditRating,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success(MapToDto(client, 0));
    }

    public async Task<ServiceResult<ClientDto>> UpdateAsync(
        Guid id,
        UpdateClientRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationError = ValidateClient(request.Name, request.Industry, request.ContactEmail, request.CreditRating);
        if (validationError is not null)
        {
            return ServiceResult.Failure<ClientDto>(validationError, ServiceErrorType.Validation);
        }

        var client = await _db.Clients.SingleOrDefaultAsync(candidate => candidate.Id == id, ct);
        if (client is null)
        {
            return ServiceResult.Failure<ClientDto>("Клієнта не знайдено.", ServiceErrorType.NotFound);
        }

        var email = request.ContactEmail.Trim().ToLowerInvariant();
        if (await _db.Clients.AnyAsync(candidate => candidate.Id != id && candidate.ContactEmail == email, ct))
        {
            return ServiceResult.Failure<ClientDto>("Клієнт з таким email вже існує.", ServiceErrorType.Conflict);
        }

        client.Name = request.Name.Trim();
        client.Industry = request.Industry.Trim();
        client.ContactEmail = email;
        client.CreditRating = request.CreditRating;

        await _db.SaveChangesAsync(ct);

        var totalOrdersCount = await _db.Orders.CountAsync(order => order.ClientId == id, ct);
        return ServiceResult.Success(MapToDto(client, totalOrdersCount));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var client = await _db.Clients.SingleOrDefaultAsync(candidate => candidate.Id == id, ct);
        if (client is null)
        {
            return false;
        }

        if (await _db.Orders.AnyAsync(order => order.ClientId == id, ct))
        {
            return false;
        }

        _db.Clients.Remove(client);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static string? ValidateClient(string name, string industry, string contactEmail, int creditRating)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length is < 2 or > 150)
        {
            return "Назва клієнта повинна містити від 2 до 150 символів.";
        }

        if (string.IsNullOrWhiteSpace(industry) || industry.Trim().Length is < 2 or > 100)
        {
            return "Галузь клієнта повинна містити від 2 до 100 символів.";
        }

        if (string.IsNullOrWhiteSpace(contactEmail) ||
            contactEmail.Trim().Length > 150 ||
            !new EmailAddressAttribute().IsValid(contactEmail.Trim()))
        {
            return "Некоректний контактний email.";
        }

        if (creditRating is < 1 or > 100)
        {
            return "Кредитний рейтинг повинен бути в межах від 1 до 100.";
        }

        return null;
    }

    private static ClientDto MapToDto(Client client, int totalOrdersCount)
    {
        return new ClientDto(
            client.Id,
            client.Name,
            client.Industry,
            client.ContactEmail,
            client.CreditRating,
            totalOrdersCount,
            client.CreatedAtUtc);
    }
}
