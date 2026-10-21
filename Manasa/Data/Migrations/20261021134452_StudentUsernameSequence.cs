using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manasa.Data.Migrations
{
    /// <inheritdoc />
    public partial class StudentUsernameSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "StudentUsernameSequence");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "StudentUsernameSequence");
        }
    }
}
