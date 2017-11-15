using System.Threading;
using Starcounter;
using Starcounter.Authorization.Routing;

namespace Joozek78.Star.AsyncHandlers.Examples
{
    public class Program
    {
        static void Main()
        {
            Application.Current.Use(new HtmlFromJsonProvider());
            Application.Current.Use(new PartialToStandaloneHtmlProvider());

            Router.CreateDefault().RegisterAllFromCurrentAssembly();
        }
    }
}