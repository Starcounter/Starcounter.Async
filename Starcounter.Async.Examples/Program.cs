using Starcounter.Startup;

namespace Starcounter.Async.Examples
{
    public class Program
    {
        static void Main()
        {
            DefaultStarcounterBootstrapper.Start(new Startup());
        }
    }
}