namespace PostCommentApi.Dtos;

public record AuthenticationResultDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);
