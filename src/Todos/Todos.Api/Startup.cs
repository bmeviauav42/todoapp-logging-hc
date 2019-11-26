using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace Todos.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; }
        public bool IsLive { get; private set; } = true;

        public void ConfigureServices(IServiceCollection services)
        {
            // for the routing of requests
            services.AddControllers()
                .AddNewtonsoftJson();

            // adds all the database related component
            services.AddTodosDal(Configuration);

            // adds the caching related components
            services.AddTodosCache(Configuration);

            // adds background service for handling deleted users
            services.AddUsersDeleteService(Configuration);

            // only when run in debug mode: prefills the database with sample data
            if (HostingEnvironment.IsDevelopment())
            {
                services.AddHostedService<TestDataProvider>();
                //services.AddHealthChecksUI(setupSettings: settings =>
                //{
                //    settings.AddHealthCheckEndpoint("liveness", "http://localhost:5081/health/live");
                //    settings.AddHealthCheckEndpoint("readiness", "http://localhost:5081/health/ready");
                //});
            }

            services.AddHealthChecks()
                .AddCheck("liveness", () => IsLive ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy())
                .AddRedis(Configuration.GetValue<string>("RedisUrl") ?? "redis:6379", tags: new[] { "readiness" })
                .AddElasticsearch(Configuration.GetValue<string>("ElasticsearchUrl") ?? "http://elasticsearch:9200", tags: new[] { "readiness" });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // only when run in debug mode: allow CORS to make frontend development simpler
            if (env.IsDevelopment())
                app.UseCors(config => config.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            // for the routing of requests
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/api/switch", async r =>
                {
                    IsLive = !IsLive;
                    await r.Response.WriteAsync($"IsLive is now {IsLive}");
                });
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/api/health/live", new HealthCheckOptions 
                {
                    Predicate = r => r.Name.Contains("liveness"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/api/health/ready", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("readiness"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                //if (env.IsDevelopment())
                //    endpoints.MapHealthChecksUI();
            });
        }
    }
}
