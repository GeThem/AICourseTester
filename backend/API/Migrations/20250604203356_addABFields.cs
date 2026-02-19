using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICourseTester.Migrations
{
    /// <inheritdoc />
    public partial class addABFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "AlphaBeta",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserPath",
                table: "AlphaBeta",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Path",
                table: "AlphaBeta");

            migrationBuilder.DropColumn(
                name: "UserPath",
                table: "AlphaBeta");
        }
    }
}
