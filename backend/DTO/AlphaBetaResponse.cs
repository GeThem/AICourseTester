using AICourseTester.Models;

namespace AICourseTester.DTO
{
    public class AlphaBetaResponse
    {
        public ProblemTree<ABNode>? Problem { get; set; }
        public List<ABNodeModel>? Solution { get; set; }
        public List<ABNodeModel>? UserSolution { get; set; }
    }
}
