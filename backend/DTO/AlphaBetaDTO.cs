using AICourseTester.Models;

namespace AICourseTester.DTO
{
    public class AlphaBetaDTO
    {
        public int Id { get; set; }
        public ProblemTree<ABNode>? Problem { get; set; }
        public List<ABNodeDTO>? Solution { get; set; }
        public int[]? Path { get; set; }
        public List<ABNodeDTO>? UserSolution { get; set; }
        public int[]? UserPath { get; set; }
        public int TreeHeight { get; set; }
        public bool IsSolved { get; set; }
        public DateTime Date { get; set; }
    }
}
