using AutoMapper;
public class CommentProfile : Profile
{
  public CommentProfile()
  {
    CreateMap<Comment, CreateCommentDto>();
    CreateMap<CreateCommentDto, Comment>();
    CreateMap<Comment, CommentDto>();
    CreateMap<Comment, CommentTreeDto>();
  }
}