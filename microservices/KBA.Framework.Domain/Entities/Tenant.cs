using KBA.Framework.Domain.Common;

namespace KBA.Framework.Domain.Entities;

/// <summary>
/// Entité tenant pour le multi-tenancy
/// </summary>
public class Tenant : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ConnectionString { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public Dictionary<string, string> Settings { get; set; } = new();

    public Tenant()
    {
    }

    public Tenant(string name)
    {
        Name = name;
        IsActive = true;
    }

    public void ChangeName(string newName)
    {
        Name = newName;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void Delete()
    {
        MarkAsDeleted();
    }
}
