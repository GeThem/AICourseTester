namespace AICourseTester.Models
{
    public class AlphaBetaNode : Node
    {
        public int Value { get; set; }

        public List<AlphaBetaNode>? SubNodes = null;
        public List<int> SubNodesIds { get; set; } = new();
        public List<int>? Leaves { get; set; }
    }
}
