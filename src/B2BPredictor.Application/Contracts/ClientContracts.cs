using System.ComponentModel.DataAnnotations;

namespace B2BPredictor.Application.Contracts;

public sealed record ClientDto(
    Guid Id,
    string Name,
    string Industry,
    string ContactEmail,
    int CreditRating,
    int TotalOrdersCount,
    DateTime CreatedAtUtc);

public sealed record CreateClientRequest(
    [property: Required, StringLength(150, MinimumLength = 2)] string Name,
    [property: Required, StringLength(100, MinimumLength = 2)] string Industry,
    [property: Required, EmailAddress, StringLength(150)] string ContactEmail,
    [property: Range(1, 100)] int CreditRating);

public sealed record UpdateClientRequest(
    [property: Required, StringLength(150, MinimumLength = 2)] string Name,
    [property: Required, StringLength(100, MinimumLength = 2)] string Industry,
    [property: Required, EmailAddress, StringLength(150)] string ContactEmail,
    [property: Range(1, 100)] int CreditRating);

public interface IClientService
{
    Task<IReadOnlyList<ClientDto>> GetAllAsync(
        string? search = null,
        string? sortBy = "name",
        CancellationToken ct = default);

    Task<ClientDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResult<ClientDto>> CreateAsync(CreateClientRequest request, CancellationToken ct = default);
    Task<ServiceResult<ClientDto>> UpdateAsync(Guid id, UpdateClientRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}