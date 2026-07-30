namespace EventGateway.Domain.Enums;

public enum EventStatus
{
    Received,   // stored at Gateway, not yet confirmed applied downstream
    Applied,    // AccountService confirmed it processed the transaction
    Failed      // AccountService rejected it or was unreachable
}