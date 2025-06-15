using AICourseTester.Models;

namespace AICourseTester.DTO
{
    public class FifteenPuzzleResponse
    {
        public List<ANode>? Problem { get; set; }
        public List<ANodeDTO>? Solution { get; set; }
        public List<ANodeDTO>? UserSolution { get; set; }
    }
}
