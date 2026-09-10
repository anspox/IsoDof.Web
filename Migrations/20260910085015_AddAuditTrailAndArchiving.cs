using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IsoDof.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTrailAndArchiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                table: "Dofs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Dofs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchivedByUserId",
                table: "Dofs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Dofs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DofStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DofId = table.Column<int>(type: "int", nullable: false),
                    OldStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DofStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DofStatusHistories_AppUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DofStatusHistories_Dofs_DofId",
                        column: x => x.DofId,
                        principalTable: "Dofs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dofs_ArchivedByUserId",
                table: "Dofs",
                column: "ArchivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DofStatusHistories_ChangedByUserId",
                table: "DofStatusHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DofStatusHistories_DofId",
                table: "DofStatusHistories",
                column: "DofId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dofs_AppUsers_ArchivedByUserId",
                table: "Dofs",
                column: "ArchivedByUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dofs_AppUsers_ArchivedByUserId",
                table: "Dofs");

            migrationBuilder.DropTable(
                name: "DofStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Dofs_ArchivedByUserId",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "Dofs");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Dofs");
        }
    }
}
