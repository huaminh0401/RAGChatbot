using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RAGChatbotMVC.Data;
using RAGChatbotMVC.DTOs;
using RAGChatbotMVC.Models;

namespace RAGChatbotMVC.Services;

public class AuthService : IAuthService
{
    private const string DemoPassword = "Password@123";
    private readonly AppDbContext _db;
    private readonly PasswordHasher<ApplicationUser> _passwordHasher;
    private readonly JwtOptions _jwtOptions;

    public AuthService(AppDbContext db, PasswordHasher<ApplicationUser> passwordHasher, IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponseDTO?> LoginAsync(string userNameOrEmail, string password)
    {
        var normalized = userNameOrEmail.Trim().ToLowerInvariant();
        var user = await _db.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized || u.UserName.ToLower() == normalized);

        if (user == null || !user.IsActive) return null;

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;

        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponseDTO?> RefreshAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens
            .Include(t => t.ApplicationUser)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (stored?.ApplicationUser == null || !stored.IsActive || !stored.ApplicationUser.IsActive) return null;

        stored.RevokedAt = DateTime.Now;
        var response = await IssueTokensAsync(stored.ApplicationUser, saveImmediately: false);
        stored.ReplacedByTokenHash = HashToken(response.RefreshToken);
        await _db.SaveChangesAsync();
        return response;
    }

    public async Task RevokeRefreshTokenAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        if (stored == null || stored.RevokedAt != null) return;

        stored.RevokedAt = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task<ApplicationUser> CreateUserAsync(UserCreateViewModel input)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        var userName = input.UserName.Trim().ToLowerInvariant();

        if (!UserRoles.All.Contains(input.Role))
        {
            throw new InvalidOperationException("Role khong hop le.");
        }

        if (await _db.ApplicationUsers.AnyAsync(u => u.Email == email || u.UserName == userName))
        {
            throw new InvalidOperationException("Email hoac ten dang nhap da ton tai.");
        }

        var user = new ApplicationUser
        {
            Email = email,
            UserName = userName,
            FullName = input.FullName.Trim(),
            Role = input.Role,
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, input.Password);

        _db.ApplicationUsers.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task SeedDefaultUsersAsync()
    {
        await SeedUserAsync("admin@example.com", "admin", "Administrator", UserRoles.Admin);
        await SeedUserAsync("teacher@example.com", "teacher", "Demo Teacher", UserRoles.Teacher);
        await SeedUserAsync("student@example.com", "student", "Demo Student", UserRoles.Student);
    }

    private async Task SeedUserAsync(string email, string userName, string fullName, string role)
    {
        var existing = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == email || u.UserName == userName);
        if (existing != null)
        {
            existing.Email = email;
            existing.UserName = userName;
            existing.FullName = fullName;
            existing.Role = role;
            existing.IsActive = true;
            existing.PasswordHash = _passwordHasher.HashPassword(existing, DemoPassword);
            await _db.SaveChangesAsync();
            return;
        }

        var user = new ApplicationUser
        {
            Email = email,
            UserName = userName,
            FullName = fullName,
            Role = role,
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, DemoPassword);
        _db.ApplicationUsers.Add(user);
        await _db.SaveChangesAsync();
    }

    private async Task<AuthResponseDTO> IssueTokensAsync(ApplicationUser user, bool saveImmediately = true)
    {
        var expiresAt = DateTime.Now.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var accessToken = CreateAccessToken(user, expiresAt);
        var refreshToken = CreateSecureToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            ApplicationUserId = user.Id,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = DateTime.Now.AddDays(_jwtOptions.RefreshTokenDays)
        });

        if (saveImmediately)
        {
            await _db.SaveChangesAsync();
        }

        return new AuthResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = new AuthUserDTO
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                Role = user.Role
            }
        };
    }

    private string CreateAccessToken(ApplicationUser user, DateTime expiresAt)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string CreateSecureToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
