using AICourseTester.Data;
using AICourseTester.Models;
using AICourseTester.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SixLabors.ImageSharp.Web.DependencyInjection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          if (builder.Environment.IsDevelopment())
                          {
                              policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                          }
                          else
                          {
                              var front_url = Environment.GetEnvironmentVariable("FRONTEND_URL");
                              if (front_url == null)
                              {
                                  policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                              }
                              else
                              {
                                  policy
                                  //.SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
                                  .WithOrigins(front_url)
                                  .AllowAnyHeader().AllowAnyMethod();
                              }
                          }
                      });
});

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


string connString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? builder.Configuration.GetConnectionString("main_db");
builder.Services.AddDbContext<MainDbContext>(options =>
{
    options
    .UseNpgsql(connString);
});
builder.Services.AddTransient<UsersService>();

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>(identityOptions =>
    {
        identityOptions.Lockout.MaxFailedAccessAttempts = 10;
        identityOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        identityOptions.Lockout.AllowedForNewUsers = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<MainDbContext>();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddImageSharp();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    //// Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings.
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._+!@#$%^&*";
});


var tokenPolicy = "token";

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(tokenPolicy, httpContext =>
    RateLimitPartition.GetTokenBucketLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        //partitionKey: httpContext.User.Identity?.Name ?? "anonymous",
        factory: _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 150,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            ReplenishmentPeriod = TimeSpan.FromSeconds(60),
            TokensPerPeriod = 50,
            AutoReplenishment = true
        })
    );
});

var app = builder.Build();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.MapIdentityApi<ApplicationUser>();

app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.UseImageSharp();

app.UseStaticFiles();

app.MapControllers();

app.UseRateLimiter();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new string[] { "Administrator" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }


    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var userStore = scope.ServiceProvider.GetRequiredService<IUserStore<ApplicationUser>>();
    var ctx = scope.ServiceProvider.GetRequiredService<MainDbContext>();

    string? userName = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? builder.Configuration["Admin:UserName"] ?? "admin";
    var password = builder.Configuration["Admin:Password"] ?? builder.Configuration["Admin:Pw"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
    if (password == null)
    {
        throw new Exception("Provide password for admin");
    }

    ApplicationUser? user = await userManager.FindByNameAsync(userName);
    if (user != null)
    {
        if (!await userManager.CheckPasswordAsync(user, password))
        {
            string resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, resetToken, password);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join("\n", result.Errors));
            }
        }
    }
    else
    {       
        user = new ApplicationUser();
        await userStore.SetUserNameAsync(user, userName, CancellationToken.None);
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, "Administrator");
    }
}

var (http, https) = (Environment.GetEnvironmentVariable("LOCAL_URL_HTTP"), Environment.GetEnvironmentVariable("LOCAL_URL_HTTPS"));
if (!http.IsNullOrEmpty()) 
{ 
    app.Urls.Add(http);
}
if (!https.IsNullOrEmpty())
{
    app.Urls.Add(https);
}
app.Run();
