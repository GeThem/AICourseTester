namespace AICourseTester.DTO
{
    public sealed class UserModifyDTO
    {
        public UserModifyDTO() { }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? Name { get; set; }
        public string? SecondName { get; set; }
        public string? Patronymic { get; set; }
        public int? GroupId { get; set; }

        public bool RemoveGroup { get; set; } = false;
    }
}
