using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeligateWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class Init18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserUniqueId",
                table: "RegisterArchive",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserUniqueId",
                table: "DeletedAccount",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserUniqueId",
                table: "RegisterArchive");

            migrationBuilder.DropColumn(
                name: "UserUniqueId",
                table: "DeletedAccount");
        }
    }
}
