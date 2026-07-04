using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Josapar.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderClientGeneratedId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientGeneratedId",
                table: "Orders",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RepresentativeId_ClientGeneratedId",
                table: "Orders",
                columns: new[] { "RepresentativeId", "ClientGeneratedId" },
                unique: true,
                filter: "`ClientGeneratedId` IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_RepresentativeId_ClientGeneratedId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClientGeneratedId",
                table: "Orders");
        }
    }
}
