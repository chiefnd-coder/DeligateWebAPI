using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeligateWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class init11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Errormessage",
                table: "UserTracker",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Errormessage",
                table: "UserTracker");
        }
    }
}
