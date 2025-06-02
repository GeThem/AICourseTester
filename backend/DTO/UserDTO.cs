using AICourseTester.Models;

namespace AICourseTester.DTO
{
    public class UserDTO
    {
        public required string Id { get; set; }
        public string? Name { get; set; }
        public string? SecondName { get; set; }
        public string? Patronymic { get; set; }
        public string? Group { get; set; }
    }
}
