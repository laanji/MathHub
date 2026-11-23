export type ScorePreview = {
  estimate: number;
  mathScore: number;
  rank: { name: string; subRank: string };
};
export async function previewScore(
  correct: boolean,
  signal?: AbortSignal,
): Promise<ScorePreview> {
  const response = await fetch("/api/assessment/preview", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ theta: 60, difficulty: 60, correct }),
    signal,
  });
  if (!response.ok)
    throw new Error("Не вдалося отримати результат. Спробуйте ще раз.");
  return response.json() as Promise<ScorePreview>;
}
