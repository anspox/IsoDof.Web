using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IsoDof.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSicilNoToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SicilNo",
                table: "AppUsers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_SicilNo",
                table: "AppUsers",
                column: "SicilNo",
                unique: true,
                filter: "[SicilNo] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppUsers_SicilNo",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "SicilNo",
                table: "AppUsers");
        }
    }
}
