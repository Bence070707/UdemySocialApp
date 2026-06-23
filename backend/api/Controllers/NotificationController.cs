using api.Models;
using api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace api.Controllers;

[Controller]
[Route("/notification")]
public class NotificationController(NotificationService notificationService) : Controller
{
    private readonly NotificationService _notificationService = notificationService;

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetUserNotification([FromRoute] string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest(new {message = "Problem with provided id."});
        }

        List<Notification> notifications = await _notificationService.GetUserNotifications(id);

        if (notifications.IsNullOrEmpty())
        {
            return NotFound(new {message = "No notification found.", success = false});
        }

        return Ok(new{ notifications});
    }

    [HttpGet]
    [Route("mark-notification-asread")]
    public async Task<IActionResult> MarkNotificationAsRead([FromQuery] string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest(new {message = "Problem with provided id."});
        }

        bool isMarked = await _notificationService.MarkNotificationRead(id);

        if (!isMarked)
        {
            return BadRequest(new {message = "Problem marking the notification read."});
        }

        List<Notification> notifications = await _notificationService.GetUserNotifications(id);

        if (notifications.IsNullOrEmpty())
        {
            return NotFound(new {message = "No notification found.", success = false});
        }

        return Ok(new{ notifications});
    }

}