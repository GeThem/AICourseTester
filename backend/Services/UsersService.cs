using AICourseTester.Data;
using AICourseTester.DTO;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using AICourseTester.Models;
using Azure.Core;

namespace AICourseTester.Services
{
    public class UsersService
    {
        private readonly MainDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UsersService(MainDbContext context, IWebHostEnvironment webHostEnvironment) 
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IQueryable<UserDTO> UserLeftJoinGroup(string? userId = null)
        {
            var start = userId == null ? _context.Users.Where(u => u.NormalizedUserName != "ADMIN")
                : _context.Users.Where(u => u.Id == userId);
            var result = start
                .GroupJoin(_context.UserGroups, u => u.Id, g => g.UserId, (u, g) => new { u, g })
                .SelectMany(ug => ug.g.DefaultIfEmpty(), (u, g) => new { u.u.Id, u.u.Name, u.u.SecondName, u.u.Patronymic, g.GroupId, u.u.PfpPath })
                .GroupJoin(_context.Groups, u => u.GroupId, g => g.Id, (u, g) => new { u, g })
                .SelectMany(ug => ug.g.DefaultIfEmpty(), (u, g) => new UserDTO
                {
                    Id = u.u.Id,
                    Name = u.u.Name,
                    SecondName = u.u.SecondName,
                    Patronymic = u.u.Patronymic,
                    Group = g.Name,
                    Pfp = u.u.PfpPath == null ?
                    Environment.GetEnvironmentVariable("LOCAL_URL").Split(";", StringSplitOptions.None)[0] + "/Images/Default.webp"
                    : Environment.GetEnvironmentVariable("LOCAL_URL").Split(";", StringSplitOptions.None)[0] + $"/{ u.u.PfpPath}"
                });
            return result;
        }

        public async Task<string> UploadPfp(string userId, IFormFile pfp)
        {
            var pfpPath = $"Images/{userId}.webp";
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, pfpPath);
            using var ms = new MemoryStream();
            await pfp.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            using var input = Image.Load<Rgba32>(ms);
            if (input.Width > 256 || input.Height > 256)
            {
                input.Mutate(x => x.Resize(256, 256));
            }
            await input.SaveAsWebpAsync(fullPath);
            return pfpPath;
        }

        public string? LoadPfp(ApplicationUser user)
        {
            if (user.PfpPath == null)
            {
                return $"{Environment.GetEnvironmentVariable("LOCAL_URL").Split(";", StringSplitOptions.None)[0]}/Images/Default.webp";
            }
            return Environment.GetEnvironmentVariable("LOCAL_URL").Split(";", StringSplitOptions.None)[0] + $"/{user.PfpPath}";
        }
    }
}
