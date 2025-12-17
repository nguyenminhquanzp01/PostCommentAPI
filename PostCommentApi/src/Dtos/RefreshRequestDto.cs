namespace PostCommentApi.Dtos;

// Client-sent payload to exchange an expired/old access token + refresh token
public record RefreshRequestDto(string AccessToken, string RefreshToken);
