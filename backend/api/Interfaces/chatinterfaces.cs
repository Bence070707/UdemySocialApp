namespace api.Interfaces;

public class SendMessageInterface
{
    public string Content { get; set; } = null!;
    public string Sender { get; set; } = null!;
    public string Receiver { get; set; } = null!;
}