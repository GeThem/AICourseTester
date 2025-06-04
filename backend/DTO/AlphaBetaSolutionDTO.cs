using AICourseTester.Models;

namespace AICourseTester.DTO
{
    public class AlphaBetaSolutionDTO
    {
        public List<ABNodeModel>? Nodes { get; set; }
        public int[]? Path { get; set; }
    }
}
