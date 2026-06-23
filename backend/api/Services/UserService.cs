using api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace api.Services;

public class UserService
{
    private readonly IMongoCollection<User> _userCollection;
    public UserService(IOptions<MongoDbSettings> mongoDBSettings)
    {
        MongoClient client = new(mongoDBSettings.Value.ConnectionString);

        IMongoDatabase database = client.GetDatabase(mongoDBSettings.Value.DatabaseName);

        _userCollection = database.GetCollection<User>(mongoDBSettings.Value.UserCollection);
    }

    public async Task CreateAsync(User user)
    {
        await _userCollection.InsertOneAsync(user);
        return;
    }
    
    public async Task<User?> GetUserByEmail(string email)
    {
        return await _userCollection.Find(x => x.Email == email).FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserById(string id)
    {
        return await _userCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User?> UpdateUser(string id, User newUser)
    {
        return await _userCollection.FindOneAndReplaceAsync(x => x.Id == id, newUser);
    }

    public async Task DeleteAsync(string id)
    {
        FilterDefinition<User> filter = Builders<User>.Filter.Eq("Id", id);
        await _userCollection.DeleteOneAsync(filter);
        return;
    }
}