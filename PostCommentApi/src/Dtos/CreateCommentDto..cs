namespace PostCommentApi.Dtos;

public record CreateCommentDto(int? ParentId, string Content, int AuthorId);