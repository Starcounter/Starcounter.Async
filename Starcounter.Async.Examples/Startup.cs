using Microsoft.Extensions.DependencyInjection;
using Starcounter.Startup.Abstractions;
using Starcounter.Startup.Routing;

namespace Starcounter.Async.Examples
{
    public class Startup : IStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouter();
        }

        public void Configure(IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.ApplicationServices.GetRouter().RegisterAllFromCurrentAssembly();
        }
    }
}