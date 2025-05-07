using AICourseTester.backend.Data;
using System.Text.Json.Serialization;

namespace AICourseTester.backend.Models
{
    public class FifteenPuzzle
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        [JsonIgnore]
        public ApplicationUser User { get; set; } = null!;
        public string? Problem { get; set; }
        public string? Solution { get; set; }
        public string? UserSolution { get; set; }
        public int? Heuristic { get; set; }
        public int Dimensions { get; set; }
        public int TreeHeight { get; set; }
        public bool IsSolved { get; set; } = false;
    }

    public class FifteenPuzzleResponse
    {
        public List<ANode>? Problem { get; set; }
        public List<ANodeModel>? Solution { get; set; }
        public List<ANodeModel>? UserSolution { get; set; }
    }
}
