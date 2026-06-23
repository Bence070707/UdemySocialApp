using MongoDB.Bson.Serialization.Attributes;

namespace api.Models;

public class Notification
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string Details { get; set; } = null!;
    public string MainUserId { get; set; } = null!;
    public string TargetId { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public UserIn User { get; set; } = default!;
}

public class UserIn
{
    public string Name { get; set; } = null!;
    public string Avatar { get; set; } = null!;

}