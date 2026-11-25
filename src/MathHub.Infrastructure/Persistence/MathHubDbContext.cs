using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
namespace MathHub.Infrastructure.Persistence;
public sealed class MathHubDbContext(DbContextOptions<MathHubDbContext> options) : DbContext(options)
{
    public DbSet<AccountRow> Accounts => Set<AccountRow>();
    public DbSet<ParentLinkRow> ParentLinks => Set<ParentLinkRow>();
    public DbSet<AuthSessionRow> AuthSessions => Set<AuthSessionRow>();
    public DbSet<RefreshTokenRow> RefreshTokens => Set<RefreshTokenRow>();
    public DbSet<QuestionRow> Questions => Set<QuestionRow>();
    public DbSet<TestRow> Tests => Set<TestRow>();
    public DbSet<AnswerRow> Answers => Set<AnswerRow>();
    public DbSet<CourseRow> Courses => Set<CourseRow>();
    public DbSet<GroupRow> Groups => Set<GroupRow>();
    public DbSet<EnrollmentRow> Enrollments => Set<EnrollmentRow>();
    public DbSet<PaymentRow> Payments => Set<PaymentRow>();
    public DbSet<ReceiptRow> Receipts => Set<ReceiptRow>();
    public DbSet<TeacherRow> Teachers => Set<TeacherRow>();
    public DbSet<DemoCheckoutRow> DemoCheckouts => Set<DemoCheckoutRow>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AccountRow>().ToTable("users", "identity", t => t.HasCheckConstraint("ck_users_score", "math_score BETWEEN 0 AND 100"));
        b.Entity<AccountRow>().HasIndex(x => x.Email).IsUnique();
        b.Entity<AccountRow>().Property(x => x.Email).HasMaxLength(254);
        b.Entity<AccountRow>().Property(x => x.DisplayName).HasMaxLength(100);
        b.Entity<ParentLinkRow>().ToTable("parent_links", "identity");
        b.Entity<ParentLinkRow>().HasIndex(x => x.ChildId).IsUnique();
        b.Entity<ParentLinkRow>().HasOne<AccountRow>().WithMany().HasForeignKey(x => x.ChildId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ParentLinkRow>().HasOne<AccountRow>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AuthSessionRow>().ToTable("auth_sessions", "identity");
        b.Entity<AuthSessionRow>().HasOne<AccountRow>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RefreshTokenRow>().ToTable("refresh_tokens", "identity");
        b.Entity<RefreshTokenRow>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<RefreshTokenRow>().HasOne<AuthSessionRow>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<QuestionRow>().ToTable("questions", "assessment", t => t.HasCheckConstraint("ck_questions_difficulty", "difficulty BETWEEN 0 AND 100"));
        b.Entity<QuestionRow>().Property(x => x.OptionsJson).HasColumnType("jsonb");
        b.Entity<QuestionRow>().HasIndex(x => new { x.Context, x.Active, x.Difficulty });
        b.Entity<TestRow>().ToTable("tests", "assessment");
        b.Entity<TestRow>().HasOne<AccountRow>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TestRow>().HasOne<QuestionRow>().WithMany().HasForeignKey(x => x.CurrentQuestionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<TestRow>().HasIndex(x => x.StudentId).IsUnique().HasFilter("status = 'InProgress'");
        b.Entity<AnswerRow>().ToTable("test_answers", "assessment");
        b.Entity<AnswerRow>().Property(x => x.ResponseJson).HasColumnType("jsonb");
        b.Entity<AnswerRow>().HasIndex(x => new { x.TestId, x.QuestionId }).IsUnique();
        b.Entity<AnswerRow>().HasIndex(x => new { x.TestId, x.RequestId }).IsUnique();
        b.Entity<AnswerRow>().HasOne<TestRow>().WithMany().HasForeignKey(x => x.TestId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AnswerRow>().HasOne<QuestionRow>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CourseRow>().ToTable("courses", "groups", t => t.HasCheckConstraint("ck_courses_price", "price_per_lesson > 0"));
        b.Entity<CourseRow>().Property(x => x.PricePerLesson).HasPrecision(12, 2);
        b.Entity<CourseRow>().Property(x => x.SlotsJson).HasColumnType("jsonb");
        b.Entity<GroupRow>().ToTable("cohort_groups", "groups");
        b.Entity<GroupRow>().HasOne<CourseRow>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GroupRow>().HasIndex(x => new { x.CourseId, x.AgeCohort, x.Slot });
        b.Entity<EnrollmentRow>().ToTable("enrollments", "groups", t => t.HasCheckConstraint("ck_enrollments_score", "score_at_entry BETWEEN 0 AND 100"));
        b.Entity<EnrollmentRow>().Property(x => x.PriceSnapshot).HasPrecision(12, 2);
        b.Entity<EnrollmentRow>().HasOne<AccountRow>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EnrollmentRow>().HasOne<CourseRow>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EnrollmentRow>().HasOne<GroupRow>().WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EnrollmentRow>().HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique().HasFilter("status IN ('Active', 'Reserved')");
        b.Entity<EnrollmentRow>().HasIndex(x => new { x.GroupId, x.Status, x.ReservedUntil });
        b.Entity<PaymentRow>().ToTable("payments", "billing");
        b.Entity<PaymentRow>().Property(x => x.Amount).HasPrecision(12, 2);
        b.Entity<PaymentRow>().HasIndex(x => new { x.StudentId, x.RequestId }).IsUnique();
        b.Entity<PaymentRow>().HasIndex(x => x.EnrollmentId).IsUnique();
        b.Entity<PaymentRow>().HasOne<EnrollmentRow>().WithMany().HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ReceiptRow>().ToTable("request_receipts", "platform");
        b.Entity<ReceiptRow>().Property(x => x.ResponseJson).HasColumnType("jsonb");
        b.Entity<ReceiptRow>().HasIndex(x => new { x.StudentId, x.Scope, x.RequestId }).IsUnique();
        b.Entity<TeacherRow>().ToTable("teachers", "learning");
        b.Entity<TeacherRow>().Property(x => x.DisplayName).HasMaxLength(100);
        b.Entity<DemoCheckoutRow>().ToTable("demo_checkouts", "billing", t => t.HasCheckConstraint("ck_demo_amount", "amount > 0"));
        b.Entity<DemoCheckoutRow>().Property(x => x.Amount).HasPrecision(12, 2);
        b.Entity<DemoCheckoutRow>().HasIndex(x => new { x.UserId, x.RequestId }).IsUnique();
        b.Entity<DemoCheckoutRow>().HasOne<AccountRow>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DemoCheckoutRow>().HasOne<CourseRow>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
    }
}
