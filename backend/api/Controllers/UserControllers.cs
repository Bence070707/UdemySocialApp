using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.Interfaces;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api.Controllers;

[Controller]
[Route("/user")]

public class UserController(IConfiguration configuration,
UserService userService, NotificationService notificationService) : Controller
{
    private readonly IConfiguration _configuration = configuration;
    private readonly UserService _UserService = userService;
    private readonly NotificationService _notificationService = notificationService;

    [HttpPost]
    [Route("signup")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateUserInterface body)
    {
        var user = new User { };
        if (body.FirstName == null || body.LastName == null || body.Email == null || body.Password == null)
        {
            return BadRequest(new { message = "Missing required fields" });
        }

        user.Name = body.FirstName + body.LastName;
        user.Email = body.Email;
        user.Password = Models.User.EncryptPasswordBase64(body.Password);

        var checkUser = await _UserService.GetUserByEmail(body.Email);
        if (checkUser != null) return BadRequest(new { message = "User already exists." });

        await _UserService.CreateAsync(user);

        //create token
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id ?? throw new InvalidOperationException()),
            new(ClaimTypes.Name, user.Name ?? throw new InvalidOperationException()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var tokenSecret = _configuration.GetValue<string>("JwtSecret:Secret");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecret ?? throw new InvalidOperationException()));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddMinutes(60);

        var token = new JwtSecurityToken(
            issuer: "https://localhost:5000",
            audience: "https://localhost:5000",
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return Ok(new { result = user, token = new JwtSecurityTokenHandler().WriteToken(token) });
    }

    [HttpPost]
    [Route("signin")]
    public async Task<IActionResult> LogInUser([FromBody] LoginInterface body)
    {
        if (body.Email == null || body.Password == null) return BadRequest(new { message = "Missing required fields" });

        var user = await _UserService.GetUserByEmail(body.Email);
        if (user is null) return BadRequest(new { message = "User not found" });

        var decodedPassword = Models.User.DecryptPasswordBase64(user.Password);
        if (decodedPassword != body.Password) return BadRequest(new { message = "Given email or password is incorrect." });

        //create token
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id ?? throw new InvalidOperationException()),
            new(ClaimTypes.Name, user.Name ?? throw new InvalidOperationException()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var tokenSecret = _configuration.GetValue<string>("JwtSecret:Secret");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecret ?? throw new InvalidOperationException()));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddMinutes(60);

        var token = new JwtSecurityToken(
            issuer: "https://localhost:5000",
            audience: "https://localhost:5000",
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return Ok(new { result = user, token = new JwtSecurityTokenHandler().WriteToken(token) });
    }

    [HttpGet]
    [Route("getUser/{id}")]
    public async Task<IActionResult> GetUserById([FromRoute] string id)
    {

        var user = await _UserService.GetUserById(id);

        if (user is null) return NotFound(new { message = "User with such id doesn't exist." });

        //TODO return also the user posts

        return Ok(new { user = user, posts = Array.Empty<object>() });
    }

    [HttpPatch]
    [Route("update/{id}"), Authorize]
    public async Task<IActionResult> UpdateUser([FromRoute] string id, [FromBody] UpdateUserInterface body)
    {
        var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdToken != id) return BadRequest(new { message = "You can only update your profile." });

        if (body.ImageUrl == null || body.Name == null || body.Bio == null) return BadRequest(new { message = "Something is missing." });

        var user = new User { };
        user = await _UserService.GetUserById(id);

        if (user is null) return NotFound(new { message = "User doesn't exit with such id." });

        user.Name = body.Name;
        user.Bio = body.Bio;
        user.ImageUrl = body.ImageUrl;

        var updateUser = await _UserService.UpdateUser(id, user);

        if (updateUser is null) return NotFound(new { message = "Cannot update user." });

        return Ok(new { user = user });
    }

    [HttpPatch]
    [Route("{id}/following"), Authorize]
    public async Task<IActionResult> Following([FromRoute] string id)
    {
        if (id == null) return BadRequest(new { message = "Problem with provided id data" });

        try
        {
            var user2 = await _UserService.GetUserById(id);
            if (user2 is null || user2.Id is null) return NotFound(new { message = "User not found", Success = false });

            var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdToken is null) return BadRequest(new { message = "Problem with provided id data of token" });

            var user1 = await _UserService.GetUserById(userIdToken);

            if (user1 is null || user1.Id is null) return NotFound(new { message = "User not found", success = false });

            if (user1.Id == user2.Id) return BadRequest(new { message = "You cannot follow yourself." });

            user1.Followings ??= [];

            user2.Followings ??= [];

            var fo = user1.Followings;
            var fo2 = user2.Followers;

            if (fo.Contains(id))
            {
                fo.Remove(id);
                user1.Followings = fo;
                fo2.Remove(user1.Id);
                user2.Followers = fo2;
            }
            else
            {
                fo.Add(id);
                user1.Followings = fo;
                fo2.Add(user1.Id);
                user2.Followers = fo2;
                
                // call notification start
                var details = user1.Name + " started following you.";
                var user = new UserIn{Name = user1.Name, Avatar = user1.ImageUrl};
                var notification = new Notification
                {
                    MainUserId = user2.Id,
                    TargetId = user1.Id,
                    Details = details,
                    User = user
                };

                await _notificationService.CreateNotification(notification);
                // call notification end
            }

            await _UserService.UpdateUser(user1.Id, user1);
            await _UserService.UpdateUser(user2.Id, user2);

            return Ok(new
            {
                user1 = user1,
                user2 = user2,
                success = true,
                message = "Successfully."
            });

        }
        catch (Exception ex)
        {
            return BadRequest(new {message = ex.Message, success = false});
        }
    }

    [HttpGet]
    [Route("getsug"), Authorize]
    public async Task<IActionResult> GetSugUsers([FromQuery] string id)
    {
        try
        {
            if (id == "undefined") return BadRequest(new { message = "Id is undefined", success = false });

            var mainUser = await _UserService.GetUserById(id);

            if (mainUser is null) return NotFound(new { message = "Id is undefined", success = false });

            var followingList = mainUser.Followings;
            if (followingList is null) return NotFound(new { message = "Null following list for user", success = false });

            var followUsersList = new List<User> { };
            foreach (var uid in followingList)
            {
                var getUserFollowing = await _UserService.GetUserById(uid);
                if (getUserFollowing != null)
                {
                    followUsersList.Add(getUserFollowing);
                }
            }

            // start use f list
            var usersIdsForSug = new List<string>();
            var FinalUsers = new List<User>();
            foreach (var us in followUsersList)
            {
                if (us.Followers != null && mainUser.Id != null)
                {
                    foreach (var ids in us.Followers)
                    {
                        if (usersIdsForSug.Contains(ids) || ids == mainUser.Id) continue;
                        var gus = await _UserService.GetUserById(ids);
                        if (gus != null) FinalUsers.Add(gus);
                        usersIdsForSug.Add(ids);
                    }
                }

                //following
                if (us.Followings != null && mainUser.Id != null)
                {
                    foreach (var ids in us.Followings)
                    {
                        if (usersIdsForSug.Contains(ids) || ids == mainUser.Id) continue;
                        var gus = await _UserService.GetUserById(ids);
                        if (gus != null) FinalUsers.Add(gus);
                        usersIdsForSug.Add(ids);
                    }
                }

            }
            // return the result
            return Ok(new
            {
                users = FinalUsers,
                success = true,
                message = "Successfully"
            });

        }
        catch (Exception ex)
        {
            return BadRequest(new {message = ex.Message, success = false});
        }
    }

    [HttpDelete]
    [Route("delete/{id}"), Authorize]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        var userIdToken = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(userIdToken != id) return Unauthorized(new{ message = "You are not authorized to delete another's account."});

        await _UserService.DeleteAsync(id);
        return Ok();
    }
}
