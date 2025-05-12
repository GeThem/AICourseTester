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

namespace AICourseTester.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MainDbContext _context;

        public UserController(MainDbContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
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
        public async Task<ActionResult> UpdateUser(int groupId, string userId)
        {
            if (await _context.Users.FirstOrDefaultAsync(f => f.Id == userId) == null)
            {
                return NotFound();
            }
            _context.UserGroups.Add(new UserGroups { UserId = userId, GroupId = groupId });
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

        public async Task<string?> GetGroup(string id)
        {
            var result = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.UserId == id);
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
            var result = await userManager.CreateAsync(user, registration.Password);

            if (!result.Succeeded)
            {
                return CreateValidationProblem(result);
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
