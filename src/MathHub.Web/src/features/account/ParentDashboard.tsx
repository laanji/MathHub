import { useEffect, useRef, useState } from "react";
import { api } from "../../lib/api";
import type { Account, Child, Enrollment } from "../../lib/types";
import { EnrollmentCards } from "./EnrollmentCards";
export function ParentDashboard({ account }: { account: Account }) {
  const [children, setChildren] = useState<Child[]>([]);
  const [items, setItems] = useState<Record<string, Enrollment[]>>({});
  const [codes, setCodes] = useState<Record<string, string>>({});
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const [mailboxEnabled, setMailboxEnabled] = useState(false);
  const requests = useRef<Record<string, string>>({});
  async function load() {
    const list = await api<Child[]>("/parent/children");
    setChildren(list);
    const pairs = await Promise.all(
      list
        .filter((c) => c.confirmed)
        .map(
          async (c) =>
            [
              c.id,
              await api<Enrollment[]>(
                "/parent/children/" + c.id + "/enrollments",
              ),
            ] as const,
        ),
    );
    setItems(Object.fromEntries(pairs));
  }
  useEffect(() => {
    load().catch((e) => setError(e.message));
    api<{ developmentMailbox: boolean }>("/config")
      .then((c) => setMailboxEnabled(c.developmentMailbox))
      .catch(() => {});
  }, []);
  async function run(action: () => Promise<void>) {
    setBusy(true);
    setError("");
    try {
      await action();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function confirm(c: Child) {
    await run(async () => {
      await api("/parent/children/" + c.id + "/confirm", "POST", {
        code: codes[c.id] ?? "",
      });
      setNotice(
        "Акаунт дитини активовано. Тепер вона може увійти та пройти діагностику.",
      );
      await load();
    });
  }
  async function mail(c: Child) {
    await run(async () => {
      const message = await api<{ code: string; recipient: string }>(
        "/dev/mail/" + c.id,
      );
      setCodes({ ...codes, [c.id]: message.code });
      setNotice(
        "Код отримано з локальної тестової пошти для " +
          message.recipient +
          ". Лист не надсилався назовні.",
      );
    });
  }
  async function pay(c: Child, e: Enrollment) {
    await run(async () => {
      const requestId = (requests.current[e.id] ??= crypto.randomUUID());
      const r = await api<{ message: string }>(
        "/parent/children/" + c.id + "/enrollments/" + e.id + "/payment",
        "POST",
        { requestId },
      );
      setNotice(r.message);
      await load();
    });
  }
  return (
    <main className="portal dashboard">
      <div className="dashboard-heading">
        <div>
          <span className="eyebrow">КАБІНЕТ БАТЬКІВ</span>
          <h1>Вітаємо, {account.displayName.split(" ")[0]}.</h1>
          <p>
            Підтверджуйте акаунти дітей і переглядайте їхні навчальні записи.
          </p>
        </div>
        <button
          className="button secondary"
          onClick={() => run(load)}
          disabled={busy}
        >
          Оновити ↻
        </button>
      </div>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}
      {notice && (
        <p role="status" className="notice">
          {notice}
        </p>
      )}
      {children.length === 0 && (
        <div className="panel empty">
          <h2>Поки немає пов’язаних акаунтів</h2>
          <p>
            Під час реєстрації дитини до 16 років вкажіть саме ваш email:{" "}
            <strong>{account.email}</strong>.
          </p>
          <a href="#/register" className="button secondary">
            Форма реєстрації →
          </a>
        </div>
      )}
      {children.map((c) => (
        <section className="panel portal-section" key={c.id}>
          <div className="section-heading">
            <h2>{c.displayName}</h2>
            <span className={"status " + (c.confirmed ? "success" : "")}>
              {c.confirmed
                ? "Акаунт підтверджено"
                : "Очікує вашого підтвердження"}
            </span>
          </div>
          {!c.confirmed ? (
            <div className="parent-confirm">
              <p>
                Введіть код з листа та підтвердьте згоду на активацію
                навчального акаунта дитини. Код дійсний 24 години; доступні 5
                спроб.
              </p>
              <label>
                Код підтвердження
                <input
                  value={codes[c.id] ?? ""}
                  onChange={(e) =>
                    setCodes({ ...codes, [c.id]: e.target.value })
                  }
                  inputMode="numeric"
                  maxLength={6}
                  autoComplete="one-time-code"
                />
              </label>
              <button
                className="button primary"
                disabled={busy || (codes[c.id] ?? "").length !== 6}
                onClick={() => confirm(c)}
              >
                Підтвердити зв’язок і згоду
              </button>
              {mailboxEnabled && (
                <button
                  className="button secondary"
                  disabled={busy}
                  onClick={() => mail(c)}
                >
                  Тестова пошта ↗
                </button>
              )}
            </div>
          ) : (
            <>
              <p className="child-score">
                MathScore <strong>{c.mathScore ?? "ще не визначено"}</strong>
              </p>
              <EnrollmentCards
                items={items[c.id] ?? []}
                busy={busy}
                pay={(e) => pay(c, e)}
              />
            </>
          )}
        </section>
      ))}
    </main>
  );
}
