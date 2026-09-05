using TradingPulse.Api.Contracts;
using TradingPulse.Application.Abstractions;
using TradingPulse.Application.Models;
using TradingPulse.Domain;
using TradingPulse.Domain.Enums;

namespace TradingPulse.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders", SubmitOrderAsync);
        app.MapGet("/api/orders/history", GetHistoryAsync);
        app.MapGet("/api/orders/by-symbol/{symbol}", GetBySymbolAsync);

        return app;
    }

    private static async Task<IResult> SubmitOrderAsync(
        SubmitOrderRequest request,
        IOrderSubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientOrderId) || string.IsNullOrWhiteSpace(request.Symbol))
        {
            return Results.BadRequest("ClientOrderId and Symbol are required.");
        }

        if (!Enum.TryParse<OrderSide>(request.Side, ignoreCase: true, out var side))
        {
            return Results.BadRequest($"Invalid Side '{request.Side}'. Expected Buy or Sell.");
        }

        if (!Enum.TryParse<OrderType>(request.Type, ignoreCase: true, out var type))
        {
            return Results.BadRequest($"Invalid Type '{request.Type}'. Expected Limit.");
        }

        if (request.Price <= 0 || request.Quantity <= 0)
        {
            return Results.BadRequest("Price and Quantity must be positive.");
        }

        var order = Order.CreateUserSubmitted(
            request.ClientOrderId, request.Symbol, side, type, request.Price, request.Quantity, DateTimeOffset.UtcNow);

        var decision = await submissionService.SubmitAsync(order, cancellationToken);

        var response = new OrderSubmissionResponse(
            order.Id, order.ClientOrderId, order.Symbol, order.Side.ToString(), decision.Status.ToString(), decision.RejectionReasons);

        return decision.Status == DecisionStatus.Accepted ? Results.Ok(response) : Results.UnprocessableEntity(response);
    }

    private static async Task<IResult> GetHistoryAsync(
        IOrderRepository repository,
        string? symbol,
        string? side,
        string? origin,
        string? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        OrderSide? sideFilter = null;
        if (side is not null)
        {
            if (!Enum.TryParse<OrderSide>(side, true, out var parsed))
            {
                return Results.BadRequest($"Invalid side '{side}'.");
            }

            sideFilter = parsed;
        }

        OrderOrigin? originFilter = null;
        if (origin is not null)
        {
            if (!Enum.TryParse<OrderOrigin>(origin, true, out var parsed))
            {
                return Results.BadRequest($"Invalid origin '{origin}'.");
            }

            originFilter = parsed;
        }

        DecisionStatus? statusFilter = null;
        if (status is not null)
        {
            if (!Enum.TryParse<DecisionStatus>(status, true, out var parsed))
            {
                return Results.BadRequest($"Invalid status '{status}'.");
            }

            statusFilter = parsed;
        }

        var filter = new OrderHistoryFilter(
            symbol, sideFilter, originFilter, statusFilter, from, to,
            Skip: skip < 0 ? 0 : skip,
            Take: take <= 0 ? 100 : take);

        var history = await repository.GetHistoryAsync(filter, cancellationToken);
        return Results.Ok(history);
    }

    private static async Task<IResult> GetBySymbolAsync(string symbol, IOrderRepository repository, CancellationToken cancellationToken)
    {
        var orders = await repository.GetBySymbolAsync(symbol, cancellationToken);
        return Results.Ok(orders);
    }
}
