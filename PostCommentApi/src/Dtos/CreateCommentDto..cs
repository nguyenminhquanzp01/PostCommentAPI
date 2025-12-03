namespace PostCommentApi.Dtos;

// When creating a comment, the author is determined from the caller's token.
// Admins and regular users both use the caller id. Do not include AuthorId in the request body.
public record CreateCommentDto(int? ParentId, string Content);