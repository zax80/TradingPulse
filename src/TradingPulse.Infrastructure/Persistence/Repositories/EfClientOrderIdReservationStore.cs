using Microsoft.EntityFrameworkCore;
using Npgsql;
using TradingPulse.Application.Abstractions;
using TradingPulse.Infrastructure.Persistence.Entities;

namespace TradingPulse.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core-backed <see cref="IClientOrderIdReservationStore"/>. The actual guarantee under
/// concurrency comes from the database's own primary key on
/// <c>ClientOrderIdReservationEntity.ClientOrderIdNormalized</c> (see <c>TradingPulseDbContext</c>),
/// not from anything in this class - a second INSERT for the same ClientOrderId is rejected by
/// Postgres itself, even across a restart or from a different instance. This class only translates
/// that unique-violation into the "someone else already reserved it" result instead of letting the
/// exception escape, and looks up who won.
/// </summary>
public sealed class EfClientOrderIdReservationStore(IDbContextFactory<TradingPulseDbContext> dbContextFactory)
    : IClientOrderIdReservationStore
{
    // Postgres SQLSTATE for a unique/primary-key violation.
    private const string UniqueViolationSqlState = "23505";

    public async Task<ClientOrderIdReservationResult> TryReserveAsync(
        string clientOrderId, Guid orderId, Guid decisionId, CancellationToken cancellationToken = default)
    {
        var normalized = clientOrderId.ToLowerInvariant();

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.ClientOrderIdReservations.Add(new ClientOrderIdReservationEntity
        {
            ClientOrderIdNormalized = normalized,
            OrderId = orderId,
            DecisionId = decisionId,
            ReservedAt = DateTimeOffset.UtcNow,
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new ClientOrderIdReservationResult(Reserved: true, ExistingOrderId: orderId, ExistingDecisionId: decisionId);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            // Someone else's reservation won the race between the caller's own lookup and this
            // claim - look up who, so OrderSubmissionService can resolve this exactly as if its
            // initial lookup had found the winner up front.
            await using var readDb = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var winner = await readDb.ClientOrderIdReservations
                .AsNoTracking()
                .FirstAsync(r => r.ClientOrderIdNormalized == normalized, cancellationToken);

            return new ClientOrderIdReservationResult(Reserved: false, ExistingOrderId: winner.OrderId, ExistingDecisionId: winner.DecisionId);
        }
    }
}
