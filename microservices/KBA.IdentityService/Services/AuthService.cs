using KBA.Framework.Domain.Entities;
using KBA.IdentityService.Data;
using KBA.IdentityService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace KBA.IdentityService.Services;

public interface IAuthService
{
    Task<LoginResultDto> LoginAsync(LoginDto loginDto);
    Task<UserDto> RegisterAsync(RegisterDto registerDto);
}

public class AuthService : IAuthService
{
    private readonly IdentityDbContext _context;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(IdentityDbContext context, JwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto loginDto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == loginDto.UserName || u.Email == loginDto.UserName);

        if (user == null)
            throw new UnauthorizedAccessException("Nom d'utilisateur ou mot de passe incorrect");

        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Nom d'utilisateur ou mot de passe incorrect");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Ce compte est désactivé");

        var token = _jwtTokenService.GenerateToken(user);

        return new LoginResultDto
        {
            Token = token,
            User = MapToUserDto(user),
            ExpiresIn = 3600 // 1 heure en secondes
        };
    }

    public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
    {
        // Vérifier si l'utilisateur existe déjà
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == registerDto.UserName || u.Email == registerDto.Email);

        if (existingUser != null)
            throw new InvalidOperationException("Un utilisateur avec ce nom d'utilisateur ou cet email existe déjà");

        // Créer le nouvel utilisateur
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
        
        var user = new User(
            tenantId: null,
            userName: registerDto.UserName,
            email: registerDto.Email,
            passwordHash: passwordHash,
            firstName: registerDto.FirstName,
            lastName: registerDto.LastName
        );

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return MapToUserDto(user);
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
