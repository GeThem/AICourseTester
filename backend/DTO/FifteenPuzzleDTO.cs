using AICourseTester.Models;

namespace AICourseTester.DTO
{
    public class FifteenPuzzleDTO
    {
        public int Id { get; set; }
        public List<ANode>? Problem { get; set; }
        public List<ANodeDTO>? Solution { get; set; }
        public List<ANodeDTO>? UserSolution { get; set; }
        public int? Heuristic { get; set; }
        public int Dimensions { get; set; }
        public int TreeHeight { get; set; }
        public bool IsSolved { get; set; }
        public DateTime Date { get; set; }
    }
}
