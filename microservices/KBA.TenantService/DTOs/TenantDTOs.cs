namespace KBA.TenantService.DTOs;

public class TenantDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; }
}

public class CreateTenantDto
{
    public required string Name { get; set; }
}

public class UpdateTenantDto
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}
