using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class RedesignPantryHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PantryStaplesJson",
                table: "DeviceSettings",
                newName: "AlwaysAvailableIngredientsJson");

            migrationBuilder.AddColumn<bool>(
                name: "IgnorePantry",
                table: "DeviceSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IgnorePantry",
                table: "DeviceSettings");

            migrationBuilder.RenameColumn(
                name: "AlwaysAvailableIngredientsJson",
                table: "DeviceSettings",
                newName: "PantryStaplesJson");
        }
    }
}
