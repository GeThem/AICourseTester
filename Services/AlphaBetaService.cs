using AICourseTester.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.VisualBasic;
using Mono.TextTemplating;
using System.Collections;
using System.Drawing.Printing;
using System.Numerics;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AICourseTester.Services
{
    public class AlphaBetaService
    {
        public static ProblemTree<ABNode> GenerateSolution(ProblemTree<ABNode> tree, int heuristic)
        {
            var newTree = (ProblemTree<ABNode>)tree.Clone();
            Search(newTree);
            return newTree;
        }

        public static void Search(ProblemTree<ABNode> tree)
        {
            _searchSubNode(tree.Head);
        }

        private static (int, int) _searchSubNode(ABNode node)
        {
            if (node.SubNodes == null)
            {
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
                    var (newA, newB) = _searchSubNode(node);
                    node.B = Math.Min(Math.Min(newB, newA), node.B);
                }
                else
                {
                    var (newA, newB) = _searchSubNode(node);
                    node.A = Math.Max(newB, Math.Max(newA, node.B));
                }
                if (node.A >= node.B)
                {
                    break;
                }
            }
            return (node.A, node.B);
        }

        public static ProblemTree<ABNode> GenerateTree(ABNode startState, int height)
        {
            ProblemTree<ABNode> tree = new ProblemTree<ABNode>();
            tree.Head = startState;
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
            if (node.prv != null)
            {
                node.depth = node.prv.depth + 1;
            }
            if (height == 0)
            {
                if (node.depth % 2 == 0)
                {
                    node.B = RandomNumberGenerator.GetInt32(11);
                } else {
                    node.A = RandomNumberGenerator.GetInt32(11);
                }
                return id;
            }
            node.SubNodes = Enumerable.Repeat(new ABNode(), RandomNumberGenerator.GetInt32(3) + 1).ToList();
            foreach (var subNode in node.SubNodes)
            {
                id = _generateNodes(subNode, node, height - 1, id + 1);
            }
            return id;
        }
    }
}
