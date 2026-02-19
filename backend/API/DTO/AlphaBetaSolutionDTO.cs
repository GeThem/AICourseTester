using AICourseTester.Models;

namespace AICourseTester.DTO
{
    public class AlphaBetaSolutionDTO
    {
        public List<ABNodeDTO>? Nodes { get; set; }
        public int[]? Path { get; set; }
    }
}
