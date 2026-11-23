import { useEffect, useRef, useState } from "react";
import { api } from "../../lib/api";
import type {
  Account,
  Context,
  Course,
  Enrollment,
  Test,
} from "../../lib/types";
import { EnrollmentCards } from "./EnrollmentCards";
export function StudentDashboard({
  account,
  onAccount,
}: {
  account: Account;
  onAccount: (a: Account) => void;
}) {
  const [test, setTest] = useState<Test | null>(null);
  const [contexts, setContexts] = useState<Context[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [context, setContext] = useState("intermediate");
  const [answer, setAnswer] = useState("");
  const [slots, setSlots] = useState<Record<string, string>>({});
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const pendingAnswer = useRef<{
    questionId: string;
    answer: string;
    version: number;
    requestId: string;
  } | null>(null);
  const requests = useRef<Record<string, string>>({});
  async function load() {
    const [t, c, k, e, a] = await Promise.all([
      api<Test | null>("/student/tests/current"),
      api<Context[]>("/contexts"),
      api<Course[]>("/courses"),
      api<Enrollment[]>("/student/enrollments"),
      api<Account>("/auth/me"),
    ]);
    setTest(t);
    setContexts(c);
    setCourses(k);
    setEnrollments(e);
    onAccount(a);
  }
  useEffect(() => {
    let live = true;
    load()
      .catch((e) => {
        if (live) setError(e.message);
      })
      .finally(() => {
        if (live) setLoading(false);
      });
    return () => {
      live = false;
    };
  }, []);
  async function run(action: () => Promise<void>) {
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await action();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  function request(key: string) {
    return (requests.current[key] ??= crypto.randomUUID());
  }
  async function start() {
    await run(async () => {
      setTest(await api<Test>("/student/tests", "POST", { context }));
      setAnswer("");
    });
  }
  async function submit() {
    if (!test?.question || !answer) return;
    await run(async () => {
      const q = test.question!;
      if (
        pendingAnswer.current?.questionId !== q.id ||
        pendingAnswer.current.answer !== answer
      )
        pendingAnswer.current = {
          questionId: q.id,
          answer,
          version: test.version,
          requestId: crypto.randomUUID(),
        };
      const next = await api<Test>(
        "/student/tests/" + test.id + "/answers",
        "POST",
        pendingAnswer.current,
      );
      setTest(next);
      setAnswer("");
      pendingAnswer.current = null;
      if (next.status === "Completed")
        onAccount(await api<Account>("/auth/me"));
    });
  }
  async function reserve(c: Course) {
    await run(async () => {
      const slot = slots[c.id] ?? c.slots[0];
      const key = "reserve:" + c.id + ":" + slot;
      await api("/student/enrollments", "POST", {
        courseId: c.id,
        slot,
        requestId: request(key),
      });
      delete requests.current[key];
      setEnrollments(await api<Enrollment[]>("/student/enrollments"));
      setNotice(
        "Місце зарезервовано на 15 хвилин. Підтвердьте запис тестовою оплатою.",
      );
      document
        .getElementById("my-enrollments")
        ?.scrollIntoView({ behavior: "smooth" });
    });
  }
  async function pay(e: Enrollment) {
    await run(async () => {
      const result = await api<{ message: string }>(
        "/student/enrollments/" + e.id + "/payment",
        "POST",
        { requestId: request("pay:" + e.id) },
      );
      setNotice(result.message);
      setEnrollments(await api<Enrollment[]>("/student/enrollments"));
    });
  }
  async function cancel(e: Enrollment) {
    await run(async () => {
      await api("/student/enrollments/" + e.id, "DELETE");
      setEnrollments(await api<Enrollment[]>("/student/enrollments"));
    });
  }
  const under16 =
    new Date(account.birthDate) >
    new Date(new Date().setFullYear(new Date().getFullYear() - 16));
  return (
    <main className="portal dashboard">
      <div className="dashboard-heading">
        <div>
          <span className="eyebrow">ОСОБИСТИЙ КАБІНЕТ</span>
          <h1>
            Привіт, {account.displayName.split(" ")[0]}
            <span className="pink-period">.</span>
          </h1>
          <p>Ваш рівень. Ваш розклад. Ваш наступний крок.</p>
        </div>
        <a className="button secondary" href="#">
          На головну ↗
        </a>
      </div>
      {error && (
        <div className="error" role="alert">
          {error} <button onClick={() => run(load)}>Оновити дані</button>
        </div>
      )}
      {notice && (
        <p className="notice" role="status">
          {notice}
        </p>
      )}
      {loading ? (
        <p role="status">Завантажуємо ваш кабінет…</p>
      ) : (
        <>
          <div className="dashboard-grid">
            <section className="panel score-panel">
              <span className="eyebrow">МІЙ MATHSCORE</span>
              <div className="score-number">
                {account.mathScore ?? "—"}
                <small>/100</small>
              </div>
              <span className="status success">
                {account.rank
                  ? account.rank + " " + account.subRank
                  : "Рівень ще не визначено"}
              </span>
              <p>
                {account.mathScore === null
                  ? "Пройдіть діагностику, щоб отримати свій стартовий бал і підібрати групу."
                  : "Результат збережено у вашому акаунті. Для груп використовуємо бал, зафіксований під час запису."}
              </p>
              <div className="score-track">
                <span style={{ width: (account.mathScore ?? 0) + "%" }} />
              </div>
              <small>
                Повторна діагностика — через 30 днів після завершення.
              </small>
            </section>
            <section className="panel test-panel" id="diagnostic">
              <span className="eyebrow">ДІАГНОСТИКА · 15–20 ЗАВДАНЬ</span>
              {test?.status === "InProgress" && test.question ? (
                <>
                  <div className="question-meta">
                    <span>
                      Завдання {test.version + 1} / до {test.maximumQuestions}
                    </span>
                    <span>{test.question.topic}</span>
                  </div>
                  <progress
                    value={test.version}
                    max={20}
                    aria-label="Прогрес діагностики"
                  />
                  <h2>{test.question.text}</h2>
                  <fieldset disabled={busy} className="answers">
                    <legend className="sr-only">Оберіть відповідь</legend>
                    {test.question.options.map((o, i) => (
                      <label className={answer === o ? "chosen" : ""} key={o}>
                        <input
                          type="radio"
                          name="answer"
                          value={o}
                          checked={answer === o}
                          onChange={() => setAnswer(o)}
                        />
                        <span className="answer-letter">
                          {String.fromCharCode(65 + i)}
                        </span>
                        {o}
                      </label>
                    ))}
                  </fieldset>
                  <button
                    className="button primary"
                    disabled={!answer || busy}
                    onClick={submit}
                  >
                    {busy ? "Зберігаємо…" : "Підтвердити відповідь →"}
                  </button>
                  <small>
                    Відповіді зберігаються після кожного кроку. Можна закрити
                    сторінку й продовжити пізніше.
                  </small>
                </>
              ) : test?.result ? (
                <>
                  <span className="status success">
                    Діагностику завершено ✓
                  </span>
                  <h2>Ваш старт — {test.result.mathScore} балів</h2>
                  <p>
                    {test.result.rank} {test.result.subRank} · правильних
                    відповідей {test.result.correct} із {test.result.total}.
                  </p>
                  <p>
                    MathScore враховує складність завдань, а не лише відсоток
                    правильних відповідей.
                  </p>
                  <button
                    className="button primary"
                    onClick={() =>
                      document
                        .getElementById("catalog")
                        ?.scrollIntoView({ behavior: "smooth" })
                    }
                  >
                    Обрати курс →
                  </button>
                  <small>
                    Завершено{" "}
                    {new Date(test.result.completedAt).toLocaleString("uk-UA")}.
                  </small>
                </>
              ) : (
                <>
                  <h2>
                    Знайдемо вашу
                    <br />
                    точку старту
                  </h2>
                  <p>
                    Оберіть контекст, який відповідає вашій навчальній цілі.
                    Складність завдань підлаштовуватиметься за відповідями.
                  </p>
                  {test?.status === "InsufficientData" && (
                    <p className="error">
                      Банк завдань вичерпано. Результат не зараховано; можна
                      почати нову діагностику.
                    </p>
                  )}
                  <label>
                    Навчальний контекст
                    <select
                      value={context}
                      onChange={(e) => setContext(e.target.value)}
                    >
                      {contexts.map((c) => (
                        <option value={c.id} key={c.id}>
                          {c.name}
                        </option>
                      ))}
                    </select>
                  </label>
                  <button
                    className="button primary"
                    onClick={start}
                    disabled={busy}
                  >
                    Почати діагностику ↗
                  </button>
                  <small>
                    Це діагностичний орієнтир для підбору груп, а не шкільна
                    оцінка.
                  </small>
                </>
              )}
            </section>
          </div>
          <section className="portal-section" id="my-enrollments">
            <div className="section-heading">
              <div>
                <span className="eyebrow">НАВЧАННЯ У СВОЄМУ ТЕМПІ</span>
                <h2>Мої записи</h2>
              </div>
              <button
                className="button secondary"
                disabled={busy}
                onClick={() =>
                  run(async () =>
                    setEnrollments(
                      await api<Enrollment[]>("/student/enrollments"),
                    ),
                  )
                }
              >
                Оновити ↻
              </button>
            </div>
            <EnrollmentCards
              items={enrollments}
              busy={busy}
              pay={pay}
              cancel={cancel}
              requiresParent={under16}
            />
          </section>
          <section className="portal-section" id="catalog">
            <span className="eyebrow">НАСТУПНИЙ КРОК</span>
            <h2>Оберіть свій курс</h2>
            <p>
              Підберемо мінігрупу за віком, розкладом і близьким MathScore.
              Група починає навчання від 3 підтверджених учнів, максимум — 5.
            </p>
            <div className="catalog-grid">
              {courses.map((c, i) => (
                <article
                  className={"panel catalog-card catalog-" + i}
                  key={c.id}
                >
                  <span className="eyebrow">НАПРЯМ 0{i + 1}</span>
                  <h3>{c.title}</h3>
                  <p>{c.description}</p>
                  <strong className="course-price">
                    {c.pricePerLesson} грн <small>/ заняття</small>
                  </strong>
                  <label>
                    Зручний розклад
                    <select
                      value={slots[c.id] ?? c.slots[0]}
                      onChange={(e) =>
                        setSlots({ ...slots, [c.id]: e.target.value })
                      }
                    >
                      {c.slots.map((s) => (
                        <option key={s}>{s}</option>
                      ))}
                    </select>
                  </label>
                  <button
                    className="button primary full"
                    disabled={
                      busy ||
                      account.mathScore === null ||
                      enrollments.some(
                        (e) =>
                          e.courseId === c.id &&
                          ["Active", "Reserved"].includes(e.status),
                      )
                    }
                    onClick={() => reserve(c)}
                  >
                    {account.mathScore === null
                      ? "Спочатку діагностика"
                      : "Зарезервувати місце →"}
                  </button>
                </article>
              ))}
            </div>
          </section>
        </>
      )}
    </main>
  );
}
