namespace AICourseTester.backend.Models
{
    public class ProblemTree<T> : ICloneable, IEquatable<ProblemTree<T>> where T : Node<T>
    {
        public T? Head { get; set; }

        public IEnumerable<T?> GetNodes()
        {
            if (Head == null)
            {
                yield return default;
                yield break;
            }
            foreach (var node in yieldNode(Head))
            {
                yield return node;
            }
        }

        private IEnumerable<T> yieldNode(T node)
        {
            if (node.SubNodes == null)
            {
                yield return node;
                yield break;
            }
            foreach (var subNode in node.SubNodes)
            {
                foreach (var item in yieldNode(subNode))
                {
                    yield return item;
                }
            }
            yield return node;
        }

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
            var (arr1, arr2) = (GetNodes().ToList(), other.GetNodes().ToList());
            if (arr1.Count != arr2.Count)
            {
                return false;
            }
            foreach (var (node1, node2) in arr1.Zip(arr2))
            {
                if (node1 == null && node2 == null)
                {
                    continue;
                }
                if (node1 == null || node2 == null)
                {
                    return false;
                }
                if (!node1.Equals(node2))
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
            if (node.SubNodes == null)
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
        public List<T>? SubNodes { get; set; }
        public void Reset();
    }
}
