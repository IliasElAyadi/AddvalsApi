using Microsoft.EntityFrameworkCore.Migrations;

namespace AddvalsApi.Migrations
{
    public partial class idSkytap : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idSkytape",
                table: "Users",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "idSkytape",
                table: "Users");
        }
    }
}
