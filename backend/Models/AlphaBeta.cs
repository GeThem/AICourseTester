using AICourseTester.Data;
using System.Text.Json.Serialization;

namespace AICourseTester.Models
{
    public class AlphaBeta
    {
        public int Id { get; set; }
        [JsonIgnore]
        public string UserId { get; set; } = null!;
        [JsonIgnore]
        public ApplicationUser User { get; set; } = null!;
        public string? Problem { get; set; }
        public string? Solution { get; set; }
        public string? UserSolution { get; set; }
        public int TreeHeight { get; set; } = 3;
        public bool IsSolved { get; set; } = false;

        public DateTime Date { get; set; }
    }
}
