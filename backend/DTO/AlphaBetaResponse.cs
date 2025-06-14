﻿using AICourseTester.Models;

namespace AICourseTester.DTO
{
    public class AlphaBetaResponse
    {
        public ProblemTree<ABNode>? Problem { get; set; }
        public List<ABNodeDTO>? Solution { get; set; }
        public List<ABNodeDTO>? UserSolution { get; set; }
    }
}
