using AutoMapper;
using PostCommentApi.Dtos;
using PostCommentApi.Entities;

namespace PostCommentApi.AutoMapper;

public class CommentProfile : Profile
{
  public CommentProfile()
  {
    // CreateCommentDto no longer contains AuthorId. We'll map fields except UserId which is set in the service.
    CreateMap<Comment, CreateCommentDto>();
    CreateMap<CreateCommentDto, Comment>().ForMember(dest => dest.UserId, opt => opt.Ignore());
    CreateMap<Comment, CommentDto>();
    CreateMap<Comment, CommentTreeDto>();
  }
}