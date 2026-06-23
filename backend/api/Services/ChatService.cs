using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace api.Services;

public class ChatService
{
    private readonly IMongoCollection<UnreadMessage> _unreadMessageCollection;
    private readonly IMongoCollection<Message> _messageCollection;
    private readonly IMongoCollection<User> _userCollection;

    public ChatService(IOptions<MongoDbSettings> options)
    {
        MongoClient client = new(options.Value.ConnectionString);
        IMongoDatabase database = client.GetDatabase(options.Value.DatabaseName);

        _unreadMessageCollection = database.GetCollection<UnreadMessage>(options.Value.UnMessageCollection);

        _messageCollection = database.GetCollection<Message>(options.Value.MessageCollection);

        _userCollection = database.GetCollection<User>(options.Value.UserCollection);

    }

    public async Task SendMessageAsync(Message message, string sender, string receiver)
    {
        await _messageCollection.InsertOneAsync(message);

        await SetUpdateUnreadMessageBetweenUsers(sender, receiver);

        return;
    }

    private async Task SetUpdateUnreadMessageBetweenUsers(string sender, string receiver)
    {
        var filter = Builders<UnreadMessage>.Filter.And(
            Builders<UnreadMessage>.Filter.Eq(x => x.MainUserId, receiver),
            Builders<UnreadMessage>.Filter.Eq(x => x.OtherUsedId, sender)
        );
        var update = Builders<UnreadMessage>.Update
        .Set(x => x.IsRead, false)
        .Inc(x => x.NumOfUnreadMessages, 1);

        var options = new FindOneAndUpdateOptions<UnreadMessage>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var result = await _unreadMessageCollection.FindOneAndUpdateAsync(filter, update, options);

        if (result == null)
        {
            var newUnreadMessage = new UnreadMessage
            {
                MainUserId = receiver,
                OtherUsedId = sender,
                IsRead = false,
                NumOfUnreadMessages = 1
            };

            await _unreadMessageCollection.InsertOneAsync(newUnreadMessage);
        }
    }

    public async Task<List<Message>> GetMessageByNumber(int from, string firstuid, string seconduid)
    {
        var senderFilter = Builders<Message>.Filter.Eq(x => x.Sender, firstuid);
        var receiverFilter = Builders<Message>.Filter.Eq(x => x.Receiver, seconduid);

        var senderFilter1 = Builders<Message>.Filter.Eq(x => x.Receiver, firstuid);
        var receiverFilter1 = Builders<Message>.Filter.Eq(x => x.Sender, seconduid);

        var combinedFilter = Builders<Message>.Filter.Or(
            Builders<Message>.Filter.And(senderFilter, receiverFilter),
            Builders<Message>.Filter.And(senderFilter1, receiverFilter1)
        );

        var sort = Builders<Message>.Sort.Descending(x => x.Id);
        var numOfReturningMessages = 8;
        var messages = await _messageCollection
        .Find(combinedFilter)
        .Sort(sort)
        .Skip(from * numOfReturningMessages)
        .Limit(numOfReturningMessages)
        .ToListAsync();

        messages.Reverse();
        return messages;
    }

    public async Task<List<UnreadMessage>> GetUserUnreadMessages(string userid)
    {
        var filter1 = Builders<UnreadMessage>.Filter.Eq(x => x.MainUserId, userid);
        var filter2 = Builders<UnreadMessage>.Filter.Eq(x => x.IsRead, false);

        var combinedFilter = Builders<UnreadMessage>.Filter.And(filter1, filter2);

        var unreadMessages = await _unreadMessageCollection.Find(combinedFilter).ToListAsync();

        return unreadMessages;
    }

    public async Task<bool> MarkMessagesAsRead(string otheruid, string mainuid)
    {
        var filter = Builders<UnreadMessage>.Filter.And(
            Builders<UnreadMessage>.Filter.Eq(x => x.MainUserId, mainuid),
            Builders<UnreadMessage>.Filter.Eq(x => x.OtherUsedId, otheruid)
        );

        var update = Builders<UnreadMessage>.Update
        .Set(x => x.IsRead, true)
        .Set(x => x.NumOfUnreadMessages, 0);

        var result = await _unreadMessageCollection.UpdateOneAsync(filter, update);

        return result.MatchedCount > 0;
    }
}
