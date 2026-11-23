namespace MathHub.Modules.Assessment.Domain;
public interface IScoringEngine
{
    double Update(double theta, double difficulty, bool correct, int answerNumber);
}
