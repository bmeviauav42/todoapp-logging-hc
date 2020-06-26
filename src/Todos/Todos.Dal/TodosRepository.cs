using System.Linq;
using System.Threading.Tasks;
using Nest;

namespace Todos.Dal
{
    internal class TodosRepository : ITodosRepository
    {
        private readonly ElasticClient elasticClient;

        public TodosRepository(ElasticClient elasticClient)
        {
            this.elasticClient = elasticClient;
        }

        public async Task<SearchTodoResult> Search(int? userId = null, string searchExpression = null)
        {
            // The query description is somewhat unconvential in NEST.
            // - The syntax is Fluent, follows how Elasticsearch thinks.
            // - Combining multiple search criteria must be done with && operator (or ||).
            // - Size limits the maximum of items returned. This is a must with Elasticsearch. If unspecified, default is 10.

            var result = await elasticClient
                                .SearchAsync<Entities.TodoItem>(searchDescriptor =>
                                    searchDescriptor
                                    .Query(queryDescriptor => filterForUserId(userId, queryDescriptor) && filterForText(searchExpression, queryDescriptor)) // filtering
                                    .Sort(sortDescriptor => sortDescriptor.Descending(SortSpecialField.Score)) // sort by text search relevance
                                    .Size(5));

            return new SearchTodoResult(
                items: result.Hits.Select(i => i.Source.ToDomain(i.Id)).ToList(), // The id property comes from Elasticsearch' hit object; see link in the description of the entity class TodoItem
                count: result.Total // Although only the first few items are returned, Elasticsearch tells us how many are there in total
                );
        }

        private static QueryContainer filterForText(string searchExpression, QueryContainerDescriptor<Entities.TodoItem> queryDescriptor)
            => string.IsNullOrEmpty(searchExpression)
                ? queryDescriptor.MatchAll()
                : queryDescriptor.Match(match => match.Field(f => f.Title).Query(searchExpression)); //  Match query will look for similar texts (full text search)

        private static QueryContainer filterForUserId(int? userId, QueryContainerDescriptor<Entities.TodoItem> queryDescriptor)
            => userId == null
                ? queryDescriptor.MatchAll()
                : queryDescriptor.Term(term => term.Field(f => f.UserId).Value(userId.Value)); // a Term query will look for eact match

        public async Task<TodoItem> FindById(string id)
        {
            var result = await elasticClient.GetAsync<Entities.TodoItem>(id);
            if (!result.Found)
                return null;
            else
                return result.Source.ToDomain(result.Id);
        }

        public async Task<TodoItem> Insert(CreateNewTodoRequest value)
        {
            // This operation is ***NOT*** idempotent!
            var result = await elasticClient.IndexDocumentAsync(value.ToDal());
            return await FindById(result.Id);
        }

        public async Task<TodoItem> Update(string id, EditTodoRequest value)
        {
            // This operation is idempotent!
            var result = await elasticClient.UpdateAsync<Entities.TodoItem, Entities.TodoItemChanges>(id, a => a.Doc(value.ToDal()).DocAsUpsert(false));
            if (result.Result == Result.Updated)
                return await FindById(result.Id);
            else
                return null;
        }

        public async Task<bool> Delete(string id)
        {
            // This operation is idempotent!
            var result = await elasticClient.DeleteAsync<Entities.TodoItem>(id);
            return result.Result == Result.Deleted;
        }

        public Task DeleteAllOfUser(int userId)
        {
            // This operation is mostly idempotent, but is not protected from concurrent delete and insert!
            return elasticClient.DeleteByQueryAsync<Entities.TodoItem>(q =>
                q.Query(queryDescriptor => filterForUserId(userId, queryDescriptor))
                .Conflicts(Elasticsearch.Net.Conflicts.Proceed));
        }
    }
}
