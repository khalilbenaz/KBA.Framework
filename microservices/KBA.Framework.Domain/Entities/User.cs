using KBA.Framework.Domain.Common;

namespace KBA.Framework.Domain.Entities;

/// <summary>
/// Entité utilisateur pour les microservices
/// </summary>
public class User : AggregateRoot
{
    public Guid? TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public string FullName => $"{FirstName} {LastName}";

    public User()
    {
    }

    public User(string email, string userName, string passwordHash, 
                string firstName, string lastName, Guid? tenantId = null)
    {
        Email = email;
        UserName = userName;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        TenantId = tenantId;
        IsActive = true;
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        MarkAsUpdated();
    }

    public void UpdateEmail(string email)
    {
        Email = email;
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
