using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Manasa.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminRoleSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "a1000000-0000-0000-0000-000000000001", "a1000000-0000-0000-0000-000000000001", "Administrator", "ADMINISTRATOR" },
                    { "a1000000-0000-0000-0000-000000000002", "a1000000-0000-0000-0000-000000000002", "Teacher", "TEACHER" },
                    { "a1000000-0000-0000-0000-000000000003", "a1000000-0000-0000-0000-000000000003", "Student", "STUDENT" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a1000000-0000-0000-0000-000000000001");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a1000000-0000-0000-0000-000000000002");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a1000000-0000-0000-0000-000000000003");
        }
    }
}
