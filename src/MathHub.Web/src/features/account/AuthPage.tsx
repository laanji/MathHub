import { useState, type FormEvent } from "react";
import { api, login } from "../../lib/api";
import type { Account } from "../../lib/types";
export function AuthPage({
  register,
  onSignedIn,
}: {
  register: boolean;
  onSignedIn: (a: Account) => void;
}) {
  const [role, setRole] = useState("Student");
  const [birth, setBirth] = useState("");
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const under16 =
    !!birth &&
    new Date(birth) >
      new Date(new Date().setFullYear(new Date().getFullYear() - 16));
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setBusy(true);
    setError("");
    setNotice("");
    try {
      const email = String(data.get("email"));
      const password = String(data.get("password"));
      if (register) {
        const result = await api<{ message: string; requiresParent: boolean }>(
          "/auth/register",
          "POST",
          {
            email,
            password,
            displayName: data.get("displayName"),
            birthDate: data.get("birthDate"),
            role,
            parentEmail: data.get("parentEmail") || null,
            consent: data.get("consent") === "on",
          },
        );
        if (result.requiresParent) {
          setNotice(result.message);
          return;
        }
      }
      await login(email, password);
      onSignedIn(await api<Account>("/auth/me"));
      location.hash = "/account";
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <main className="portal auth-layout">
      <aside className="auth-intro">
        <span className="eyebrow">ВАШ НАСТУПНИЙ КРОК</span>
        <h1>
          Математика починається
          <br />з вас<span className="pink-period">.</span>
        </h1>
        <p>
          Збережіть свій MathScore, знайдіть групу близького рівня та рухайтеся
          у своєму темпі.
        </p>
        <div className="auth-steps">
          <p>
            <b>01</b> Створіть акаунт
          </p>
          <p>
            <b>02</b> Визначте свій рівень
          </p>
          <p>
            <b>03</b> Оберіть курс і розклад
          </p>
        </div>
        <a href="#">← На головну</a>
      </aside>
      <section className="panel auth-form">
        <span className="eyebrow">MATHHUB · ОСОБИСТИЙ КАБІНЕТ</span>
        <h2>{register ? "Створити акаунт" : "Раді бачити вас"}</h2>
        <p>
          {register
            ? "Кілька деталей — і можна починати."
            : "Увійдіть, щоб продовжити навчальний шлях."}
        </p>
        <div className="tabs">
          <a className={!register ? "selected" : ""} href="#/login">
            Вхід
          </a>
          <a className={register ? "selected" : ""} href="#/register">
            Реєстрація
          </a>
        </div>
        {notice ? (
          <div className="notice" role="status">
            <h3>Потрібне підтвердження батьків</h3>
            <p>{notice}</p>
            <p>
              Для локальної демонстрації лист доступний у кабінеті батьків за
              кнопкою «Тестова пошта».
            </p>
            <a
              className="button primary"
              href="#/register"
              onClick={() => {
                setNotice("");
                setRole("Parent");
                setBirth("");
              }}
            >
              Створити акаунт батьків →
            </a>
          </div>
        ) : (
          <form onSubmit={submit}>
            {register && (
              <>
                <label>
                  Я реєструюся як
                  <select
                    value={role}
                    onChange={(e) => setRole(e.target.value)}
                  >
                    <option value="Student">Учень / учениця</option>
                    <option value="Parent">Хтось із батьків / опікун</option>
                  </select>
                </label>
                <label>
                  Ім’я та прізвище
                  <input
                    name="displayName"
                    autoComplete="name"
                    required
                    maxLength={100}
                  />
                </label>
                <label>
                  Дата народження
                  <input
                    type="date"
                    name="birthDate"
                    value={birth}
                    onChange={(e) => setBirth(e.target.value)}
                    required
                    max={new Date().toISOString().slice(0, 10)}
                  />
                </label>
              </>
            )}
            <label>
              Email
              <input
                type="email"
                name="email"
                autoComplete="email"
                required
                maxLength={254}
              />
            </label>
            <label>
              Пароль
              <input
                type="password"
                name="password"
                autoComplete={register ? "new-password" : "current-password"}
                required
                minLength={register ? 10 : undefined}
                maxLength={72}
              />
              {register && (
                <small>
                  Щонайменше 10 символів. Пароль не надсилається в листах.
                </small>
              )}
            </label>
            {register && role === "Student" && under16 && (
              <label>
                Email батьків / опікуна
                <input type="email" name="parentEmail" required />
                <small>
                  До 16 років акаунт активується після підтвердження батьками.
                </small>
              </label>
            )}
            {register && (
              <label className="checkbox">
                <input type="checkbox" name="consent" required />
                Погоджуюся на обробку наданих даних для роботи навчального
                акаунта.
              </label>
            )}
            {error && (
              <p className="error" role="alert">
                {error}
              </p>
            )}
            <button className="button primary full" disabled={busy}>
              {busy
                ? "Зачекайте…"
                : register
                  ? "Створити акаунт →"
                  : "Увійти →"}
            </button>
          </form>
        )}
      </section>
    </main>
  );
}
