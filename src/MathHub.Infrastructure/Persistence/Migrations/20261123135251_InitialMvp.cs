using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "groups");

            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.EnsureSchema(
                name: "assessment");

            migrationBuilder.EnsureSchema(
                name: "platform");

            migrationBuilder.CreateTable(
                name: "courses",
                schema: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    context = table.Column<string>(type: "text", nullable: false),
                    price_per_lesson = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    slots_json = table.Column<string>(type: "jsonb", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses", x => x.id);
                    table.CheckConstraint("ck_courses_price", "price_per_lesson > 0");
                });

            migrationBuilder.CreateTable(
                name: "questions",
                schema: "assessment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    context = table.Column<string>(type: "text", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    topic = table.Column<string>(type: "text", nullable: false),
                    options_json = table.Column<string>(type: "jsonb", nullable: false),
                    correct_answer = table.Column<string>(type: "text", nullable: false),
                    difficulty = table.Column<double>(type: "double precision", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.id);
                    table.CheckConstraint("ck_questions_difficulty", "difficulty BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "request_receipts",
                schema: "platform",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "text", nullable: false),
                    payload_hash = table.Column<string>(type: "text", nullable: false),
                    response_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_receipts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    failed_logins = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    math_score = table.Column<int>(type: "integer", nullable: true),
                    last_test_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    waiting_since = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_score", "math_score BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "cohort_groups",
                schema: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    age_cohort = table.Column<string>(type: "text", nullable: false),
                    slot = table.Column<string>(type: "text", nullable: false),
                    delta_limit = table.Column<int>(type: "integer", nullable: false),
                    archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cohort_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_cohort_groups_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "groups",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "auth_sessions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_auth_sessions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parent_links",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_email = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consent_version = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parent_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_parent_links_users_child_id",
                        column: x => x.child_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parent_links_users_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tests",
                schema: "assessment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    context = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    theta = table.Column<double>(type: "double precision", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    current_question_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    score = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tests", x => x.id);
                    table.ForeignKey(
                        name: "FK_tests_questions_current_question_id",
                        column: x => x.current_question_id,
                        principalSchema: "assessment",
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tests_users_student_id",
                        column: x => x.student_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "enrollments",
                schema: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score_at_entry = table.Column<int>(type: "integer", nullable: false),
                    price_snapshot = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    reserved_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enrollments", x => x.id);
                    table.CheckConstraint("ck_enrollments_score", "score_at_entry BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_enrollments_cohort_groups_group_id",
                        column: x => x.group_id,
                        principalSchema: "groups",
                        principalTable: "cohort_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollments_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "groups",
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollments_users_student_id",
                        column: x => x.student_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_auth_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "identity",
                        principalTable: "auth_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_answers",
                schema: "assessment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_version = table.Column<int>(type: "integer", nullable: false),
                    raw_answer = table.Column<string>(type: "text", nullable: false),
                    difficulty_snapshot = table.Column<double>(type: "double precision", nullable: false),
                    correct = table.Column<bool>(type: "boolean", nullable: false),
                    theta_after = table.Column<double>(type: "double precision", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    response_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_test_answers_questions_question_id",
                        column: x => x.question_id,
                        principalSchema: "assessment",
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_test_answers_tests_test_id",
                        column: x => x.test_id,
                        principalSchema: "assessment",
                        principalTable: "tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    gateway_reference = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_payments_enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalSchema: "groups",
                        principalTable: "enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_sessions_user_id",
                schema: "identity",
                table: "auth_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_cohort_groups_course_id_age_cohort_slot",
                schema: "groups",
                table: "cohort_groups",
                columns: new[] { "course_id", "age_cohort", "slot" });

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_course_id",
                schema: "groups",
                table: "enrollments",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_group_id_status_reserved_until",
                schema: "groups",
                table: "enrollments",
                columns: new[] { "group_id", "status", "reserved_until" });

            migrationBuilder.CreateIndex(
                name: "IX_enrollments_student_id_course_id",
                schema: "groups",
                table: "enrollments",
                columns: new[] { "student_id", "course_id" },
                unique: true,
                filter: "status IN ('Active', 'Reserved')");

            migrationBuilder.CreateIndex(
                name: "IX_parent_links_child_id",
                schema: "identity",
                table: "parent_links",
                column: "child_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parent_links_parent_id",
                schema: "identity",
                table: "parent_links",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_enrollment_id",
                schema: "billing",
                table: "payments",
                column: "enrollment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_student_id_request_id",
                schema: "billing",
                table: "payments",
                columns: new[] { "student_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_questions_context_active_difficulty",
                schema: "assessment",
                table: "questions",
                columns: new[] { "context", "active", "difficulty" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_session_id",
                schema: "identity",
                table: "refresh_tokens",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                schema: "identity",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_receipts_student_id_scope_request_id",
                schema: "platform",
                table: "request_receipts",
                columns: new[] { "student_id", "scope", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_answers_question_id",
                schema: "assessment",
                table: "test_answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_test_answers_test_id_question_id",
                schema: "assessment",
                table: "test_answers",
                columns: new[] { "test_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_test_answers_test_id_request_id",
                schema: "assessment",
                table: "test_answers",
                columns: new[] { "test_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tests_current_question_id",
                schema: "assessment",
                table: "tests",
                column: "current_question_id");

            migrationBuilder.CreateIndex(
                name: "IX_tests_student_id",
                schema: "assessment",
                table: "tests",
                column: "student_id",
                unique: true,
                filter: "status = 'InProgress'");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                schema: "identity",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parent_links",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "request_receipts",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "test_answers",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "enrollments",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "auth_sessions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "tests",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "cohort_groups",
                schema: "groups");

            migrationBuilder.DropTable(
                name: "questions",
                schema: "assessment");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "courses",
                schema: "groups");
        }
    }
}
