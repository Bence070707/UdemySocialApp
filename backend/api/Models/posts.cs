using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.Models;

public class Post
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]

    public string? Id { get; set; }
    [BsonElement("Title")]
    public string? Title { get; set; }
    public string? Creator { get; set; }
    [BsonElement("Message")]
    public string? Message { get; set; }
    public string? SelectedFile { get; set; }
    public List<string> Likes { get; set; } = [];
    public List<string> Comments { get; set; } = [];
    public DateTime? CreatedAt { get; set; } = DateTime.Now;
}