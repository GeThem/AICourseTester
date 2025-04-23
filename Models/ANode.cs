using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace AICourseTester.Models
{
    public class ANode : Node, ICloneable, IEquatable<ANode>
    {
        [JsonIgnore]
        public int depth = 0;
        [JsonIgnore]
        public ANode? prv = null;
        public List<int> Parents { get; set; } = new();
        public int Id { get; set; }
        public int[][]? State { get; set; }
        public int G { get; set; } = -1;
        public int H { get; set; } = -1;
        public int F { get; set; } = -1;
        [JsonIgnore]
        public List<ANode>? SubNodes = null;
        public List<int> SubNodesIds { get; set; } = new();

        public ANode() { }

        public ANode(int dimensions)
        {
            State = new int[dimensions][];
            int k = 0;
            for (int i = 0; i < dimensions; i++)
            {
                State[i] = new int[dimensions];
                for (int j = 0; j < dimensions; j++)
                {
                    State[i][j] = k++;
                }
            }
        }

        public object Clone()
        {
            ANode newNode = new();
            newNode.depth = depth;
            newNode.Parents = new(Parents);
            newNode.Id = Id;
            if (State != null)
            {
                newNode.State = new int[State.Length][];
                for (int i = 0; i < State[0].Length; i++)
                {
                    newNode.State[i] = (int[])State[i].Clone();
                }
            }
            newNode.SubNodesIds = new(SubNodesIds);
            return newNode;
        }

        public bool Equals(ANode? other)
        {
            if (other == null) return false;
            if (Id != other.Id || depth != other.depth) return false;
            if (!Parents.SequenceEqual(other.Parents)) return false;
            if (State == null && other.State == State) return true;
            if (State == null || other.State == null) return false; 
            if (State.Length != other.State.Length || State[0].Length != other.State[0].Length) return false;
            for (int i = 0; i < State.Length; i++)
            {
                if (!State[i].SequenceEqual(other.State[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
