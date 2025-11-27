using AutoMapper;
using PostCommentApi.Dtos;
using PostCommentApi.Entities;

namespace PostCommentApi.AutoMapper;

public class PostProfile : Profile
{
  public PostProfile()
  {
    CreateMap<Post, PostDto>();
    CreateMap<CreatePostDto, Post>();
  }
}