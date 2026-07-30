using EventGateway.Application.Interfaces;
using EventGateway.Domain.Entities;
using MediatR;

namespace EventGateway.Application.Handlers;

public class SubmitEventCommandHandler : IRequestHandler<Commands.SubmitEventCommand, Commands.SubmitEventResult>
{
    private readonly IEventRepository _repository;
    private readonly IAccountServiceClient _accountServiceClient;

    public SubmitEventCommandHandler(IEventRepository repository, IAccountServiceClient accountServiceClient)
    {
        _repository = repository;
        _accountServiceClient = accountServiceClient;
    }

    public async Task<Commands.SubmitEventResult> Handle(Commands.SubmitEventCommand request, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(request.EventId, ct);
        if (existing is not null)
        {
            return new Commands.SubmitEventResult(
                existing.EventId, Commands.SubmitEventOutcome.Duplicate, existing.Status, null, null);
        }

        var @event = Event.Create(
            request.EventId, request.AccountId, request.Type, request.Amount,
            request.Currency, request.EventTimestamp, request.MetadataJson, request.TraceId);

        await _repository.AddAsync(@event, ct);
        await _repository.SaveChangesAsync(ct);

        var callResult = await _accountServiceClient.ApplyTransactionAsync(
            request.AccountId,
            new ApplyTransactionRequest(request.EventId, request.Type, request.Amount, request.Currency, request.EventTimestamp),
            request.TraceId,
            ct);

        switch (callResult.Status)
        {
            case AccountServiceCallStatus.Success:
                @event.MarkApplied();
                await _repository.SaveChangesAsync(ct);
                return new Commands.SubmitEventResult(
                    @event.EventId, Commands.SubmitEventOutcome.Created, @event.Status, callResult.Balance, null);

            case AccountServiceCallStatus.Unavailable:
                @event.MarkFailed();
                await _repository.SaveChangesAsync(ct);
                return new Commands.SubmitEventResult(
                    @event.EventId, Commands.SubmitEventOutcome.DownstreamUnavailable, @event.Status, null, callResult.ErrorMessage);

            default:
                @event.MarkFailed();
                await _repository.SaveChangesAsync(ct);
                return new Commands.SubmitEventResult(
                    @event.EventId, Commands.SubmitEventOutcome.DownstreamRejected, @event.Status, null, callResult.ErrorMessage);
        }
    }
}