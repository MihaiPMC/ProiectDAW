using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProiectDAW.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtToGroupMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "GroupMessages",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "GroupMessages");
        }
    }
}

