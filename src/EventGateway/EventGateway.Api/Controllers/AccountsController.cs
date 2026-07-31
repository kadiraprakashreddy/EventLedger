using EventGateway.Application.Interfaces;
using EventGateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventGateway.Api.Controllers;

[ApiController]
[Route("accounts")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{accountId}/balance")]
    public async Task<IActionResult> GetBalance(string accountId, CancellationToken ct)
    {
        var traceId = HttpContext.Items["TraceId"]?.ToString() ?? HttpContext.TraceIdentifier;

        var result = await _mediator.Send(new GetAccountBalanceQuery(accountId, traceId), ct);

        return result.Status switch
        {
            AccountServiceCallStatus.Success => Ok(new { result.AccountId, result.Balance }),
            AccountServiceCallStatus.Unavailable => StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = result.ErrorMessage ?? "AccountService is unreachable.", result.AccountId }),
            AccountServiceCallStatus.Rejected => NotFound(new { error = result.ErrorMessage, result.AccountId }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
