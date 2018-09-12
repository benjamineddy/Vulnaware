using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Vulnaware.Migrations
{
    public partial class AddedUserCveConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Status",
                columns: table => new
                {
                    StatusID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    StatusName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Status", x => x.StatusID);
                });

            migrationBuilder.CreateTable(
                name: "UserCveConfiguration",
                columns: table => new
                {
                    AspNetUserID = table.Column<string>(nullable: false),
                    ProductID = table.Column<int>(nullable: false),
                    CveID = table.Column<int>(nullable: false),
                    StatusID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCveConfiguration", x => new { x.AspNetUserID, x.ProductID, x.CveID });
                    table.ForeignKey(
                        name: "FK_UserCveConfiguration_AspNetUsers_AspNetUserID",
                        column: x => x.AspNetUserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCveConfiguration_Cve_CveID",
                        column: x => x.CveID,
                        principalTable: "Cve",
                        principalColumn: "CveID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCveConfiguration_Product_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Product",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCveConfiguration_Status_StatusID",
                        column: x => x.StatusID,
                        principalTable: "Status",
                        principalColumn: "StatusID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCveConfiguration_CveID",
                table: "UserCveConfiguration",
                column: "CveID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCveConfiguration_ProductID",
                table: "UserCveConfiguration",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCveConfiguration_StatusID",
                table: "UserCveConfiguration",
                column: "StatusID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCveConfiguration");

            migrationBuilder.DropTable(
                name: "Status");
        }
    }
}
