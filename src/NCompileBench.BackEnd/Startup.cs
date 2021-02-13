using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Weikio.ApiFramework.AspNetCore;
using Weikio.ApiFramework.AspNetCore.StarterKit;
using Weikio.EventFramework.AspNetCore.Extensions;
using Weikio.EventFramework.EventGateway.Http;
using Weikio.EventFramework.Extensions.EventAggregator;

namespace NCompileBench.BackEnd
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddTransient<Decryptor>();
            services.AddSingleton(new BlobServiceClient(_configuration["Storage:ConnectionString"]));
            services.AddSingleton<BlobFileService>();

            services.AddEventFramework()
                .AddHttpGateway()
                .AddHandler<ResultHandler>();
            
            services.AddApiFrameworkWithAdmin()
                .AddApi<ResultApi>("/results");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
