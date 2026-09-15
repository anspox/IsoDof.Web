using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IsoDof.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRootCauseEffectivenessAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EffectivenessCheckDueDate",
                table: "Dofs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectivenessCheckedAt",
                table: "Dofs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectivenessCheckedByUserId",
                table: "Dofs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EffectivenessNotes",
                table: "Dofs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectivenessReminderSentAt",
                table: "Dofs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectivenessResult",
                table: "Dofs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastSlaReminderDaysBefore",
                table: "Dofs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootCauseAnalysis",
                table: "Dofs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RootCauseCategory",
                table: "Dofs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootCauseWhy1",
                table: "Dofs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootCauseWhy2",
                table: "Dofs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootCauseWhy3",
                table: "Dofs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootCauseWhy4",
                table: "Dofs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootCauseWhy5",
                table: "Dofs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DofTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TitleTemplate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DofTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DofTemplates_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dofs_EffectivenessCheckedByUserId",
                table: "Dofs",
                column: "EffectivenessCheckedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DofTemplates_DepartmentId",
                table: "DofTemplates",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dofs_AppUsers_EffectivenessCheckedByUserId",
                table: "Dofs",
                column: "EffectivenessCheckedByUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dofs_AppUsers_EffectivenessCheckedByUserId",
                table: "Dofs");

            migrationBuilder.DropTable(
                name: "DofTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Dofs_EffectivenessCheckedByUserId",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "EffectivenessCheckDueDate",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "EffectivenessCheckedAt",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "EffectivenessCheckedByUserId",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "EffectivenessNotes",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "EffectivenessReminderSentAt",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "EffectivenessResult",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "LastSlaReminderDaysBefore",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "RootCauseAnalysis",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "RootCauseCategory",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "RootCauseWhy1",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "RootCauseWhy2",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "RootCauseWhy3",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "RootCauseWhy4",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "RootCauseWhy5",
                table: "Dofs");
        }
    }
}
