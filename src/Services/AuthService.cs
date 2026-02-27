using IdentityService.Data;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;

public class AuthService(IdentityDbContext db, TokenService tokenService)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        var exists = await db.Users.AnyAsync(u => u.Email == req.Email.ToLower());
        if (exists)
            throw new InvalidOperationException("Email já cadastrado.");

        var user = new User
        {
            Name = req.Name.Trim(),
            Email = req.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (token, expiresIn) = tokenService.Generate(user);
        return new AuthResponse(token, "Bearer", expiresIn, user.Id, user.Name);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciais inválidas.");

        var (token, expiresIn) = tokenService.Generate(user);
        return new AuthResponse(token, "Bearer", expiresIn, user.Id, user.Name);
    }
}
