﻿using AICourseTester.Data;
using AICourseTester.DTO;
using AICourseTester.Models;
using SixLabors.ImageSharp.PixelFormats;

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

        public IQueryable<UserDTO> UserLeftJoinGroup(string? userId = null, bool getUserNames = false, bool getPfp = false)
        {
            var start = userId != null ? _context.Users.Where(u => u.Id == userId) : _context.Users;
            var result = start
                .GroupJoin(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                .SelectMany(uur => uur.ur.DefaultIfEmpty(), (u, r) => new { u.u, r })
                .GroupJoin(_context.Roles, ur => ur.r.RoleId, r => r.Id, (ur, r) => new { ur.u, r })
                .SelectMany(uur => uur.r.DefaultIfEmpty(), (u, r) => new { u.u, r })
                .GroupJoin(_context.UserGroups, ur => ur.u.Id, g => g.UserId, (ur, g) => new { ur, g })
                .SelectMany(urg => urg.g.DefaultIfEmpty(), (urg, g) => new { urg.ur.u.Id, urg.ur.u.UserName, urg.ur.u.Name, urg.ur.u.SecondName, urg.ur.u.Patronymic, g.GroupId, urg.ur.u.PfpPath, RoleName = urg.ur.r.Name })
                .GroupJoin(_context.Groups, u => u.GroupId, g => g.Id, (u, g) => new { u, g })
                .SelectMany(urg => urg.g.DefaultIfEmpty(), (ur, g) => new UserDTO
                {
                    Id = ur.u.Id,
                    UserName = getUserNames ? ur.u.UserName : null,
                    Name = ur.u.Name,
                    SecondName = ur.u.SecondName,
                    Patronymic = ur.u.Patronymic,
                    GroupId = ur.u.GroupId,
                    Group = g.Name,
                    Pfp = getPfp ? (Environment.GetEnvironmentVariable("LOCAL_URL_HTTPS") ?? Environment.GetEnvironmentVariable("LOCAL_URL_HTTP")).Split(";", StringSplitOptions.None)[0]
                        + $"/{ur.u.PfpPath ?? "Images/Default.webp"}"
                        : null,
                    IsAdmin = ur.u.RoleName == "Administrator" ? true : null
                }).OrderBy(u => u.Group);
            return result;
        }

        public async Task<string> UploadPfp(string userId, IFormFile pfp)
        {     
            var pfpPath = $"Images/{userId}.png";
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, pfpPath);
            using var ms = new MemoryStream();
            await pfp.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            using var input = Image.Load<Rgba32>(ms);
            await input.SaveAsync(fullPath);
            return pfpPath;
        }

        public string? LoadPfp(ApplicationUser user)
        {
            if (user.PfpPath == null)
            {
                return $"{(Environment.GetEnvironmentVariable("LOCAL_URL_HTTPS") ?? Environment.GetEnvironmentVariable("LOCAL_URL_HTTP")).Split(";", StringSplitOptions.None)[0]}/Images/Default.webp";
            }
            return (Environment.GetEnvironmentVariable("LOCAL_URL_HTTPS") ?? Environment.GetEnvironmentVariable("LOCAL_URL_HTTP")).Split(";", StringSplitOptions.None)[0] + $"/{user.PfpPath}";
        }
    }
}
