namespace CRM.Domain.Exceptions;

public sealed class DuplicateEntityException : DomainException
{
    public DuplicateEntityException(string entityName, string field, object value)
        : base($"A {entityName} with {field} '{value}' already exists.")
    {
        EntityName = entityName;
        Field = field;
        Value = value;
    }

    public string EntityName { get; }

    public string Field { get; }

    public object Value { get; }
}
