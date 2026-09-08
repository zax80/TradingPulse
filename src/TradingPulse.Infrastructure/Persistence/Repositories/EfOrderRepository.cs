using Microsoft.EntityFrameworkCore;
using TradingPulse.Application.Abstractions;
using TradingPulse.Application.Models;
using TradingPulse.Domain;
using TradingPulse.Infrastructure.Persistence.Entities;

namespace TradingPulse.Infrastructure.Persistence.Repositories;

/// <summary>EF Core-backed order + decision store. See <see cref="IOrderRepository"/>.</summary>
public sealed class EfOrderRepository(IDbContextFactory<TradingPulseDbContext> dbContextFactory) : IOrderRepository
{
    public async Task AddAsync(Order order, OrderDecision decision, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        db.Orders.Add(OrderEntity.FromDomain(order));
        db.OrderDecisions.Add(OrderDecisionEntity.FromDomain(decision));

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrderHistoryEntry?> GetByClientOrderIdAsync(string clientOrderId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var normalized = clientOrderId.ToLower();
        // CA1862 suggests string.Equals(..., StringComparison.OrdinalIgnoreCase) here, but this
        // runs inside an EF Core query - ToLower() translates to SQL LOWER(); the StringComparison
        // overload does not translate and would throw at runtime.
#pragma warning disable CA1862
        var match = await (from order in db.Orders
                            join decision in db.OrderDecisions on order.Id equals decision.OrderId
                            where order.ClientOrderId.ToLower() == normalized
                            select new { Order = order, Decision = decision })
            .FirstOrDefaultAsync(cancellationToken);
#pragma warning restore CA1862

        return match is null ? null : new OrderHistoryEntry(match.Order.ToDomain(), match.Decision.ToDomain());
    }

    public async Task<IReadOnlyList<OrderHistoryEntry>> GetHistoryAsync(OrderHistoryFilter filter, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var query = from order in db.Orders
                     join decision in db.OrderDecisions on order.Id equals decision.OrderId
                     select new { Order = order, Decision = decision };

        if (filter.Symbol is not null)
        {
            var symbol = filter.Symbol.ToLower();
            // See CA1862 note in ExistsWithClientOrderIdAsync above - same EF query-translation reason.
#pragma warning disable CA1862
            query = query.Where(x => x.Order.Symbol.ToLower() == symbol);
#pragma warning restore CA1862
        }

        if (filter.Side is not null)
        {
            var side = filter.Side.Value.ToString();
            query = query.Where(x => x.Order.Side == side);
        }

        if (filter.Origin is not null)
        {
            var origin = filter.Origin.Value.ToString();
            query = query.Where(x => x.Order.Origin == origin);
        }

        if (filter.Status is not null)
        {
            var status = filter.Status.Value.ToString();
            query = query.Where(x => x.Decision.Status == status);
        }

        if (filter.From is not null)
        {
            query = query.Where(x => x.Order.SubmittedAt >= filter.From);
        }

        if (filter.To is not null)
        {
            query = query.Where(x => x.Order.SubmittedAt <= filter.To);
        }

        var page = await query
            .OrderByDescending(x => x.Order.SubmittedAt)
            .Skip(filter.Skip)
            .Take(filter.Take)
            .ToListAsync(cancellationToken);

        return [.. page.Select(x => new OrderHistoryEntry(x.Order.ToDomain(), x.Decision.ToDomain()))];
    }

    public async Task<IReadOnlyList<OrderHistoryEntry>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var normalized = symbol.ToLower();
        // See CA1862 note in ExistsWithClientOrderIdAsync above - same EF query-translation reason.
#pragma warning disable CA1862
        var page = await (from order in db.Orders
                           join decision in db.OrderDecisions on order.Id equals decision.OrderId
                           where order.Symbol.ToLower() == normalized
                           orderby order.SubmittedAt descending
                           select new { Order = order, Decision = decision })
            .ToListAsync(cancellationToken);
#pragma warning restore CA1862

        return [.. page.Select(x => new OrderHistoryEntry(x.Order.ToDomain(), x.Decision.ToDomain()))];
    }
}
