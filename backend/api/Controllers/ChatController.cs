using api.Interfaces;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Controller]
[Route("/chat")]

public class ChatController(IConfiguration configuration, ChatService chatService) : Controller
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ChatService _chatService = chatService;

    [HttpPost]
    [Route("sendmessage"), Authorize]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageInterface body)
    {
        if(body.Content == null ||body.Receiver == null || body.Sender == null) return BadRequest();
        var msg = new Message
        {
            Content = body.Content,
            Sender = body.Sender,
            Receiver = body.Receiver
        };

        await _chatService.SendMessageAsync(msg, body.Sender, body.Receiver);
        if(msg == null)
        {
            return BadRequest();
        }
        return Ok(new {success = true});
    }

    [HttpGet]
    [Route("getmessagesbynumbers")]
    public async Task<IActionResult> GetMessagesByNumsBetweenTwoUsers([FromQuery] string from, [FromQuery] string firstuid, [FromQuery] string seconduid)
    {
        if(string.IsNullOrEmpty(from) || string.IsNullOrEmpty(firstuid) || string.IsNullOrEmpty(seconduid))
        {
            return BadRequest(new {message = "Problem with provided query parameters."});
        }

        List<Message> messages = await _chatService.GetMessageByNumber(int.Parse(from), firstuid, seconduid);

        return Ok(new {messages});
    }

    [HttpGet]
    [Route("get-user-unreadmessages")]
    public async Task<IActionResult> GetUserUnreadMessage([FromQuery]string userid)
    {
        if (string.IsNullOrEmpty(userid))
        {
            return BadRequest(new {message = "Problem with provided query parameters."});
        }

        List<UnreadMessage> unreadMessages = await _chatService.GetUserUnreadMessages(userid);

        int totalUnreadMessagesCount = unreadMessages.Sum(x => x.NumOfUnreadMessages);

        return Ok(new {messages = unreadMessages, total = totalUnreadMessagesCount});
    }

    [HttpGet]
    [Route("mark-message-read")]
    public async Task<IActionResult> MarkMessageAsRead([FromQuery]string mainuid, [FromQuery]string otheruid)
    {
        if (string.IsNullOrEmpty(mainuid) || string.IsNullOrEmpty(otheruid))
        {
            return BadRequest(new {message = "Problem with provided query parameters."});
        }

        bool isMarked = await _chatService.MarkMessagesAsRead(otheruid, mainuid);
        return Ok(new {isMarked});
    }
}