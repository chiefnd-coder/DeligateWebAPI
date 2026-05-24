using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeligateWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class Init15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyResetCodeRequestCount",
                table: "Register",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DailyResetCodeWindowStart",
                table: "Register",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastResetCodeRequestedAt",
                table: "Register",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResetCodeRequestCount",
                table: "Register",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetCodeRequestWindowStart",
                table: "Register",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyResetCodeRequestCount",
                table: "Register");

            migrationBuilder.DropColumn(
                name: "DailyResetCodeWindowStart",
                table: "Register");

            migrationBuilder.DropColumn(
                name: "LastResetCodeRequestedAt",
                table: "Register");

            migrationBuilder.DropColumn(
                name: "ResetCodeRequestCount",
                table: "Register");

            migrationBuilder.DropColumn(
                name: "ResetCodeRequestWindowStart",
                table: "Register");
        }
    }
}
