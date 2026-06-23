using api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace api.Services;

public class PostService
{
    private readonly IMongoCollection<User> _userCollection;
    private readonly IMongoCollection<Post> _postCollection;

    public PostService(IOptions<MongoDbSettings> mongoDBSettings)
    {
        MongoClient client = new(mongoDBSettings.Value.ConnectionString);

        IMongoDatabase database = client.GetDatabase(mongoDBSettings.Value.DatabaseName);

        _userCollection = database.GetCollection<User>(mongoDBSettings.Value.UserCollection);
        _postCollection = database.GetCollection<Post>(mongoDBSettings.Value.PostCollection);
    }

    public async Task CreateOnePostAsync(Post post)
    {
        await _postCollection.InsertOneAsync(post);
        return;
    }

    public async Task<Post?> UpdatePost(string id, Post post)
    {
        return await _postCollection.FindOneAndReplaceAsync(x => x.Id == id, post);
    }

    public async Task<Post?> GetPostById(string id)
    {
        return await _postCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserById(string id)
    {
        return await _userCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task DeletePostAsync(string id)
    {
        FilterDefinition<Post> filter = Builders<Post>.Filter.Eq("Id", id);
        await _postCollection.DeleteOneAsync(filter);
    }

    public async Task<(List<Post>, List<User>)> Search(string searchQuery)
    {
        string query = searchQuery.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return ([], []);
        }

        var regex = new BsonRegularExpression(Regex.Escape(query), "i");

        FilterDefinition<Post> FilterPost = Builders<Post>.Filter.Or(
            Builders<Post>.Filter.Regex(post => post.Title, regex),
            Builders<Post>.Filter.Regex(post => post.Message, regex)
        );

        FilterDefinition<User> FilterUser = Builders<User>.Filter.Or(
            Builders<User>.Filter.Regex(user => user.Name, regex),
            Builders<User>.Filter.Regex(user => user.Email, regex)
        );

        List<Post> posts = await _postCollection.Find(FilterPost).ToListAsync();
        List<User> users = await _userCollection.Find(FilterUser).ToListAsync();

        return (posts, users);
    }

    public Object Query(List<string> ids, int? queryPage)
    {
        var filter = Builders<Post>.Filter.In("creator", ids);

        var sort = Builders<Post>.Sort.Descending("Id");
        var find = _postCollection.Find(filter).Sort(sort);

        int currentPage = queryPage.GetValueOrDefault(1) == 0 ? 1 : queryPage.GetValueOrDefault(1);

        int perPage = 3;
        var numberOfPages = find.CountDocuments() / perPage;

        return new
        {
            data = find.Skip((currentPage - 1) * perPage).Limit(perPage).ToList(),
            numberOfPages,
            currentPage
        };
    }
}
