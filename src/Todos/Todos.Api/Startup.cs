using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

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
                services.AddHealthChecksUI(setupSettings: settings =>
                {
                    settings.AddHealthCheckEndpoint("readiness", "http://localhost:5081/health/ready");
                    settings.AddHealthCheckEndpoint("liveness", "http://localhost:5081/health/live");
                });
            }

            services.AddHealthChecks()
                .AddCheck("readiness", () => HealthCheckResult.Healthy())
                .AddRedis(Configuration.GetValue<string>("RedisUrl") ?? "redis:6379", tags: new[] { "liveness" })
                .AddElasticsearch(Configuration.GetValue<string>("ElasticsearchUrl") ?? "http://elasticsearch:9200", tags: new[] { "liveness" });
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
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions 
                {
                    Predicate = r => r.Name.Contains("readiness"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("liveness"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                if (env.IsDevelopment())
                    endpoints.MapHealthChecksUI();
            });
        }
    }
}
