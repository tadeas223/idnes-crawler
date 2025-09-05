public class Log
{
  public int VerboseLevel;

  public Log(int verbose_level)
  {
    VerboseLevel = verbose_level;
  }

  public void WriteLine(string text, int verbose_level)
  {
    if(VerboseLevel >= verbose_level)
    {
      Console.WriteLine(text);
    }
  }

}
