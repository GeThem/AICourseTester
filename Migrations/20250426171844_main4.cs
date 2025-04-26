using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICourseTester.Migrations
{
    /// <inheritdoc />
    public partial class main4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TreeDepth",
                table: "Fifteens",
                newName: "TreeHeight");

            migrationBuilder.RenameColumn(
                name: "TreeDepth",
                table: "AlphaBeta",
                newName: "TreeHeight");

            migrationBuilder.AddColumn<string>(
                name: "UserSolution",
                table: "AlphaBeta",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION InsertAlphaBeta() RETURNS trigger AS $InsertAlphaBeta$
                    BEGIN
                       INSERT INTO "AlphaBeta"(
                        "UserId")
                    values (
                        NEW."Id"
                        );

                    RETURN NEW;
                END;
                   $InsertAlphaBeta$ LANGUAGE plpgsql;

                CREATE OR REPLACE TRIGGER insertAlphaBeta AFTER INSERT ON "AspNetUsers"
                FOR EACH ROW EXECUTE PROCEDURE InsertAlphaBeta();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION InsertAlphaBeta;
                DROP TRIGGER insertAlphaBeta;
                """);

            migrationBuilder.DropColumn(
                name: "UserSolution",
                table: "AlphaBeta");

            migrationBuilder.RenameColumn(
                name: "TreeHeight",
                table: "Fifteens",
                newName: "TreeDepth");

            migrationBuilder.RenameColumn(
                name: "TreeHeight",
                table: "AlphaBeta",
                newName: "TreeDepth");
        }
    }
}
