using MathHub.SharedKernel.Domain;

namespace MathHub.Modules.Groups.Domain;

public sealed class Course
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; }
    public decimal PricePerLesson { get; private set; }
    public Guid TeacherId { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Course(string title, decimal pricePerLesson, Guid teacherId)
    {
        if (string.IsNullOrWhiteSpace(title) || pricePerLesson <= 0 || teacherId == Guid.Empty)
            throw new BusinessRuleException("course.invalid", "Потрібні назва, викладач і додатна вартість заняття.");

        Title = title.Trim();
        PricePerLesson = pricePerLesson;
        TeacherId = teacherId;
    }

    public void Archive() => IsActive = false;
}
