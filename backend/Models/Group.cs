using Microsoft.EntityFrameworkCore;

namespace AICourseTester.Models
{
    [Index(nameof(Name), IsUnique = true)] 
    public class Group
    {
        public int Id { get; set; }
        public required string Name { get; set; }
    }
}
