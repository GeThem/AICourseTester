using AICourseTester.Data;
using AICourseTester.Models;
using AICourseTester.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
                              policy.SetIsOriginAllowed(origin => new Uri(origin).IsLoopback).AllowAnyHeader().AllowAnyMethod();
                          }
                          else
                          {
                              policy.SetIsOriginAllowed(origin => new Uri(origin).IsLoopback).AllowAnyHeader().AllowAnyMethod();
                          }
                      });
});

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MainDbContext>(options =>
{
    options
    .UseNpgsql(builder.Configuration.GetConnectionString("main_db"));
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
    //options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    //options.Lockout.MaxFailedAccessAttempts = 5;
    //options.Lockout.AllowedForNewUsers = true;

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
            TokenLimit = 50,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            ReplenishmentPeriod = TimeSpan.FromSeconds(60),
            TokensPerPeriod = 20,
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

    string? userName = builder.Configuration["Admin:UserName"];
    if (userName == null)
    {
        userName = "admin";
    }
    var password = ((string?[])[builder.Configuration["Admin:Password"], builder.Configuration["Admin:Pw"]]).FirstOrDefault(pw => pw != null);
    if (password == null)
    {
        throw new Exception("Provide password for admin - secrets.json / appsettings.json:\n\t\"Admin\": { {\"Password\" | \"Pw\"}: \"<password>\"[, \"UserName\": \"<username>\"] }");
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
        var role = await ctx.Roles.Where(r => r.Name == "Administrator").FirstAsync();
        var prevAdmin = await ctx.UserRoles.FirstOrDefaultAsync(u => u.RoleId == role.Id);
        if (prevAdmin != null)
        {
            await ctx.Users.Where(u => u.Id == prevAdmin.UserId).ExecuteDeleteAsync();
            await ctx.SaveChangesAsync();
        }
        
        user = new ApplicationUser();
        await userStore.SetUserNameAsync(user, userName, CancellationToken.None);
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, "Administrator");
    }
    if (user.Name != "admin")
    {
        user.Name = "admin";
        await ctx.SaveChangesAsync();
    }
}

app.Run();
