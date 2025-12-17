using PostCommentApi;

public class TestService
{
  public string Name { get; set; }
  public int Age { get; set; }
  public void Initialize(string name, int age)
  {
    Name = name;
    Age = age;
  }
  
}