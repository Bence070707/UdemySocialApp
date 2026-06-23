using System.Runtime.CompilerServices;
using System.Security.Claims;
using api.Interfaces;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace api.Controllers;

[Controller]
[Route("/posts")]

public class PostController(IConfiguration configuration, PostService postService, NotificationService notificationService) : Controller
{
    private readonly IConfiguration _configuration = configuration;
    private readonly PostService _postService = postService;
    private readonly NotificationService _notificationService = notificationService;

    [HttpPost]
    [Route(""), Authorize]
    public async Task<IActionResult> CreatePost([FromBody] CreateOrUpdatePostInterface body)
    {
        var post = new Post();

        if (body.Title == null || body.Message == null || body.SelectedFile == null)
        {
            return BadRequest(new { message = "Problem with provided body data." });
        }

        post.Title = body.Title;
        var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);

        post.Creator = userIdToken;
        post.Message = body.Message;
        post.SelectedFile = body.SelectedFile;

        await _postService.CreateOnePostAsync(post);
        if (post == null)
        {
            return BadRequest(new { message = "Something went wrong." });
        }

        return Ok(new { post = post });
    }

    [HttpGet]
    [Route("{id}"), Authorize]
    public async Task<IActionResult> GetPost([FromRoute] string id)
    {
        if (id is null) return BadRequest(new { message = "Problem with provided id." });

        var post = new Post();
        post = await _postService.GetPostById(id);

        if (post is null) return NotFound(new { message = "Post not found.", success = false });

        return Ok(new { post = post });
    }

    [HttpPost]
    [Route("{id}/commentpost"), Authorize]

    public async Task<IActionResult> AddComment([FromRoute] string id, [FromBody] CommentBodyInterface body)
    {
        if (body.Value == null || id is null) return BadRequest(new { message = "Problem with provided body data id or comment value." });

        var post = await _postService.GetPostById(id);
        if (post is null) return NotFound(new { message = "Post not found.", success = false });

        post.Comments.Add(body.Value);

        var npost = await _postService.UpdatePost(id, post);
        if (npost is null) return NotFound(new { message = "Problem with provided value.", success = false });

        var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (post.Creator != null && userIdToken != null)
        {
            var user = new User { };
            user = await _postService.GetUserById(userIdToken);
            if (user is not null)
            {
                var details = user.Name + " commented on your post.";
                var us = new UserIn { Name = user.Name, Avatar = user.ImageUrl };
                var notification = new Notification
                {
                    MainUserId = post.Creator,
                    TargetId = id,
                    Details = details,
                    User = us
                };
                await _notificationService.CreateNotification(notification);
            }



        }
        return Ok(new { data = post });

    }

    [HttpGet]
    [Route("search"), Authorize]
    public async Task<IActionResult> SearchForUsersPost([FromQuery] string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery)) return BadRequest(new { message = "Problem with provided searchquery." });

        var posts = new List<Post>();
        var users = new List<User>();

        (posts, users) = await _postService.Search(searchQuery);

        return Ok(new { posts = posts, user = users });
    }

    [HttpGet]
    [Route("")]
    public async Task<IActionResult> GetPostsPaginationAsync([FromQuery] int page, [FromQuery] string id)
    {
        if (id == "undefined") return BadRequest(new { message = "Problem with provided id." });

        var user = new User { };
        user = await _postService.GetUserById(id);

        if (user is null || user.Id is null)
        {
            return NotFound(new { message = "User with given id is not found" });
        }

        var ids = user.Followings;
        ids.Add(user.Id);

        return Ok(_postService.Query(ids, page));
    }

    [HttpPatch]
    [Route("{id}"), Authorize]
    public async Task<IActionResult> UpdatePost([FromRoute] string id, [FromBody] CreateOrUpdatePostInterface body)
    {
        if (body.Title == null || body.Message == null || body.SelectedFile == null)
        {
            return BadRequest(new { message = "Problem with provided body data." });
        }

        var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdToken is null) return NotFound(new { message = "User not authorized." });

        var post = new Post { };
        post = await _postService.GetPostById(id);

        if (post is null)
        {
            return NotFound(new { message = "Post with given id is not found." });
        }

        if (userIdToken != post.Creator) return Unauthorized(new { message = "User not authorized. You are not the creator of the post." });

        //add the new data
        post.Title = body.Title;
        post.Message = body.Message;
        post.SelectedFile = body.SelectedFile;

        // update post
        var upPost = await _postService.UpdatePost(id, post);

        if (upPost is null) return BadRequest(new { message = "Cannot update the post." });

        return Ok(new { post = post });
    }

    [HttpPatch]
    [Route("{id}/likepost"), Authorize]
    public async Task<IActionResult> LikeDislikePost([FromRoute] string id)
    {
        var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdToken is null) return NotFound(new { message = "User not authorized." });

        var post = new Post { };
        post = await _postService.GetPostById(id);

        if (post is null)
        {
            return NotFound(new { message = "Post with given id is not found." });
        }

        if (!post.Likes.Remove(userIdToken))
        {
            post.Likes.Add(userIdToken);

            //call notification
            if (post.Creator is not null)
            {
                var user = new User { };
                user = await _postService.GetUserById(userIdToken);
                if (user is not null)
                {
                    var details = user.Name + " liked your post.";
                    var us = new UserIn { Name = user.Name, Avatar = user.ImageUrl };
                    var notification = new Notification
                    {
                        MainUserId = post.Creator,
                        TargetId = id,
                        Details = details,
                        User = us
                    };
                    await _notificationService.CreateNotification(notification);
                }
            }
        }

        // update post
        var upPost = await _postService.UpdatePost(id, post);

        if (upPost is null) return BadRequest(new { message = "Cannot update the post." });

        return Ok(new { post = post });
    }

    [HttpDelete]
    [Route("{id}"), Authorize]
    public async Task<IActionResult> DeletePost([FromRoute] string id)
    {
        var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdToken is null) return NotFound(new { message = "User not authorized." });

        var post = new Post { };
        post = await _postService.GetPostById(id);

        if (post is null)
        {
            return NotFound(new { message = "Post with given id is not found." });
        }

        if (userIdToken != post.Creator) return Unauthorized(new { message = "User not authorized. You are not the creator of the post." });

        await _postService.DeletePostAsync(post.Id!);

        return Ok(new { message = "Post has been deleted." });
    }
}
