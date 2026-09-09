using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IsoDof.Web.Migrations
{
    /// <inheritdoc />
    public partial class MakeAppUserDepartmentRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Departments_DepartmentId",
                table: "AppUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Dofs_AppUsers_AssignedToUserId",
                table: "Dofs");

            migrationBuilder.DropForeignKey(
                name: "FK_Dofs_AppUsers_CreatedByUserId",
                table: "Dofs");

            migrationBuilder.DropForeignKey(
                name: "FK_Dofs_Departments_DepartmentId",
                table: "Dofs");

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "AppUsers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Departments_DepartmentId",
                table: "AppUsers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dofs_AppUsers_AssignedToUserId",
                table: "Dofs",
                column: "AssignedToUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dofs_AppUsers_CreatedByUserId",
                table: "Dofs",
                column: "CreatedByUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Dofs_Departments_DepartmentId",
                table: "Dofs",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Departments_DepartmentId",
                table: "AppUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Dofs_AppUsers_AssignedToUserId",
                table: "Dofs");

            migrationBuilder.DropForeignKey(
                name: "FK_Dofs_AppUsers_CreatedByUserId",
                table: "Dofs");

            migrationBuilder.DropForeignKey(
                name: "FK_Dofs_Departments_DepartmentId",
                table: "Dofs");

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "AppUsers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Departments_DepartmentId",
                table: "AppUsers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Dofs_AppUsers_AssignedToUserId",
                table: "Dofs",
                column: "AssignedToUserId",
                principalTable: "AppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Dofs_AppUsers_CreatedByUserId",
                table: "Dofs",
                column: "CreatedByUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Dofs_Departments_DepartmentId",
                table: "Dofs",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
