using Microsoft.Extensions.Configuration;
using Todos.Api.Cache;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TodosCacheBuilder
    {
        public static void AddTodosCache(this IServiceCollection services, IConfiguration config)
        {
            var redisUrl = config.GetValue<string>("RedisUrl") ?? "redis:6379";

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisUrl;
                options.InstanceName = "todos";
            });

            services.AddTransient<TodosCache>();
        }
    }
}
