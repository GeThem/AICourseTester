using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICourseTester.Migrations
{
    /// <inheritdoc />
    public partial class ABandUSER : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PfpPath",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxValue",
                table: "AlphaBeta",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Template",
                table: "AlphaBeta",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PfpPath",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "AlphaBeta");

            migrationBuilder.DropColumn(
                name: "Template",
                table: "AlphaBeta");
        }
    }
}
