namespace AICourseTester.DTO
{
    //
    // Summary:
    //     The request type for the "/register" endpoint added by Microsoft.AspNetCore.Routing.IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi``1(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder).
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
