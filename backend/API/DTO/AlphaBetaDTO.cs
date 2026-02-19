using AICourseTester.Models;
using System.Text.Json.Serialization;

namespace AICourseTester.DTO
{
    public class AlphaBetaDTO
    {
        public int Id { get; set; }
        public ProblemTree<ABNode>? Problem { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ABNodeDTO>? Solution { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Path { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ABNodeDTO>? UserSolution { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? UserPath { get; set; }
        public int TreeHeight { get; set; }
        public bool IsSolved { get; set; }
        public DateTime Date { get; set; }
    }
}
