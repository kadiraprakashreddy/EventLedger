namespace EventGateway.Domain.Exceptions;

public class InvalidEventException : DomainException
{
    public InvalidEventException(string message) : base(message) { }
}