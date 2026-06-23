using api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api.Services;

public class NotificationService
{
    private readonly IMongoCollection<Notification> _notificationCollection;

    public NotificationService(IOptions<MongoDbSettings> options)
    {
        MongoClient client = new(options.Value.ConnectionString);
        IMongoDatabase database = client.GetDatabase(options.Value.DatabaseName);
        _notificationCollection = database.GetCollection<Notification>(options.Value.NotificationCollection);
    }

    public async Task CreateNotification(Notification notification)
    {
        await _notificationCollection.InsertOneAsync(notification);
        // todo call realtime notification grpc

        return;
    }

    public async Task<List<Notification>> GetUserNotifications(string uid)
    {
        var filter = Builders<Notification>.Filter.Regex(x => x.MainUserId, new BsonRegularExpression(uid, "i"));

        var notifications = await _notificationCollection.Find(filter)
        .SortByDescending(p => p.CreatedAt)
        .ToListAsync();

        return notifications;
    }

    public async Task<bool> MarkNotificationRead(string uid)
    {
        var filter = Builders<Notification>.Filter.Regex(x => x.MainUserId, new BsonRegularExpression(uid, "i"));

        var update = Builders<Notification>.Update
        .Set(x => x.IsRead, true);

        var result = _notificationCollection.UpdateManyAsync(filter, update);

        return result != null;
    }
}