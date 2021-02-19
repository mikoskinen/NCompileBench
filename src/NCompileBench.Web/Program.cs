using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NCompileBench.Web.Infastructure;

namespace NCompileBench.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddHttpClient<ResultClient>(options =>
            {
                // For some reason the production configuration is not correctly loaded, so hard code these based on the release/debug configs
                #if DEBUG
                options.BaseAddress = new Uri("https://localhost:5001");
                #else
                options.BaseAddress = new Uri("https://ncompilebench.azurewebsites.net");
                #endif
            });
            
            await builder.Build().RunAsync();
        }
    }
}
