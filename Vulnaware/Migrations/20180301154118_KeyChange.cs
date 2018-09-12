using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Vulnaware.Migrations
{
    public partial class KeyChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Configuration_AspNetUsers_ApplicationUserId",
                table: "Configuration");

            migrationBuilder.DropIndex(
                name: "IX_Configuration_ApplicationUserId",
                table: "Configuration");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Configuration");

            migrationBuilder.AlterColumn<string>(
                name: "AspNetUserID",
                table: "Configuration",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Configuration_AspNetUserID",
                table: "Configuration",
                column: "AspNetUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Configuration_AspNetUsers_AspNetUserID",
                table: "Configuration",
                column: "AspNetUserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Configuration_AspNetUsers_AspNetUserID",
                table: "Configuration");

            migrationBuilder.DropIndex(
                name: "IX_Configuration_AspNetUserID",
                table: "Configuration");

            migrationBuilder.AlterColumn<string>(
                name: "AspNetUserID",
                table: "Configuration",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Configuration",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Configuration_ApplicationUserId",
                table: "Configuration",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Configuration_AspNetUsers_ApplicationUserId",
                table: "Configuration",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
