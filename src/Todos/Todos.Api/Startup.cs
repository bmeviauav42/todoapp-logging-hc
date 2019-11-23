using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddControllers();

            // adds all the database related component
            services.AddTodosDal(Configuration);

            // adds the caching related components
            services.AddTodosCache(Configuration);

            // adds background service for handling deleted users
            services.AddUsersDeleteService(Configuration);

            // only when run in debug mode: prefills the database with sample data
            if (HostingEnvironment.IsDevelopment())
                services.AddHostedService<TestDataProvider>();
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
            });
        }
    }
}
