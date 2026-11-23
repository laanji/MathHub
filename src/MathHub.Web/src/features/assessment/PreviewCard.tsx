import { useEffect, useRef, useState } from "react";
import { previewScore, type ScorePreview } from "./api";
export function PreviewCard() {
  const [selected, setSelected] = useState<number | null>(null);
  const [result, setResult] = useState<ScorePreview | null>(null);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const controller = useRef<AbortController | null>(null);
  useEffect(() => () => controller.current?.abort(), []);
  async function answer(value: number) {
    if (pending || result) return;
    setSelected(value);
    setPending(true);
    setError("");
    controller.current?.abort();
    controller.current = new AbortController();
    try {
      setResult(await previewScore(value === 12, controller.current.signal));
    } catch (e) {
      if (e instanceof Error && e.name !== "AbortError") setError(e.message);
    } finally {
      setPending(false);
    }
  }
  return (
    <section className="test-card" id="preview" aria-labelledby="preview-title">
      <div className="card-top">
        <span className="eyebrow">СПРОБУЙТЕ ЗАРАЗ</span>
        <span className="tiny-pill">Демо · 1 завдання</span>
      </div>
      <div className="test-symbol" aria-hidden="true">
        x²
      </div>
      <h2 id="preview-title">
        У математики є<br />
        ваш рівень.
      </h2>
      <p className="test-description">
        Одна задача — перший крок.
        <br />
        Повний тест визначатиме ваш MathScore.
      </p>
      <div className="question">
        <span>Знайдіть значення x</span>
        <strong>3x + 6 = 42</strong>
      </div>
      <div className="answers" aria-label="Варіанти відповіді">
        {[8, 10, 12, 14].map((v) => (
          <button
            key={v}
            disabled={pending || !!result}
            className={selected === v ? "selected" : ""}
            onClick={() => void answer(v)}
            aria-pressed={selected === v}
          >
            {v}
            <span>{selected === v ? "✓" : "↗"}</span>
          </button>
        ))}
      </div>
      <div className="preview-feedback" aria-live="polite">
        {pending ? (
          "Обчислюємо…"
        ) : error ? (
          <>
            <p role="alert">{error}</p>
            <button
              className="text-button"
              onClick={() => selected !== null && void answer(selected)}
            >
              Повторити
            </button>
          </>
        ) : result ? (
          <>
            <strong>
              {selected === 12 ? "Саме так!" : "Правильна відповідь — 12."}
            </strong>
            <p>
              Приклад зміни оцінки: 60 → <b>{result.mathScore}</b> ·{" "}
              {result.rank.name} {result.rank.subRank}
            </p>
            <button
              className="text-button"
              onClick={() => {
                setResult(null);
                setSelected(null);
              }}
            >
              Спробувати ще раз ↗
            </button>
          </>
        ) : (
          <p>Оберіть відповідь, щоб побачити приклад розрахунку.</p>
        )}
      </div>
      <div className="test-footer">
        <span className="dot" /> Без реєстрації · результат не зберігається
      </div>
    </section>
  );
}
