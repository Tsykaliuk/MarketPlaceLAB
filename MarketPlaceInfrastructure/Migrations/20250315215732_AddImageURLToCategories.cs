using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketPlaceInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImageURLToCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "categories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "categories");
        }
    }
}
