using AICourseTester.Data;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AICourseTester.Models
{
    public class FifteenPuzzle
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public string? Problem { get; set; }
        public string? Solution { get; set; }
        public int? Heuristic { get; set; }
        public int Dimensions { get; set; }
        public int TreeDepth { get; set; }
        public bool IsSolved { get; set; }
    }
}
