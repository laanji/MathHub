using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MathHub.Modules.Assessment.Application;
namespace MathHub.Infrastructure.Persistence;
public static class SeedData
{
    public static readonly ContextView[] Contexts = [new("starter", "Початкова математика", 7), new("elementary", "Дроби та відсотки", 25), new("intermediate", "Алгебра · 7–8 класи", 45), new("advanced", "Алгебра · 9–10 класи", 65), new("proficient", "Підготовка до НМТ", 82), new("master", "Поглиблена математика", 95)];
    public static async Task InitializeAsync(MathHubDbContext db, CancellationToken ct = default)
    {
        // Serialized by startup advisory lock: safe when two API instances start together.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(610041)", ct);
        if (!await db.Questions.AnyAsync(ct))
        {
            var bounds = new[] { (0, 14), (15, 34), (35, 54), (55, 74), (75, 89), (90, 100) };
            for (var context = 0; context < Contexts.Length; context++)
                for (var i = 1; i <= 40; i++)
                {
                    var a = i + 2; var c = i % 7 + 2;
                    var (prompt, answer, topic) = context switch
                    {
                        0 => ($"Обчисліть: {i % 12 + 1} + {c}.", i % 12 + 1 + c, "Додавання"),
                        1 => ($"Знайдіть {c * 5}% від числа {a * 20}.", c * a, "Відсотки"),
                        2 => ($"Розв’яжіть рівняння: 2x + {c} = {2 * a + c}.", a, "Лінійні рівняння"),
                        3 => ($"Який додатний корінь рівняння (x − {a})(x + {c}) = 0?", a, "Квадратні рівняння"),
                        4 => ($"Знайдіть f′(2), якщо f(x) = x³ + {a}x.", 12 + a, "Похідна"),
                        _ => ($"Обчисліть визначник матриці з рядками ({a}, {c}) і (2, 3).", a * 3 - c * 2, "Визначники")
                    };
                    var options = new[] { answer, answer + 1, answer - 2, answer + 3 }.OrderBy(v => ((v * 17 + i * 7) % 31 + 31) % 31).Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                    db.Questions.Add(new QuestionRow { Context = Contexts[context].Id, Text = prompt, Topic = topic, CorrectAnswer = answer.ToString(System.Globalization.CultureInfo.InvariantCulture), OptionsJson = JsonSerializer.Serialize(options), Difficulty = bounds[context].Item1 + (i - 1) % (bounds[context].Item2 - bounds[context].Item1 + 1) });
                }
        }
        if (!await db.Courses.AnyAsync(ct))
        {
            var slots = JsonSerializer.Serialize(new[] { "Вівторок і четвер · 18:00", "Понеділок і середа · 19:00", "Субота · 11:00" });
            db.Courses.AddRange(new CourseRow { Title = "Впевнена база", Description = "Числа, дроби та відсотки: розуміння замість заучування.", Context = "elementary", PricePerLesson = 250, SlotsJson = slots }, new CourseRow { Title = "Алгебра без прогалин", Description = "Рівняння, функції та впевненість у власних знаннях.", Context = "intermediate", PricePerLesson = 300, SlotsJson = slots }, new CourseRow { Title = "Підготовка до НМТ", Description = "Практика й системний рух до вашого результату.", Context = "proficient", PricePerLesson = 350, SlotsJson = slots });
        }
        // Only pending seed profiles are updated; edited biographies are preserved.
        foreach (var profile in new[] {
            new TeacherRow { Id = Guid.Parse("813b10fe-8415-4cdf-bf56-94340d06b101"), DisplayName = "Друченко Олексій Сергійович", Subject = "Математика", Bio = "Студент 3-го курсу ФІОТ КПІ, спеціальність 126 — Інформаційні системи та технології.", PhotoPath = "/teachers/oleksii.jpg", IsPlaceholder = false },
            new TeacherRow { Id = Guid.Parse("813b10fe-8415-4cdf-bf56-94340d06b102"), DisplayName = "Гриценко Марина Миколаївна", Subject = "Математика", Bio = "Студентка 2-го курсу ФІОТ КПІ, спеціальність 121 — Інженерія програмного забезпечення. Результат НМТ — 200 балів. Пів року досвіду навчання дітей.", PhotoPath = "/teachers/maryna.jpg", IsPlaceholder = false } }) {
            var existing = await db.Teachers.SingleOrDefaultAsync(t => t.Id == profile.Id, ct);
            if (existing is null) db.Teachers.Add(profile);
            else if (existing.IsPlaceholder || existing.PhotoPath == "") {
                existing.DisplayName = profile.DisplayName; existing.Subject = profile.Subject; existing.Bio = profile.Bio;
                existing.PhotoPath = profile.PhotoPath; existing.IsPlaceholder = false;
            }
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
