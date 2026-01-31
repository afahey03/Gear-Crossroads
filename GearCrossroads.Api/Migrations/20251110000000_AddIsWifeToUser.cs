using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GearCrossroads.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsWifeToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWife",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWife",
                table: "AspNetUsers");
        }
    }
}
