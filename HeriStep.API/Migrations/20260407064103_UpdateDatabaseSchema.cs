using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeriStep.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Stalls");

            migrationBuilder.DropColumn(
                name: "subscription_level",
                table: "Stalls");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Subscriptions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Longitude",
                table: "Stalls",
                newName: "longitude");

            migrationBuilder.RenameColumn(
                name: "Latitude",
                table: "Stalls",
                newName: "latitude");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Stalls",
                newName: "id");

            migrationBuilder.AddColumn<int>(
                name: "stall_id",
                table: "Subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "Stalls",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "id", "full_name", "password_hash", "role", "username" },
                values: new object[] { 1, "System Admin", "$2a$11$n/A1qU55YyC7o2s1K0kC1O/0wA1oHh5X2w3E1z8e7H7A9R2lX4m", "Admin", "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_stall_id",
                table: "Subscriptions",
                column: "stall_id");

            migrationBuilder.CreateIndex(
                name: "IX_Stalls_TourID",
                table: "Stalls",
                column: "TourID");

            migrationBuilder.AddForeignKey(
                name: "FK_Stalls_Tours_TourID",
                table: "Stalls",
                column: "TourID",
                principalTable: "Tours",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Stalls_stall_id",
                table: "Subscriptions",
                column: "stall_id",
                principalTable: "Stalls",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stalls_Tours_TourID",
                table: "Stalls");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Stalls_stall_id",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_stall_id",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Stalls_TourID",
                table: "Stalls");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "stall_id",
                table: "Subscriptions");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Subscriptions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "longitude",
                table: "Stalls",
                newName: "Longitude");

            migrationBuilder.RenameColumn(
                name: "latitude",
                table: "Stalls",
                newName: "Latitude");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Stalls",
                newName: "Id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "Stalls",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Stalls",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "subscription_level",
                table: "Stalls",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
