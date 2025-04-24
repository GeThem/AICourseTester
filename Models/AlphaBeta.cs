using AICourseTester.Data;
using System.Text.Json.Serialization;

namespace AICourseTester.Models
{
    public class AlphaBeta
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        [JsonIgnore]
        public ApplicationUser User { get; set; } = null!;
        public string? Problem { get; set; }
        public string? Solution { get; set; }
        public int TreeDepth { get; set; }
        public bool IsSolved { get; set; } = false;
    }
}
