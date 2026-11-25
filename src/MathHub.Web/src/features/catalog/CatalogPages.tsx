import { useEffect, useRef, useState } from "react";
import { api } from "../../lib/api";
import type { Account, Course, Enrollment } from "../../lib/types";
import { EnrollmentCards } from "../account/EnrollmentCards";
type Teacher = {
  id: string;
  displayName: string;
  subject: string;
  bio: string;
  photoPath: string;
  isPlaceholder: boolean;
};
type Payment = {
  id: string;
  enrollmentId?: string;
  courseTitle?: string;
  amount: number;
  status: string;
  gatewayReference: string;
  createdAt: string;
};
function useList<T>(path: string) {
  const [items, setItems] = useState<T[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  async function load() {
    setError("");
    try {
      setItems(await api<T[]>(path));
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }
  useEffect(() => {
    void load();
  }, [path]);
  return { items, error, loading, load };
}
export function CoursesPage() {
  const { items, error, loading, load } = useList<Course>("/courses");
  return (
    <main className="portal">
      <span className="eyebrow">КАТАЛОГ MATHHUB</span>
      <h1>Курси математики</h1>
      <p>
        Три навчальні приклади для перевірки MVP. Розклад і ціни демонстраційні;
        заняття з викладачем ще не проводяться через платформу.
      </p>
      {error && (
        <p className="error" role="alert">
          {error} <button onClick={load}>Спробувати знову</button>
        </p>
      )}
      {loading && <p role="status">Завантажуємо курси…</p>}
      <div className="catalog-grid">
        {items.map((c, i) => (
          <article className={"panel catalog-card catalog-" + i} key={c.id}>
            <span className="status">Демонстраційний курс</span>
            <h2>{c.title}</h2>
            <p>{c.description}</p>
            <strong className="course-price">
              {c.pricePerLesson} грн <small>/ заняття</small>
            </strong>
            <p>{c.slots.join(" · ")}</p>
            <div className="page-actions">
              <a className="button primary" href="#/groups">
                Підібрати групу
              </a>
              <a
                className="button secondary"
                href={"#/payments?course=" + c.id}
              >
                Перевірити оплату
              </a>
            </div>
          </article>
        ))}
      </div>
    </main>
  );
}
export function TeachersPage() {
  const { items, error, loading, load } = useList<Teacher>("/teachers");
  return (
    <main className="portal">
      <span className="eyebrow">КОМАНДА MATHHUB</span>
      <h1>Наші викладачі</h1>
      <p>Знайомтеся з викладачами математики MathHub.</p>
      {loading && <p role="status">Завантажуємо профілі…</p>}
      {error && (
        <p className="error" role="alert">
          {error} <button onClick={load}>Спробувати знову</button>
        </p>
      )}
      <div className="teacher-grid">
        {items.map((t, i) => (
          <article className="panel" key={t.id}>
            <img
              className={"teacher-photo teacher-photo-" + i}
              src={t.photoPath}
              alt={t.displayName}
              loading="lazy"
            />
            {t.isPlaceholder && (
              <span className="status">Дані очікують уточнення</span>
            )}
            <h2>{t.displayName}</h2>
            <strong>{t.subject}</strong>
            <p>{t.bio}</p>
          </article>
        ))}
      </div>
    </main>
  );
}
export function SignInPrompt({ title }: { title: string }) {
  return (
    <main className="portal">
      <h1>{title}</h1>
      <section className="panel">
        <h2>Увійдіть у свій акаунт</h2>
        <p>
          Групи, записи й історія оплат прив’язані до користувача. Для резерву
          потрібен MathScore; демонстрацію оплати можна пройти окремо.
        </p>
        <div className="page-actions">
          <a className="button primary" href="#/login">
            Увійти
          </a>
          <a className="button secondary" href="#/register">
            Реєстрація
          </a>
        </div>
      </section>
    </main>
  );
}
export function PaymentsPage({ account }: { account: Account }) {
  const [courses, setCourses] = useState<Course[]>([]);
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [demo, setDemo] = useState<Payment[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [course, setCourse] = useState(
    new URLSearchParams(location.hash.split("?")[1]).get("course") ?? "",
  );
  const [scenario, setScenario] = useState("success");
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const request = useRef<{ payload: string; id: string } | null>(null);
  async function load() {
    const [c, d, p, e] = await Promise.all([
      api<Course[]>("/courses"),
      api<Payment[]>("/billing/demo"),
      api<Payment[]>("/billing/payments"),
      account.role === "Student"
        ? api<Enrollment[]>("/student/enrollments")
        : Promise.resolve([]),
    ]);
    setCourses(c);
    setDemo(d);
    setPayments(p);
    setEnrollments(e);
    setCourse((v) => (c.some((x) => x.id === v) ? v : (c[0]?.id ?? "")));
  }
  useEffect(() => {
    load()
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [account.id]);
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
  async function checkout() {
    await run(async () => {
      const payload = course + scenario;
      if (request.current?.payload !== payload)
        request.current = { payload, id: crypto.randomUUID() };
      const result = await api<Payment>("/billing/demo", "POST", {
        courseId: course,
        scenario,
        requestId: request.current.id,
      });
      request.current = null;
      setNotice(
        result.status === "Succeeded"
          ? "Демонстраційну оплату підтверджено. Запис у групу не створено."
          : "Тестовий шлюз відхилив оплату. Кошти не списані, запис у групу не створено.",
      );
      await load();
    });
  }
  const under16 =
    account.role === "Student" &&
    new Date(account.birthDate) >
      new Date(new Date().setFullYear(new Date().getFullYear() - 16));
  return (
    <main className="portal">
      <span className="eyebrow">ТЕСТОВИЙ ПЛАТІЖНИЙ ШЛЮЗ</span>
      <h1>Оплата та квитанції</h1>
      <p>
        Усі операції тут тестові. Банківські реквізити не потрібні, реальні
        кошти не списуються.
      </p>
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}
      {notice && (
        <p className="notice" role="status">
          {notice}
        </p>
      )}
      <button
        className="button secondary"
        disabled={busy}
        onClick={() => run(load)}
      >
        Оновити дані
      </button>
      {loading ? (
        <p role="status">Завантажуємо оплати…</p>
      ) : (
        <>
          <section className="panel portal-section">
            <h2>Перевірка оплати без запису в групу</h2>
            <p>
              Окремий демонстраційний платіж за вибраний курс. Він зберігається
              в БД та історії нижче, але не займає місце й не підтверджує
              навчання. Діагностика для цього не потрібна.
            </p>
            {under16 ? (
              <p className="notice">
                До 16 років демонстрацію оплати проводить хтось із батьків у
                власному акаунті.
              </p>
            ) : (
              <>
                <label>
                  Демонстраційний курс
                  <select
                    value={course}
                    onChange={(e) => setCourse(e.target.value)}
                    disabled={busy}
                  >
                    {courses.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.title} · {c.pricePerLesson} грн
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Відповідь тестового шлюзу
                  <select
                    value={scenario}
                    onChange={(e) => setScenario(e.target.value)}
                    disabled={busy}
                  >
                    <option value="success">Успішна оплата</option>
                    <option value="declined">Відхилена оплата</option>
                  </select>
                </label>
                <button
                  className="button primary"
                  disabled={busy || !course}
                  onClick={checkout}
                >
                  {busy ? "Обробляємо…" : "Виконати тестову оплату"}
                </button>
              </>
            )}
          </section>
          <section className="portal-section">
            <h2>Оплата резерву групи</h2>
            <p>
              Ця дія підтверджує вже створений резерв. Резерв діє 15 хвилин.
            </p>
            {account.role === "Student" ? (
              <>
                <EnrollmentCards
                  items={enrollments.filter((e) => e.status === "Reserved")}
                  busy={busy}
                  requiresParent={under16}
                  pay={(e) =>
                    run(async () => {
                      const result = await api<{
                        confirmed: boolean;
                        message: string;
                      }>("/student/enrollments/" + e.id + "/payment", "POST", {
                        requestId: crypto.randomUUID(),
                      });
                      setNotice(result.message);
                      await load();
                    })
                  }
                />
                <a className="button secondary" href="#/groups">
                  Групи та мої записи
                </a>
              </>
            ) : (
              <p>
                Підтвердити резерв дитини можна в{" "}
                <a href="#/account">батьківському кабінеті</a>.
              </p>
            )}
          </section>
          <section className="portal-section">
            <h2>Історія оплат резервів</h2>
            <PaymentList items={payments} />
          </section>
          <section className="portal-section">
            <h2>Демонстраційні квитанції без групи</h2>
            <PaymentList items={demo} />
          </section>
        </>
      )}
    </main>
  );
}
function PaymentList({ items }: { items: Payment[] }) {
  return (
    <div className="enrollment-list">
      {!items.length && <p>Операцій поки немає.</p>}
      {items.map((p) => (
        <article className="panel payment-receipt" key={p.id}>
          <span
            className={"status " + (p.status === "Succeeded" ? "success" : "")}
          >
            {p.status === "Succeeded"
              ? "Успішно"
              : p.status === "Declined"
                ? "Відхилено"
                : p.status}
          </span>
          <h3>
            {p.courseTitle ?? "Підтвердження резерву"} · {p.amount} грн
          </h3>
          <p>{new Date(p.createdAt).toLocaleString("uk-UA")}</p>
          <p>Квитанція: {p.id}</p>
          <p>Шлюз: {p.gatewayReference}</p>
          {p.enrollmentId && <p>Запис: {p.enrollmentId}</p>}
        </article>
      ))}
    </div>
  );
}
