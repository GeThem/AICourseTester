namespace AICourseTester.DTO
{
    public sealed class RegReq
    {
        public RegReq() { }
        public required string UserName { get; init; }
        public required string Password { get; init; }
        public string? Name { get; init; }
        public string? SecondName { get; init; }
        public string? Patronymic { get; init; }
        public int? GroupId { get; init; }
    }
}
