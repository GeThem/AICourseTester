using AICourseTester.Data;
using AICourseTester.DTO;
using Microsoft.EntityFrameworkCore;
using System;

namespace AICourseTester.Services
{
    public class UsersService
    {
        private MainDbContext _context;
        public UsersService(MainDbContext context) 
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public IQueryable<UserDTO> UserLeftJoinGroup()
        {
            var result = _context.Users
                .GroupJoin(_context.UserGroups, u => u.Id, g => g.UserId, (u, g) => new { u, g })
                .SelectMany(ug => ug.g.DefaultIfEmpty(), (u, g) => new { u.u.Id, u.u.Name, u.u.SecondName, u.u.Patronymic, g.GroupId })
                .GroupJoin(_context.Groups, u => u.GroupId, g => g.Id, (u, g) => new { u, g })
                .SelectMany(ug => ug.g.DefaultIfEmpty(), (u, g) => new UserDTO
                {
                    Id = u.u.Id,
                    Name = u.u.Name,
                    SecondName = u.u.SecondName,
                    Patronymic = u.u.Patronymic,
                    Group = g.Name
                });
            return result;
        }
    }
}
