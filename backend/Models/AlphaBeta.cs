using AICourseTester.backend.Data;
using System.Text.Json.Serialization;

namespace AICourseTester.backend.Models
{
    public class AlphaBeta
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        [JsonIgnore]
        public ApplicationUser User { get; set; } = null!;
        public string? Problem { get; set; }
        public string? Solution { get; set; }
        public string? UserSolution { get; set; }
        public int TreeHeight { get; set; } = 3;
        public bool IsSolved { get; set; } = false;
    }

    public class AlphaBetaResponse
    {
        public ProblemTree<ABNode>? Problem { get; set; }
        public List<ABNodeModel>? Solution { get; set; }
        public List<ABNodeModel>? UserSolution { get; set; }
    }
}
