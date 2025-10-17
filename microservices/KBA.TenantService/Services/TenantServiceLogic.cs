using KBA.Framework.Domain.Entities;
using KBA.TenantService.Data;
using KBA.TenantService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace KBA.TenantService.Services;

public interface ITenantServiceLogic
{
    Task<List<TenantDto>> GetAllAsync();
    Task<TenantDto?> GetByIdAsync(Guid id);
    Task<TenantDto> CreateAsync(CreateTenantDto dto);
    Task<TenantDto> UpdateAsync(Guid id, UpdateTenantDto dto);
    Task DeleteAsync(Guid id);
}

public class TenantServiceLogic : ITenantServiceLogic
{
    private readonly TenantDbContext _context;

    public TenantServiceLogic(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<List<TenantDto>> GetAllAsync()
    {
        var tenants = await _context.Tenants.ToListAsync();
        return tenants.Select(MapToDto).ToList();
    }

    public async Task<TenantDto?> GetByIdAsync(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        return tenant == null ? null : MapToDto(tenant);
    }

    public async Task<TenantDto> CreateAsync(CreateTenantDto dto)
    {
        var existingTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Name == dto.Name);

        if (existingTenant != null)
            throw new InvalidOperationException($"Un tenant avec le nom {dto.Name} existe déjà");

        var tenant = new Tenant(dto.Name);

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        return MapToDto(tenant);
    }

    public async Task<TenantDto> UpdateAsync(Guid id, UpdateTenantDto dto)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            throw new KeyNotFoundException($"Tenant avec l'ID {id} introuvable");

        if (!string.IsNullOrEmpty(dto.Name))
        {
            var nameExists = await _context.Tenants.AnyAsync(t => t.Name == dto.Name && t.Id != id);
            if (nameExists)
                throw new InvalidOperationException("Ce nom de tenant est déjà utilisé");
            
            tenant.ChangeName(dto.Name);
        }

        if (dto.IsActive.HasValue)
        {
            if (dto.IsActive.Value)
                tenant.Activate();
            else
                tenant.Deactivate();
        }

        await _context.SaveChangesAsync();
        return MapToDto(tenant);
    }

    public async Task DeleteAsync(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            throw new KeyNotFoundException($"Tenant avec l'ID {id} introuvable");

        tenant.Delete();
        await _context.SaveChangesAsync();
    }

    private static TenantDto MapToDto(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            IsActive = tenant.IsActive
        };
    }
}
