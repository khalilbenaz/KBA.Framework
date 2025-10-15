using KBA.Framework.Application.DTOs.Users;
using KBA.Framework.Domain.Entities.Identity;
using KBA.Framework.Domain.Repositories;

namespace KBA.Framework.Application.Services;

/// <summary>
/// Service de gestion des utilisateurs
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserContext _currentUserContext;

    /// <summary>
    /// Constructeur
    /// </summary>
    public UserService(IUserRepository userRepository, ICurrentUserContext currentUserContext)
    {
        _userRepository = userRepository;
        _currentUserContext = currentUserContext;
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user == null ? null : MapToDto(user);
    }

    /// <inheritdoc />
    public async Task<List<UserDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetListAsync(cancellationToken);
        return users.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // Vérifier si l'utilisateur existe déjà
        var existingUser = await _userRepository.GetByUserNameAsync(dto.UserName, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException($"Un utilisateur avec le nom '{dto.UserName}' existe déjà.");

        existingUser = await _userRepository.GetByEmailAsync(dto.Email, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException($"Un utilisateur avec l'email '{dto.Email}' existe déjà.");

        var user = new User(
            tenantId: _currentUserContext.TenantId,
            userName: dto.UserName,
            email: dto.Email
        );

        // Note: Le hachage du mot de passe devrait utiliser un service de hachage approprié
        user.SetPasswordHash(HashPassword(dto.Password));

        if (!string.IsNullOrWhiteSpace(dto.FirstName) || !string.IsNullOrWhiteSpace(dto.LastName) || !string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            user.UpdatePersonalInfo(dto.FirstName, dto.LastName, dto.PhoneNumber);
        }

        var createdUser = await _userRepository.InsertAsync(user, cancellationToken);
        return MapToDto(createdUser);
    }

    /// <inheritdoc />
    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"Utilisateur avec l'id {id} non trouvé.");

        user.UpdatePersonalInfo(dto.FirstName, dto.LastName, dto.PhoneNumber);

        var updatedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        return MapToDto(updatedUser);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"Utilisateur avec l'id {id} non trouvé.");

        await _userRepository.DeleteAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("Le nom d'utilisateur ne peut pas être vide.", nameof(userName));

        var user = await _userRepository.GetByUserNameAsync(userName, cancellationToken);
        return user == null ? null : MapToDto(user);
    }

    /// <summary>
    /// Hache un mot de passe (implémentation simplifiée - à remplacer par un vrai service de hachage)
    /// </summary>
    private static string HashPassword(string password)
    {
        // NOTE: Ceci est une implémentation simplifiée
        // En production, utilisez BCrypt, PBKDF2, ou ASP.NET Core Identity PasswordHasher
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
    }

    /// <summary>
    /// Convertit une entité User en UserDto
    /// </summary>
    private static UserDto MapToDto(User user)
    {
        return new UserDto(
            Id: user.Id,
            TenantId: user.TenantId,
            UserName: user.UserName,
            Email: user.Email,
            EmailConfirmed: user.EmailConfirmed,
            PhoneNumber: user.PhoneNumber,
            PhoneNumberConfirmed: user.PhoneNumberConfirmed,
            TwoFactorEnabled: user.TwoFactorEnabled,
            IsActive: user.IsActive,
            FirstName: user.FirstName,
            LastName: user.LastName,
            FullName: user.FullName,
            CreatedAt: user.CreatedAt
        );
    }
}
