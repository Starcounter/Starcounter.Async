using Starcounter.Startup;

namespace Joozek78.Star.Async.Examples
{
    public class Program
    {
        static void Main()
        {
            DefaultStarcounterBootstrapper.Start(new Startup());
        }
    }
}