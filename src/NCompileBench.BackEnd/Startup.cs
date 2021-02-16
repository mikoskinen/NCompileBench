using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NCompileBench.Backend.Infrastructure;
using Weikio.ApiFramework.AspNetCore;
using Weikio.EventFramework.AspNetCore.Extensions;
using Weikio.EventFramework.EventGateway.Http;
using Weikio.EventFramework.Extensions.EventAggregator;

namespace NCompileBench.Backend
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
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });
            services.AddControllers();
            services.AddTransient<Decryptor>();
            services.AddSingleton(new BlobServiceClient(_configuration["Storage:ConnectionString"]));
            services.AddSingleton<BlobFileService>();

            services.AddApiFramework()
                .AddApi<ResultApi>("/results");

            services.AddEventFramework()
                .AddHttpGateway()
                .AddHandler<ResultHandler>();

            services.AddOpenApiDocument(settings =>
            {
                settings.Title = "NCompileBench Backend";
                settings.ApiGroupNames = new[] { "api_framework_endpoint" };
            });
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
            app.UseCors();

            app.UseAuthorization();
            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
