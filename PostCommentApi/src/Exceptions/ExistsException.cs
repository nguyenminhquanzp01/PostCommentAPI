public class ExistsException : Exception
{
  public ExistsException(string name, object key) : base($"{name} with id '{key}' already exists.")
  {

  }
}