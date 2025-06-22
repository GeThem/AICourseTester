using Microsoft.EntityFrameworkCore;

namespace AICourseTester.Models
{
    [PrimaryKey(nameof(UserId), nameof(GroupId))]
    public class UserGroups
    {
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public int GroupId { get; set; }
        public Group Group { get; set; } = null!;
    }
}
