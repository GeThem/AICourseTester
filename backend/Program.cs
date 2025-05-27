using AICourseTester.Data;
using AICourseTester.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                          //policy.WithOrigins("http://example.com",
                                              //"http://www.contoso.com");
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

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<MainDbContext>();

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

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});


var app = builder.Build();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.MapIdentityApi<ApplicationUser>();

app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthorization();

app.MapControllers();

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
        var ctx = scope.ServiceProvider.GetRequiredService<MainDbContext>();
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
    
}

app.Run();
