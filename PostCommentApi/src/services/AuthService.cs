using Microsoft.EntityFrameworkCore;
using PostCommentApi.Dtos;

namespace PostCommentApi.Services;

using PostCommentApi.Dtos;

public class AuthService(AppDb db, TokenProvider tokenProvider)
{
  /// <summary>
  /// Authenticate a user by username and password and return a JWT token on success.
  /// </summary>
  /// <param name="username">Username of the user.</param>
  /// <param name="password">Plain-text password (should be hashed in production).</param>
  /// <returns>JWT token string representing the authenticated user.</returns>
  /// <exception cref="UnauthorizedAccessException">When credentials are invalid.</exception>
  public async Task<AuthenticationResultDto> Authenticate(string username, string password)
  {
    var user = await db.Users
      .FirstOrDefaultAsync(u => u.UserName == username && u.Password == password);

    if (user == null)
    {
      throw new UnauthorizedAccessException("Invalid username or password.");
    }
    var accessToken = tokenProvider.Create(user);
    var (refreshToken, expiresAt) = await tokenProvider.CreateRefreshTokenAsync(db, user.Id);
    return new AuthenticationResultDto(accessToken, refreshToken, expiresAt);
  }

  /// <summary>
  /// Register a new user and return a JWT token for convenience.
  /// </summary>
  /// <param name="dto">Registration payload containing username, password, email and name.</param>
  /// <returns>JWT token for the newly created user.</returns>
  /// <exception cref="ExistsException">When username or email already exists.</exception>
  public async Task<string> Register(RegisterDto dto)
  {
    // Ensure username uniqueness
    var isUserNameExist = await db.Users.AnyAsync(u => u.UserName == dto.UserName);
    var isEmailExist = await db.Users.AnyAsync(u => u.Email == dto.Email);
    if (isUserNameExist || isEmailExist) throw new ExistsException("user or email", "");

    var user = new Entities.User
    {
      UserName = dto.UserName,
      Password = dto.Password,
      Name = dto.Name,
      Email = dto.Email,
      IsAdmin = false
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    // Return token for convenience after registration
    return tokenProvider.Create(user);
  }

  /// <summary>
  /// Exchange an (expired) access token + refresh token pair for a new pair.
  /// Validates that the refresh token exists, belongs to the token subject, is not expired and not revoked.
  /// Performs rotation: issues a new refresh token and revokes the old one.
  /// </summary>
  public async Task<AuthenticationResultDto> RefreshTokens(string accessToken, string refreshToken)
  {
    var principal = tokenProvider.GetPrincipalFromToken(accessToken, validateLifetime: false);
    if (principal == null) throw new UnauthorizedAccessException("Invalid access token.");

    var sub = principal.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(sub) || !int.TryParse(sub, out var userId))
      throw new UnauthorizedAccessException("Invalid token subject.");

    var stored = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken && r.UserId == userId);
    if (stored == null) throw new UnauthorizedAccessException("Refresh token not found.");
    if (stored.RevokedAtUtc.HasValue) throw new UnauthorizedAccessException("Refresh token revoked.");
    if (stored.ExpiresAtUtc <= DateTime.UtcNow) throw new UnauthorizedAccessException("Refresh token expired.");

    var user = await db.Users.FindAsync(userId) ?? throw new UnauthorizedAccessException("User not found.");

    var newAccess = tokenProvider.Create(user);
    var (newRefresh, newExpires) = await tokenProvider.CreateRefreshTokenAsync(db, userId);

    stored.RevokedAtUtc = DateTime.UtcNow;
    stored.ReplacedBy = newRefresh;
    await db.SaveChangesAsync();

    return new AuthenticationResultDto(newAccess, newRefresh, newExpires);
  }
}