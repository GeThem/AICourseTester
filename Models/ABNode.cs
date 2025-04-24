using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mono.TextTemplating;
using System.Text.Json.Serialization;

namespace AICourseTester.Models
{
    public class ABNode : Node<ABNode>
    {
        [JsonIgnore]
        public int depth = 0;
        [JsonIgnore]
        public ABNode? prv { get; set; } = null;
        public int Id { get; set; }
        public int A { get; set; } = int.MinValue;
        public int B { get; set; } = int.MaxValue;

        public List<ABNode>? SubNodes { get; set; } = null;

        public object Clone()
        {
            ABNode newNode = new();
            newNode.depth = depth;
            newNode.A = A;
            newNode.B = B;
            newNode.Id = Id;
            return newNode;
        }

        public bool Equals(ABNode? other)
        {
            if (other == null) return false;
            if (Id != other.Id || depth != other.depth) return false;
            if (prv != other.prv) return false;
            if (A != other.A || B != other.B) return false;
            return true;
        }

        public void Reset()
        {
            A = int.MinValue;
            B = int.MaxValue;
        }
    }
}
