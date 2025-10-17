using KBA.Framework.Domain.Entities;
using KBA.IdentityService.Data;
using KBA.IdentityService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace KBA.IdentityService.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto);
    Task DeleteAsync(Guid id);
}

public class UserService : IUserService
{
    private readonly IdentityDbContext _context;

    public UserService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _context.Users.ToListAsync();
        return users.Select(MapToUserDto).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        return user == null ? null : MapToUserDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == dto.UserName || u.Email == dto.Email);

        if (existingUser != null)
            throw new InvalidOperationException("Un utilisateur avec ce nom d'utilisateur ou cet email existe déjà");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        
        var user = new User(
            tenantId: dto.TenantId,
            userName: dto.UserName,
            email: dto.Email,
            passwordHash: passwordHash,
            firstName: dto.FirstName,
            lastName: dto.LastName
        );

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return MapToUserDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"Utilisateur avec l'ID {id} introuvable");

        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id);
            if (emailExists)
                throw new InvalidOperationException("Cet email est déjà utilisé");
            user.UpdateEmail(dto.Email);
        }

        if (!string.IsNullOrEmpty(dto.FirstName))
            user.UpdateName(dto.FirstName, user.LastName);

        if (!string.IsNullOrEmpty(dto.LastName))
            user.UpdateName(user.FirstName, dto.LastName);

        if (dto.IsActive.HasValue)
        {
            if (dto.IsActive.Value)
                user.Activate();
            else
                user.Deactivate();
        }

        await _context.SaveChangesAsync();
        return MapToUserDto(user);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"Utilisateur avec l'ID {id} introuvable");

        user.Delete();
        await _context.SaveChangesAsync();
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            TenantId = user.TenantId
        };
    }
}
