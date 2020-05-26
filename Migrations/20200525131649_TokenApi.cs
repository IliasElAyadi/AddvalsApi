using Microsoft.EntityFrameworkCore.Migrations;

namespace AddvalsApi.Migrations
{
    public partial class TokenApi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Token",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "idSkytape",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "TokenApi",
                table: "Users",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TokenSkytap",
                table: "Users",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "idSkytap",
                table: "Users",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenApi",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TokenSkytap",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "idSkytap",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "idSkytape",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
