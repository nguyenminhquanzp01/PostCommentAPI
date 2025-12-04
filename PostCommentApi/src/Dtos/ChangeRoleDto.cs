using PostCommentApi.Entities;

namespace PostCommentApi.Dtos;

public class ChangeRoleDto
{
  public PageRole NewRole { get; set; }
}