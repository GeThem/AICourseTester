using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AICourseTester.Models
{
    public class ProblemTree<T> where T : Node
    {
        public T? Head { get; set; }
    }

    public interface Node
    {
        public List<int> SubNodesIds { get; set; }
    }
}
