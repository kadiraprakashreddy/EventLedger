using System.Text.Json;
using EventGateway.Application.Commands;
using EventGateway.Application.Queries;
using EventGateway.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventGateway.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitEvent([FromBody] SubmitEventRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.Items["TraceId"]?.ToString() ?? HttpContext.TraceIdentifier;

        var command = new SubmitEventCommand(
            request.EventId, request.AccountId, request.Type, request.Amount,
            request.Currency, request.EventTimestamp,
            request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata),
            traceId);

        var result = await _mediator.Send(command, ct);

        return result.Outcome switch
        {
            SubmitEventOutcome.Duplicate => Ok(result),
            SubmitEventOutcome.Created => CreatedAtAction(nameof(GetEvent), new { id = result.EventId }, result),
            SubmitEventOutcome.DownstreamUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, result),
            SubmitEventOutcome.DownstreamRejected => StatusCode(StatusCodes.Status502BadGateway, result),
            _ => StatusCode(500, result)
        };
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEventByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetEventsByAccount([FromQuery] string account, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(account))
            return BadRequest(new { error = "account query parameter is required" });

        var result = await _mediator.Send(new GetEventsByAccountQuery(account), ct);
        return Ok(result);
    }
}

public record SubmitEventRequest(
    string EventId,
    string AccountId,
    TransactionType Type,
    decimal Amount,
    string Currency,
    DateTimeOffset EventTimestamp,
    Dictionary<string, object>? Metadata);