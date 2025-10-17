namespace KBA.Framework.Domain.Common;

/// <summary>
/// Racine d'agrégat pour le pattern DDD
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<object> _domainEvents = new();

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(object eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
