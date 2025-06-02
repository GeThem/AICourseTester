using AICourseTester.Models;

namespace AICourseTester.DTO
{
    public class FifteenPuzzleResponse
    {
        public List<ANode>? Problem { get; set; }
        public List<ANodeModel>? Solution { get; set; }
        public List<ANodeModel>? UserSolution { get; set; }
    }
}
