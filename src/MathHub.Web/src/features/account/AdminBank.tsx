import { useEffect, useState, type FormEvent } from "react";
import { api } from "../../lib/api";
import type { Context, Question } from "../../lib/types";
export function AdminBank() {
  const [questions, setQuestions] = useState<Question[]>([]);
  const [contexts, setContexts] = useState<Context[]>([]);
  const [filter, setFilter] = useState("starter");
  const [editing, setEditing] = useState<Question | null>(null);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  async function load() {
    const [q, c] = await Promise.all([
      api<Question[]>("/admin/questions"),
      api<Context[]>("/contexts"),
    ]);
    setQuestions(q);
    setContexts(c);
  }
  useEffect(() => {
    load().catch((e) => setError(e.message));
  }, []);
  async function save(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = e.currentTarget;
    const d = new FormData(form);
    setBusy(true);
    setError("");
    try {
      await api(
        "/admin/questions" + (editing ? "/" + editing.id : ""),
        editing ? "PUT" : "POST",
        {
          context: d.get("context"),
          text: d.get("text"),
          topic: d.get("topic"),
          options: [0, 1, 2, 3].map((i) => d.get("option" + i)),
          correctAnswer: d.get("correctAnswer"),
          difficulty: Number(d.get("difficulty")),
          active: d.get("active") === "on",
        },
      );
      setNotice(
        "Завдання збережено. Опубліковані попередні версії залишаються у своїх тестах.",
      );
      setEditing(null);
      form.reset();
      await load();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <main className="portal dashboard">
      <span className="eyebrow">АДМІНІСТРУВАННЯ</span>
      <h1>Банк завдань</h1>
      <p>
        Початковий навчальний банк: {questions.filter((q) => q.active).length}{" "}
        активних завдань. Складність потребує подальшого калібрування на
        відповідях учнів.
      </p>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
      {notice && (
        <p className="notice" role="status">
          {notice}
        </p>
      )}
      <div className="dashboard-grid">
        <section className="panel">
          <h2>{editing ? "Нова версія завдання" : "Додати завдання"}</h2>
          <form key={editing?.id ?? "new"} onSubmit={save}>
            <label>
              Контекст
              <select name="context" defaultValue={editing?.context ?? filter}>
                {contexts.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Тема
              <input name="topic" required defaultValue={editing?.topic} />
            </label>
            <label>
              Текст
              <textarea name="text" required defaultValue={editing?.text} />
            </label>
            {[0, 1, 2, 3].map((i) => (
              <label key={i}>
                Варіант {i + 1}
                <input
                  name={"option" + i}
                  required
                  defaultValue={
                    editing ? JSON.parse(editing.optionsJson)[i] : ""
                  }
                />
              </label>
            ))}
            <label>
              Правильна відповідь (точно як варіант)
              <input
                name="correctAnswer"
                required
                defaultValue={editing?.correctAnswer}
              />
            </label>
            <label>
              Складність 0–100
              <input
                type="number"
                min="0"
                max="100"
                step="0.1"
                name="difficulty"
                required
                defaultValue={editing?.difficulty ?? 7}
              />
            </label>
            <label className="checkbox">
              <input
                type="checkbox"
                name="active"
                defaultChecked={editing?.active ?? true}
              />
              Активне завдання
            </label>
            <button className="button primary" disabled={busy}>
              Зберегти
            </button>
            {editing && (
              <button
                type="button"
                className="button secondary"
                onClick={() => setEditing(null)}
              >
                Скасувати
              </button>
            )}
          </form>
        </section>
        <section className="panel">
          <label>
            Переглянути контекст
            <select value={filter} onChange={(e) => setFilter(e.target.value)}>
              {contexts.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </label>
          <div className="bank-list">
            {questions
              .filter((q) => q.context === filter)
              .map((q) => (
                <article key={q.id}>
                  <small>
                    {q.topic} · складність {q.difficulty.toFixed(1)} · v
                    {q.version} · {q.active ? "Активне" : "Неактивне"}
                  </small>
                  <h3>{q.text}</h3>
                  <p>Правильна відповідь: {q.correctAnswer}</p>
                  <button
                    className="button secondary"
                    onClick={() => {
                      setEditing(q);
                      window.scrollTo({ top: 0, behavior: "smooth" });
                    }}
                  >
                    Редагувати →
                  </button>
                </article>
              ))}
          </div>
        </section>
      </div>
    </main>
  );
}
