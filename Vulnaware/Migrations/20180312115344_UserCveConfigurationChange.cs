using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Vulnaware.Migrations
{
    public partial class UserCveConfigurationChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCveConfiguration_AspNetUsers_AspNetUserID",
                table: "UserCveConfiguration");

            migrationBuilder.DropTable(
                name: "ProductConfiguration");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserCveConfiguration",
                table: "UserCveConfiguration");

            migrationBuilder.DropColumn(
                name: "AspNetUserID",
                table: "UserCveConfiguration");

            migrationBuilder.AddColumn<int>(
                name: "ConfigurationID",
                table: "UserCveConfiguration",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserCveConfiguration",
                table: "UserCveConfiguration",
                columns: new[] { "ConfigurationID", "ProductID", "CveID" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserCveConfiguration_Configuration_ConfigurationID",
                table: "UserCveConfiguration",
                column: "ConfigurationID",
                principalTable: "Configuration",
                principalColumn: "ConfigurationID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCveConfiguration_Configuration_ConfigurationID",
                table: "UserCveConfiguration");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserCveConfiguration",
                table: "UserCveConfiguration");

            migrationBuilder.DropColumn(
                name: "ConfigurationID",
                table: "UserCveConfiguration");

            migrationBuilder.AddColumn<string>(
                name: "AspNetUserID",
                table: "UserCveConfiguration",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserCveConfiguration",
                table: "UserCveConfiguration",
                columns: new[] { "AspNetUserID", "ProductID", "CveID" });

            migrationBuilder.CreateTable(
                name: "ProductConfiguration",
                columns: table => new
                {
                    ConfigurationID = table.Column<int>(nullable: false),
                    ProductID = table.Column<int>(nullable: false),
                    Notes = table.Column<string>(nullable: true),
                    Resolved = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductConfiguration", x => new { x.ConfigurationID, x.ProductID });
                    table.ForeignKey(
                        name: "FK_ProductConfiguration_Configuration_ConfigurationID",
                        column: x => x.ConfigurationID,
                        principalTable: "Configuration",
                        principalColumn: "ConfigurationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductConfiguration_Product_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Product",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductConfiguration_ProductID",
                table: "ProductConfiguration",
                column: "ProductID");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCveConfiguration_AspNetUsers_AspNetUserID",
                table: "UserCveConfiguration",
                column: "AspNetUserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
