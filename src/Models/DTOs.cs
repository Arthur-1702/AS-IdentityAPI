namespace IdentityService.Models;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Name, string Email, string Password);

public record AuthResponse(string AccessToken, string TokenType, int ExpiresIn, Guid UserId, string Name);
