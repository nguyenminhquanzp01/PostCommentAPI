using Microsoft.EntityFrameworkCore;
using PostCommentApi.Dtos;

namespace PostCommentApi.Services;

public class AuthService(AppDb db, TokenProvider tokenProvider)
{
  public async Task<string> Authenticate(string username, string password)
  {
    var user = await db.Users
      .FirstOrDefaultAsync(u => u.UserName == username && u.Password == password);

    if (user == null)
    {
      throw new UnauthorizedAccessException("Invalid username or password.");
    }
    return tokenProvider.Create(user);
  }

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
}