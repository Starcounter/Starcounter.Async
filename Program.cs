using System;
using Starcounter;
using Starcounter.Authorization.Routing;

namespace StarcounterApplication4
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