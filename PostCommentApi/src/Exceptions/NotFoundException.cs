namespace PostCommentApi.Exceptions;

public class NotFoundException(string name, object key) : Exception($"{name} with id '{key}' was not found.");