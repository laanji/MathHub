using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogAndDemoCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "learning");

            migrationBuilder.CreateTable(
                name: "demo_checkouts",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_title = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    scenario = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    gateway_reference = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demo_checkouts", x => x.id);
                    table.CheckConstraint("ck_demo_amount", "amount > 0");
                    table.ForeignKey(
                        name: "FK_demo_checkouts_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "groups",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_demo_checkouts_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teachers",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    bio = table.Column<string>(type: "text", nullable: false),
                    is_placeholder = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teachers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_demo_checkouts_course_id",
                schema: "billing",
                table: "demo_checkouts",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_demo_checkouts_user_id_request_id",
                schema: "billing",
                table: "demo_checkouts",
                columns: new[] { "user_id", "request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "demo_checkouts",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "teachers",
                schema: "learning");
        }
    }
}
