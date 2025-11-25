using AutoMapper;
public class CommentProfile : Profile
{
  public CommentProfile()
  {
    CreateMap<Comment, CreateCommentDto>().ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.UserId));
    CreateMap<CreateCommentDto, Comment>().ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.AuthorId));
    CreateMap<Comment, CommentDto>();
    CreateMap<Comment, CommentTreeDto>();
  }
}
