using MongoDB.Bson.Serialization.Attributes;

namespace api.Models;

public class UnreadMessage
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string? Id { get; set; }
    public string MainUserId { get; set; } = null!;
    public string OtherUsedId { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public int NumOfUnreadMessages { get; set; } = 0;
}