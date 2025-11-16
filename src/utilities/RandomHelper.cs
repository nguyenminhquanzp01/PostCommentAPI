public static class RandomHelper
{
  private static readonly Random _rand = new Random();
  private const string chars = "abcdefghijklmnopqrstuvwxyz";

  public static string RandomString(int length)
  {
    return new string(Enumerable.Repeat(chars, length)
        .Select(s => s[_rand.Next(s.Length)]).ToArray());
  }

  public static int RandomInt(int min, int max)
  {
    return _rand.Next(min, max + 1);
  }
}

