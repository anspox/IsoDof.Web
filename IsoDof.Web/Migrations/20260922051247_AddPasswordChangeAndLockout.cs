using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IsoDof.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordChangeAndLockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "AppUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEndUtc",
                table: "AppUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Eski sürümde toplu içe aktarılan kullanıcılara herkese açık depoda yazılı sabit bir şifre
            // atanıyordu. Hangi hesapların bu şifreyi kullandığı hash'ten anlaşılamadığı için mevcut tüm
            // kullanıcılar bir sonraki girişte şifrelerini değiştirmek zorundadır.
            migrationBuilder.Sql("UPDATE AppUsers SET MustChangePassword = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "LockoutEndUtc",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AppUsers");
        }
    }
}
