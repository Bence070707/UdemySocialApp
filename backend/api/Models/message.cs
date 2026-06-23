using MongoDB.Bson.Serialization.Attributes;

namespace api.Models;

public class Message
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Content { get; set; } = null!;
    public string Sender { get; set; } = null!;
    public string Receiver { get; set; } = null!;
}