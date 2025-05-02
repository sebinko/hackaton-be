using MongoDB.Driver;
using API.DataModels;

namespace API.Data
{
    public class MongoDbService
    {
        private readonly IMongoCollection<Thought> _thoughtsCollection;

        public MongoDbService(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _thoughtsCollection = database.GetCollection<Thought>("Thoughts");
        }

        public async Task<List<Thought>> GetAllThoughtsAsync()
        {
            return await _thoughtsCollection.Find(_ => true).ToListAsync();
        }

        public async Task<Thought> GetThoughtByIdAsync(string id)
        {
            return await _thoughtsCollection.Find(t => t.ThoughtId == id).FirstOrDefaultAsync();
        }

        public async Task CreateThoughtAsync(Thought thought)
        {
            await _thoughtsCollection.InsertOneAsync(thought);
        }

        public async Task UpdateThoughtAsync(string id, Thought updatedThought)
        {
            await _thoughtsCollection.ReplaceOneAsync(t => t.ThoughtId == id, updatedThought);
        }

        public async Task DeleteThoughtAsync(string id)
        {
            await _thoughtsCollection.DeleteOneAsync(t => t.ThoughtId == id);
        }
    }
}