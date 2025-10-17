namespace KBA.IdentityService.DTOs;

public class LoginDto
{
    public required string UserName { get; set; }
    public required string Password { get; set; }
}

public class RegisterDto
{
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class LoginResultDto
{
    public required string Token { get; set; }
    public required UserDto User { get; set; }
    public int ExpiresIn { get; set; }
}
