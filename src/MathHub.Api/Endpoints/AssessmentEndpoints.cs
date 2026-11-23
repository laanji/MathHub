using MathHub.Modules.Assessment.Application;
namespace MathHub.Api.Endpoints;
public sealed record PreviewRequest(double Theta, double Difficulty, bool Correct);
public static class AssessmentEndpoints
{
    public static void MapAssessmentPreview(this IEndpointRouteBuilder endpoints)
    {
        // Non-personal demonstration only. Official sessions require Identity in Lab 5.
        endpoints.MapPost("/api/assessment/preview", (PreviewRequest request, ScorePreviewService service) =>
            Results.Ok(service.Calculate(request.Theta, request.Difficulty, request.Correct)))
            .WithName("PreviewMathScore");
    }
}
