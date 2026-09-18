using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IsoDof.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityResponsibleToDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QualityResponsibleUserId",
                table: "Departments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_QualityResponsibleUserId",
                table: "Departments",
                column: "QualityResponsibleUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_AppUsers_QualityResponsibleUserId",
                table: "Departments",
                column: "QualityResponsibleUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_AppUsers_QualityResponsibleUserId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_QualityResponsibleUserId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "QualityResponsibleUserId",
                table: "Departments");
        }
    }
}
