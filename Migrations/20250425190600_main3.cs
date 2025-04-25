using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICourseTester.Migrations
{
    /// <inheritdoc />
    public partial class main3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserSolution",
                table: "Fifteens",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TreeDepth",
                table: "AlphaBeta",
                type: "integer",
                nullable: false,
                defaultValue: 3,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<bool>(
                name: "IsSolved",
                table: "AlphaBeta",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserSolution",
                table: "Fifteens");

            migrationBuilder.AlterColumn<int>(
                name: "TreeDepth",
                table: "AlphaBeta",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 3);

            migrationBuilder.AlterColumn<bool>(
                name: "IsSolved",
                table: "AlphaBeta",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);
        }
    }
}
