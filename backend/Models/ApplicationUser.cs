using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace AICourseTester.Models
{
    [Index(nameof(UserName), IsUnique = true)]
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }
        public string? SecondName { get; set; }
        public string? Patronymic { get; set; }
    }
}
