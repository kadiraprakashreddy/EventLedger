using AccountService.Application.Commands;
using AccountService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Api.Controllers;

[ApiController]
[Route("accounts")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{accountId}/transactions")]
    public async Task<IActionResult> ApplyTransaction(
        string accountId, [FromBody] ApplyTransactionRequest request, CancellationToken ct)
    {
        var command = new ApplyTransactionCommand(
            accountId,
            request.EventId,
            request.Type,
            request.Amount,
            request.Currency,
            request.EventTimestamp,
            request.TraceId);

        var result = await _mediator.Send(command, ct);

        if (!result.WasNewlyApplied)
            return Ok(result); // duplicate — idempotent no-op, return existing state

        return CreatedAtAction(nameof(GetBalance), new { accountId }, result);
    }

    [HttpGet("{accountId}/balance")]
    public async Task<IActionResult> GetBalance(string accountId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAccountBalanceQuery(accountId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{accountId}")]
    public async Task<IActionResult> GetAccount(string accountId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAccountQuery(accountId), ct);
        return result is null ? NotFound() : Ok(result);
    }
}

public record ApplyTransactionRequest(
    string EventId,
    AccountService.Domain.Enums.TransactionType Type,
    decimal Amount,
    string Currency,
    DateTimeOffset EventTimestamp,
    string? TraceId);