using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TeacherPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "photo_path",
                schema: "learning",
                table: "teachers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "photo_path",
                schema: "learning",
                table: "teachers");
        }
    }
}
