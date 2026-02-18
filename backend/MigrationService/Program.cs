using AICourseTester.Data;
using Microsoft.EntityFrameworkCore;
using SupportTicketApi.MigrationService;


var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

string connString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? builder.Configuration.GetConnectionString("main_db");
builder.Services.AddDbContext<MainDbContext>(options =>
{
    options
    .UseNpgsql(connString);
});

var host = builder.Build();
host.Run();