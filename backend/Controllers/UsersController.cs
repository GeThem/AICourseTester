using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal;
using AICourseTester.Data;
using AICourseTester.Models;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.IdentityModel.Tokens;
using System.Net.WebSockets;
using NuGet.Packaging.Signing;

namespace AICourseTester.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly MainDbContext _context;

        public UsersController(MainDbContext context, UserManager<ApplicationUser> userManager, IUserStore<ApplicationUser> userStore)
        {
            _userManager = userManager;
            _context = context;
            _userStore = userStore;
        }

        private static ValidationProblem CreateValidationProblem(IdentityResult result)
        {
            // We expect a single error code and description in the normal case.
            // This could be golfed with GroupBy and ToDictionary, but perf! :P
            Debug.Assert(!result.Succeeded);
            var errorDictionary = new Dictionary<string, string[]>(1);

            foreach (var error in result.Errors)
            {
                string[] newDescriptions;

                if (errorDictionary.TryGetValue(error.Code, out var descriptions))
                {
                    newDescriptions = new string[descriptions.Length + 1];
                    Array.Copy(descriptions, newDescriptions, descriptions.Length);
                    newDescriptions[descriptions.Length] = error.Description;
                }
                else
                {
                    newDescriptions = [error.Description];
                }

                errorDictionary[error.Code] = newDescriptions;
            }

            return TypedResults.ValidationProblem(errorDictionary);
        }

        [Authorize(Roles = "Administrator"), HttpPut("{userId}")]
        public async Task<ActionResult<IdentityResult>> UpdateUser(string? userName, string? password, int? groupId, string? name, string? secondName, string? patronymic, string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(f => f.Id == userId);
            if (user == null)
            {
                return NotFound();
            }
            if (groupId != null && await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId) != null)
            {
                _context.UserGroups.Add(new UserGroups { UserId = userId, GroupId = (int)groupId });
            }
            if (name != null)
            {
                user.Name = name;
            }
            if (secondName != null)
            {
                user.SecondName = secondName;
            }
            if (patronymic != null)
            {
                user.Patronymic = patronymic;
            }
            await _context.SaveChangesAsync();
            if (userName != null)
            {
                var result = await _userManager.SetUserNameAsync(user, userName);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            if (password != null)
            {
                string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                IdentityResult result = await _userManager.ResetPasswordAsync(user, resetToken, password);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            return Ok();
        }

        [Authorize(Roles = "Administrator"), HttpGet("Groups")]
        public async Task<ActionResult<Group[]>> GetGroups()
        {
            return await _context.Groups.ToArrayAsync();
        }

        [Authorize(Roles = "Administrator"), HttpGet("Groups/{id}/")]
        public async Task<ActionResult<UserData[]>> GetGroup(int id)
        {
            var group = await _context.UserGroups.Where(g => g.GroupId == id).Select(g => new UserData
            {
                Id = g.User.Id,
                Name = g.User.Name,
                SecondName = g.User.SecondName,
                Patronymic = g.User.Patronymic,
                Group = g.Group.Name
            }).ToArrayAsync();
            if (group.IsNullOrEmpty()) 
            {
                return NotFound();
            }
            return group;
        }

        [Authorize(Roles = "Administrator"), HttpPut("Groups/{id}")]
        public async Task<ActionResult> ChangeGroup(int id, string[] userIds)
        {
            if (await _context.Groups.FirstOrDefaultAsync(g => g.Id == id) == null)
            {
                return NotFound();
            }
            foreach (var userId in userIds)
            {
                if (await _context.Users.FirstOrDefaultAsync(u => u.Id == userId) == null)
                {
                    continue;
                }
                _context.UserGroups.Add(new UserGroups { UserId = userId, GroupId = id });
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [Authorize(Roles = "Administrator"), HttpPost("Groups")]
        public async Task<ActionResult> AddGroup(string groupName)
        {
            _context.Groups.Add(new Group { Name = groupName });
            await _context.SaveChangesAsync();
            return Ok();
        }

        [Authorize(Roles = "Administrator"), HttpDelete("Groups")]
        public async Task<ActionResult> DeleteGroup([System.Web.Http.FromUri] int? id, int[]? ids)
        {
            if (ids != null)
            {
                foreach (var groupId in ids)
                {
                    await _context.Groups.Where(g => g.Id == groupId).ExecuteDeleteAsync();
                }
            }
            await _context.Groups.Where(g => g.Id == id).ExecuteDeleteAsync();
            await _context.SaveChangesAsync();
            return Ok();
        }

        public class UserData
        {
            public required string Id { get; set; }
            public string? Name { get; set; }
            public string? SecondName { get; set; }
            public string? Patronymic { get; set; }
            public string? Group { get; set; }
        }

        private async Task<string?> GetGroup(string id)
        {
            var result = await _context.UserGroups.Include(g => g.Group).FirstOrDefaultAsync(ug => ug.UserId == id);
            return result?.Group.Name;
        }

        [Authorize(Roles = "Administrator"), HttpGet("{userId}")]
        public async Task<ActionResult<UserData?>> GetUser(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(f => f.Id == userId);
            if (user == null)
            {
                return NotFound();
            }
            return new UserData
            {
                Id = user.Id,
                Name = user.Name,
                SecondName = user.SecondName,
                Patronymic = user.Patronymic,
                Group = await GetGroup(user.Id)
            };
        }

        [Authorize(Roles = "Administrator"), HttpDelete()]
        public async Task<ActionResult> DeleteUser([System.Web.Http.FromUri] string? userId, [System.Web.Http.FromUri] int? groupId, string[]? userIds)
        {
            if (groupId != null)
            {
                if (await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId) != null)
                {
                    var ids = await _context.UserGroups.Where(g => g.GroupId == groupId).Select(ug => ug.UserId).ToArrayAsync();
                    foreach (var id in ids)
                    {
                        await _context.Users.Where(u => u.Id == id).ExecuteDeleteAsync();
                    }
                }
            }
            if (userId != null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(f => f.Id == userId);
                if (user != null)
                {
                    _context.Users.Remove(user);
                }
            }
            if (userIds != null)
            {
                foreach (var id in userIds)
                {
                    await _context.Users.Where(u => u.Id == id).ExecuteDeleteAsync();
                }
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [Authorize(Roles = "Administrator"), HttpGet]
        public async Task<ActionResult<UserData[]>> GetUsers()
        {
            var users = await _context.Users.Select(u => new UserData
            {
                Id = u.Id,
                Name = u.Name,
                SecondName = u.SecondName,
                Patronymic = u.Patronymic,
            }).ToArrayAsync();
            if (users == null)
            {
                return NotFound();
            }
            foreach (var user in users)
            {
                user.Group = await GetGroup(user.Id);
            }
            return users;
        }

        [Authorize(Roles = "Administrator"), HttpPost("Register")]
        public async Task<Results<Ok, ValidationProblem>> RegisterUser(RegReq registration, [FromServices] IServiceProvider sp)
        {
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            var userStore = sp.GetRequiredService<IUserStore<ApplicationUser>>();
            var userName = registration.UserName;

            if (string.IsNullOrEmpty(userName))
            {
                return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidUserName(userName)));
            }

            var user = new ApplicationUser();
            await userStore.SetUserNameAsync(user, userName, CancellationToken.None);
            user.Name = registration.Name;
            user.SecondName = registration.SecondName;
            user.Patronymic = registration.Patronymic;
            var result = await userManager.CreateAsync(user, registration.Password);

            if (!result.Succeeded)
            {
                return CreateValidationProblem(result);
            }

            if (registration.GroupId != null)
            {
                var userId = await _context.Users.FirstAsync(u => u.UserName == userName);
                _context.UserGroups.Add(new UserGroups { UserId = userId.Id, GroupId = (int)registration.GroupId });
                await _context.SaveChangesAsync();
            }

            return TypedResults.Ok();
        }

        [HttpPost("Login")]
        public async Task<Results<Ok<AccessTokenResponse>, EmptyHttpResult, ProblemHttpResult>> LoginUser(LogReq login, [FromQuery] bool? useCookies, [FromQuery] bool? useSessionCookies, [FromServices] IServiceProvider sp)
        {
            var signInManager = sp.GetRequiredService<SignInManager<ApplicationUser>>();

            var useCookieScheme = useCookies == true || useSessionCookies == true;
            var isPersistent = useCookies == true && useSessionCookies != true;
            signInManager.AuthenticationScheme = useCookieScheme ? IdentityConstants.ApplicationScheme : IdentityConstants.BearerScheme;

            var result = await signInManager.PasswordSignInAsync(login.UserName, login.Password, isPersistent, lockoutOnFailure: true);

            if (result.RequiresTwoFactor)
            {
                if (!string.IsNullOrEmpty(login.TwoFactorCode))
                {
                    result = await signInManager.TwoFactorAuthenticatorSignInAsync(login.TwoFactorCode, isPersistent, rememberClient: isPersistent);
                }
                else if (!string.IsNullOrEmpty(login.TwoFactorRecoveryCode))
                {
                    result = await signInManager.TwoFactorRecoveryCodeSignInAsync(login.TwoFactorRecoveryCode);
                }
            }

            if (!result.Succeeded)
            {
                return TypedResults.Problem(result.ToString(), statusCode: StatusCodes.Status401Unauthorized);
            }

            return TypedResults.Empty;
        }

        [Authorize, HttpPost("Logout")]
        public async Task<ActionResult> LogoutUser([FromServices] IServiceProvider sp)
        {
            var signInManager = sp.GetRequiredService<SignInManager<ApplicationUser>>();
            await signInManager.SignOutAsync();
            return Ok();
        }
    }
}
