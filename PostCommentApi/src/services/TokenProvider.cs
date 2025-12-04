using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PostCommentApi.Entities;

namespace PostCommentApi.Services;

public sealed class TokenProvider(IConfiguration configuration)
{
    public string Create(User user)
    {
        var secretKey = configuration["JWT:SECRET"] ?? throw new InvalidOperationException("JWT Key is not configured.");
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        // Create the token descriptor with user claims
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim("isAdmin", user.IsAdmin.ToString())
            }),
            Expires = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("JWT:EXPIRATIONINMINUTES")),
            SigningCredentials = credentials,
            Issuer = configuration["JWT:ISSUER"],
            Audience = configuration["JWT:AUDIENCE"]    
        };
        return new JsonWebTokenHandler().CreateToken(tokenDescriptor);
    }
}