using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Vulnaware.Migrations
{
    public partial class AddedProductConcatenatedUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Product_Concatenated",
                table: "Product");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Concatenated",
                table: "Product",
                column: "Concatenated",
                unique: true,
                filter: "[Concatenated] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Product_Concatenated",
                table: "Product");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Concatenated",
                table: "Product",
                column: "Concatenated");
        }
    }
}
