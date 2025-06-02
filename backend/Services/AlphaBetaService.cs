using AICourseTester.Models;
using System.Security.Cryptography;

namespace AICourseTester.Services
{
    public class AlphaBetaService
    {
        public static readonly Random random = new Random();

        public static void PrepareTree(ProblemTree<ABNode> tree)
        {
            tree.Head.depth = 0;
            _prepareNode(tree.Head, null);
        }

        private static void _prepareNode(ABNode curr, ABNode? prev)
        {
            if (curr.SubNodes == null)
            {
                return;
            }
            curr.prv = prev;
            if (prev != null)
            {
                curr.depth = prev.depth + 1;
            }
            foreach (var subNode in curr.SubNodes)
            {
                _prepareNode(subNode, curr);
            }
        }

        public static List<ABNodeModel> Search(ProblemTree<ABNode> tree)
        {
            PrepareTree(tree);
            List<ABNodeModel> solution = new List<ABNodeModel>();
            _searchSubNode(tree.Head, solution);
            return solution;
        }

        private static (int, int) _searchSubNode(ABNode node, List<ABNodeModel> solution)
        {
            if (node.SubNodes == null)
            {
                solution.Add(new ABNodeModel(node));
                return (node.A, node.B);
            }
            if (node.prv != null)
            {
                node.A = node.prv.A;
                node.B = node.prv.B;
            }
            foreach (var subNode in node.SubNodes)
            {
                if (node.depth % 2 == 1)
                {
                    var (newA, newB) = _searchSubNode(subNode, solution);
                    node.B = Math.Min(Math.Min(newB, newA), node.B);
                }
                else
                {
                    var (newA, newB) = _searchSubNode(subNode, solution);
                    node.A = Math.Max(newB, Math.Max(newA, node.A));
                }
                if (node.A >= node.B)
                {
                    break;
                }
            }
            solution.Add(new ABNodeModel(node));
            return (node.A, node.B);
        }

        public static ProblemTree<ABNode> GenerateTree(int height)
        {
            ProblemTree<ABNode> tree = new ProblemTree<ABNode>();
            tree.Head = new ABNode();
            tree.Head.Id = 0;
            if (height == 0)
            {
                return tree;
            }
            _generateNodes(tree.Head, null, height, tree.Head.Id);
            return tree;
        }

        private static int _generateNodes(ABNode node, ABNode? prv, int height, int id)
        {
            node.Id = id;
            node.prv = prv;
            if (prv != null)
            {
                node.depth = prv.depth + 1;
            }
            if (height == 0)
            {
                int value = random.Next(0, 11);
                node.B = node.A = value;
                return id;
            }
            node.SubNodes = new List<ABNode>();
            int lim = random.Next(2, 4);
            for (int i = 0; i < lim; i++)
            {
                node.SubNodes.Add(new ABNode());
            }
            foreach (var subNode in node.SubNodes)
            {
                id = _generateNodes(subNode, node, height - 1, id + 1);
            }
            return id;
        }
    }
}
