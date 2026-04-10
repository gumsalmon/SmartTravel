using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeriStep.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabaseSchema_V2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "TicketPackages",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "FlagIconUrl",
                table: "Languages",
                newName: "flag_icon_url");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "Tours",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Tours",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "TicketPackages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Subscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "StallVisits",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_server",
                table: "StallVisits",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "Stalls",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "StallContents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_processed",
                table: "StallContents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "StallContents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "ProductTranslations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_processed",
                table: "ProductTranslations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "ProductTranslations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "Languages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Languages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "id",
                keyValue: 1,
                column: "updated_at",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "TicketPackages");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "created_at_server",
                table: "StallVisits");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "Stalls");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "StallContents");

            migrationBuilder.DropColumn(
                name: "is_processed",
                table: "StallContents");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "StallContents");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "ProductTranslations");

            migrationBuilder.DropColumn(
                name: "is_processed",
                table: "ProductTranslations");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "ProductTranslations");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Languages");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "TicketPackages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "flag_icon_url",
                table: "Languages",
                newName: "FlagIconUrl");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "StallVisits",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier")
                .Annotation("SqlServer:Identity", "1, 1");
        }
    }
}
