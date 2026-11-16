/*Цей файл посилається на сутності з B2BPredictor.Domain, тому його потрібно додати після того, як Учасник 2 
 * створить класи сутностей (AppUser, Client, Order) на Етапі 1.
 * Це дозволить вам писати юніт-тести бізнес-логіки без підключення реальної бази даних*/

/*using B2BPredictor.Domain.Entities;

namespace B2BPredictor.Application.Contracts;

public interface IClientRepository
{
    Task<IReadOnlyList<Client>> GetAllAsync(string? search, string? sortBy, CancellationToken ct = default);
    Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Client client, CancellationToken ct = default);
    Task UpdateAsync(Client client, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IOrderRepository
{
    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAsync(OrderFilterQuery query, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<int> CountSuccessfulByClientAsync(Guid clientId, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}*/