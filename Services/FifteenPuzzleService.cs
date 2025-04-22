using AICourseTester.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.VisualBasic;
using Mono.TextTemplating;
using System.Drawing.Printing;
using System.Security.Cryptography;

namespace AICourseTester.Services
{
    public class FifteenPuzzleService
    {
        public static readonly Func<ANode, int>[] Heuristics = [Heuristic1, Heuristic2];

        public static List<ANode> GenerateSolution(List<ANode> list, int heuristic)
        {
            var newList = new List<ANode>();
            list.ForEach(node => newList.Add((ANode)node.Clone()));
            var newTree = FifteenPuzzleService.ListToTree(newList);
            FifteenPuzzleService.Search(newTree, FifteenPuzzleService.Heuristics[heuristic]);
            return newList;
        }

        public static void ResetNode(ANode node)
        {
            node.G = node.F = node.H = -1;
        }

        public static void ShuffleState(ANode node, int moves = 30)
        {
            if (node.State == null) return;
            var (x, y) = (-1, -1);
            (int x, int y)[] directions = [(-1, 0), (0, -1), (1, 0), (0, 1)];

            for (int i = 0; i < node.State.Length; i++) {
                for (int j = 0; j < node.State[0].Length; j++)
                {
                    if (node.State[i][j] == 0)
                    {
                        (x, y) = (j, i);
                        break;
                    }
                }
                if (x != -1)
                {
                    break;
                }
            }

            int lastMoveIdx = -1;
            for (int i = 0; i < moves; i++)
            {
                int moveIdx = RandomNumberGenerator.GetInt32(4);
                if (moveIdx == lastMoveIdx)
                {
                    moveIdx = (moveIdx + 1) % 4;
                }
                var move = directions[moveIdx];
                var (nX, nY) = (x + move.x, y + move.y);
                if (nX < 0 || nY < 0 || nX > node.State.Length - 1 || nY  > node.State[0].Length - 1)
                {
                    moveIdx = (moveIdx + 2) % 4;
                    move = directions[moveIdx];
                }
                lastMoveIdx = moveIdx;
                (node.State[y][x], node.State[move.y][move.x]) = (node.State[move.y][move.x], node.State[y][x]);

            }
        }

        public static ProblemTree<ANode> ListToTree(List<ANode> nodes)
        {
            ProblemTree<ANode> tree = new();
            foreach (var node in nodes)
            {
                if (node.Parents.Count == 0)
                {
                    tree.Head = node;
                }
                foreach (var subNode in nodes)
                {
                    if (node.SubNodesIds.Count == 0 || node.Id == subNode.Id)
                    {
                        continue;
                    }
                    if (node.SubNodes == null)
                    {
                        node.SubNodes = new();
                    }
                    if (node.SubNodesIds.Contains(subNode.Id)) {
                        node.SubNodes.Add(subNode);
                    }
                }
            }
            return tree;
        }

        public static void GenerateNextStates(ANode node, ICollection<ANode>? ignore)
        {
            var state = node.State;
            var (ox, oy) = (-1, -1);
            bool flag = false;
            for (int i = 0; i < state.Length; i++)
            {
                if (flag)
                {
                    break;
                }
                for (int j = 0; j < state[0].Length; j++)
                {
                    if (state[i][j] == 0)
                    {
                        (ox, oy) = (j, i);
                        flag = true;
                        break;
                    }

                }
            }
            if (ox == -1)
            {
                throw new Exception("Incorrect state");
            }

            if (node.SubNodes == null)
            {
                node.SubNodes = new();
            }

            foreach (var (x, y) in ((int, int)[])[ (-1, 0), (1, 0), (0, -1), (0, 1) ] )
            {
                int Nox = ox + x, Noy = oy + y;
                if (Nox < 0 || Noy < 0 || Nox >= state[0].Length || Noy >= state.Length)
                {
                    continue;
                }
                int[][] newState = new int[node.State.Length][];
                for (int i = 0; i < node.State.Length; i++)
                {
                    newState[i] = (int[])node.State[i].Clone();
                }
                (newState[oy][ox], newState[Noy][Nox]) = (newState[Noy][Nox], newState[oy][ox]);

                if (ignore != null)
                {
                    var n = _containsState(newState, ignore);
                    if (n != null)
                    {
                        if (n.depth == node.depth + 1)
                        {
                            n.Parents.Add(node.Id);
                            node.SubNodes.Add(n);
                        }
                        continue;
                    }
                }
                ANode newNode = new ANode();
                newNode.depth = node.depth + 1;
                newNode.Parents.Add(node.Id);
                newNode.State = newState;
                node.SubNodes.Add(newNode);
                if (ignore != null)
                {
                    ignore.Add(newNode);
                }
            }
        }

        private static ANode? _containsState(int[][] state, ICollection<ANode> nodes)
        {
            foreach (var item in nodes)
            {
                var flag = true;
                for (int i = 0; i < item.State.Length; i++)
                {
                    if (!item.State[i].SequenceEqual(state[i]))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    return item;
                }
            }
            return null;
        }

        public static void Search(ProblemTree<ANode> tree, Func<ANode, int> h)
        {
            OrderedSet<ANode> openNodes = new(state => state.F)
            {
                tree.Head
            };
            tree.Head.G = 0;
            tree.Head.H = h(tree.Head);
            tree.Head.F = tree.Head.G + tree.Head.H;
            HashSet<ANode> closedNodes = new();

            while (openNodes.Count > 0)
            {
                var curr = openNodes.Pop();
                if (curr.F - curr.G == 0)
                {
                    return;
                }
                closedNodes.Add(curr);
                if (curr.SubNodes == null)
                {
                    return;
                }
                foreach (var state in curr.SubNodes)
                {
                    var item = openNodes.GetItem(state);
                    if (item != null)
                    {
                        var H = h(state);
                        var score = H + curr.G + 1;
                        if (score < item.F)
                        {
                            item.H = H;
                            item.F = score;
                            item.G = curr.G + 1;
                            item.prv = curr;
                        }
                        continue;
                    }
                    closedNodes.TryGetValue(state, out item);
                    if (item != null)
                    {
                        var H = h(state);
                        var score = H + curr.G + 1;
                        if (score < item.F)
                        {
                            closedNodes.Remove(item);
                            item.F = score;
                            item.G = curr.G + 1;
                            item.H = H;
                            item.prv = curr;
                            openNodes.Add(item);
                        }
                        continue;
                    }
                    state.H = h(state);
                    state.G = curr.G + 1;
                    state.F = state.H + state.G;
                    state.prv = curr;
                    openNodes.Add(state);
                }
            }
            return;
        }

        public static void PrepareTree(ProblemTree<ANode> tree)
        {
            _setPointer(tree.Head, null);
        }

        private static void _setPointer(ANode curr, ANode? prev)
        {
            if (curr.SubNodes == null)
            {
                return;
            }
            curr.prv = prev;
            curr.Parents.Add(prev.Id);
            foreach (var subNode in curr.SubNodes)
            {
                _setPointer(subNode, curr);
            }
        }

        public static (ProblemTree<ANode>, List<ANode>) GenerateTree(ANode startState, int height)
        {
            ProblemTree<ANode> tree = new ProblemTree<ANode>();
            tree.Head = startState;
            tree.Head.Id = 0;
            List<ANode> ignore = new() { tree.Head };
            if (height == 0)
            {
                return (tree, ignore);
            }
            _generateNodes(tree.Head, height, ignore, tree.Head.Id);
            return (tree, ignore);
        }

        private static int _generateNodes(ANode node, int height, ICollection<ANode>? ignore, int id)
        {
            node.Id = id;
            if (height == 0)
            {
                return id;
            }
            GenerateNextStates(node, ignore);
            if (node.SubNodes == null)
            {
                return id;
            }
            if (node.SubNodesIds == null)
            {
                node.SubNodesIds = new();
            }
            foreach (var subNode in node.SubNodes)
            {
                node.SubNodesIds.Add(id + 1);
                id = _generateNodes(subNode, height - 1, ignore, id + 1);
            }
            return id;
        }

        public static int Heuristic1(ANode node)
        {
            var score = 0;
            var len = node.State.Length * node.State[0].Length;
            for (int i = 1; i < len; i++)
            {
                if (node.State[i / node.State.Length][i % node.State[0].Length] != i)
                {
                    score++;
                }
            }
            return score;
        }

        public static int Heuristic2(ANode node)
        {
            var score = 0;                                     // суммарное расстояние до цели
            for (int i = 0; i < node.State.Length; i++)
            {
                for (int j = 0; j < node.State[0].Length; j++)
                {
                    var tile = node.State[i][j];
                    if (tile == 0)
                    {
                        continue;
                    }
                    var (nx, ny) = (tile % node.State[0].Length, tile / node.State[0].Length);
                    score += Math.Abs(nx - j) + Math.Abs(ny - i);

                }
            }
            return score;
        }
    }   
}
