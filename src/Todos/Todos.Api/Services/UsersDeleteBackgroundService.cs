using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Todos.Dal;

namespace Todos.Api.Services
{
    public class UsersDeleteBackgroundService : BackgroundService
    {
        private readonly IDatabase redisDb;
        private readonly ITodosRepository repository;

        public UsersDeleteBackgroundService(ConnectionMultiplexer redisConnection, ITodosRepository repository)
        {
            this.redisDb = redisConnection.GetDatabase();
            this.repository = repository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // keep re-running the task while the app is running
            while (!stoppingToken.IsCancellationRequested)
            {
                // try get an element from the queue, but do NOT remove
                var popResult = await redisDb.SetRandomMemberAsync("DeleteUserQueue").ConfigureAwait(false);
                if (!popResult.IsNullOrEmpty && popResult.TryParse(out int userId))
                {
                    // process the task/command
                    await repository.DeleteAllOfUser(userId).ConfigureAwait(false);
                    // only when the processing is complete do we remove the command from the queue
                    await redisDb.SetRemoveAsync("DeleteUserQueue", userId).ConfigureAwait(false);
                }

                await Task.Delay(System.TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            }
        }
    }
}
