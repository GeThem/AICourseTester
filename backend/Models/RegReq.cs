namespace AICourseTester.backend.Models
{
    //
    // Summary:
    //     The request type for the "/register" endpoint added by Microsoft.AspNetCore.Routing.IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi``1(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder).
    public sealed class RegReq
    {
        public RegReq() { }

        public required string UserName { get; init; }

        public required string Password { get; init; }
    }
}
