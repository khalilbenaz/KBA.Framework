using KBA.Framework.Domain.Common;

namespace KBA.Framework.Domain.Entities;

/// <summary>
/// Table de liaison User-Role
/// </summary>
public class UserRole : Entity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}
