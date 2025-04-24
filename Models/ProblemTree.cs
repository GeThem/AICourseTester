using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AICourseTester.Models
{
    public class ProblemTree<T> : ICloneable, IEquatable<ProblemTree<T>> where T : Node<T>
    {
        public T? Head { get; set; }

        public bool Equals(ProblemTree<T>? other)
        {
            if (other == null)
            {
                return false;
            }
            if (other.Head == null && Head == null)
            {
                return true;
            }
            if (other.Head == null || Head == null)
            {
                return false;
            }
            return _equalsNode(Head, other.Head);
        }

        private bool _equalsNode(T node, T other)
        {
            if (node.SubNodes != null && other.SubNodes == null)
            {
                return false;
            }
            if (node.SubNodes == null && other.SubNodes != null)
            {
                return false;
            }
            if (node.SubNodes == null)
            {
                return node.Equals(other);
            }
            if (node.SubNodes.Count != other.SubNodes.Count)
            {
                return false;
            }
            bool result = true;
            foreach (var (subNode, subNodeOther) in node.SubNodes.Zip(other.SubNodes))
            {
                if (_equalsNode(subNode, subNodeOther) == false)
                {
                    return false;
                }
            }
            return true;
        }

        public object Clone()
        {
            ProblemTree<T> newTree = new ProblemTree<T>();
            if (Head == null)
            {
                return newTree;
            }
            newTree.Head = _cloneNode(Head);
            return newTree;
        }

        private T _cloneNode(T node)
        {
            T newNode = (T)node.Clone();
            newNode.prv = node;
            if (node.SubNodes  == null)
            {
                return newNode;
            }
            List<T> newSubNodes = new();
            foreach (var subNode in node.SubNodes)
            {
                newSubNodes.Add(_cloneNode(subNode));
            }
            newNode.SubNodes = newSubNodes;
            return newNode;
        }
    }

    public interface Node<T> : ICloneable, IEquatable<T> where T : Node<T>
    {
        public T? prv { get; set; }
        public List<T>? SubNodes { get; set; }
        public void Reset();
    }
}
