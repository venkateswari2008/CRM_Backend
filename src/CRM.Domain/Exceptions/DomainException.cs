namespace CRM.Domain.Exceptions;

/// <summary>Base type for exceptions that originate from domain-level rule violations.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }

    protected DomainException(string message, Exception inner) : base(message, inner) { }
}
