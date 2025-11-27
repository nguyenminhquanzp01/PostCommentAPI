namespace PostCommentApi.Utilities;

public static class RandomHelper
{
  private static readonly Random Rand = new Random();
  private const string Chars = "abcdefghijklmnopqrstuvwxyz";

  public static string RandomString(int length)
  {
    return new string(Enumerable.Repeat(Chars, length)
      .Select(s => s[Rand.Next(s.Length)]).ToArray());
  }

  public static int RandomInt(int min, int max)
  {
    return Rand.Next(min, max + 1);
  }
}