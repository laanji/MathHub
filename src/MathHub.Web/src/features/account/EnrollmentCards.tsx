import type { Enrollment } from "../../lib/types";
const status: Record<string, string> = {
  Reserved: "Місце зарезервовано",
  Active: "Запис підтверджено",
  Expired: "Резерв завершився",
  Left: "Резерв скасовано",
  Cancelled: "Резерв скасовано",
};
const state: Record<string, string> = {
  Forming: "Група формується",
  Open: "Можна починати навчання",
  ClosedFull: "Група укомплектована",
  Archived: "Група архівна",
};
export function EnrollmentCards({
  items,
  busy,
  pay,
  cancel,
  requiresParent = false,
}: {
  items: Enrollment[];
  busy: boolean;
  pay: (e: Enrollment) => void;
  cancel?: (e: Enrollment) => void;
  requiresParent?: boolean;
}) {
  return (
    <div className="enrollment-list">
      {items.length === 0 && (
        <div className="empty">
          <span>↗</span>
          <h3>Тут будуть ваші записи</h3>
          <p>Оберіть курс після завершення діагностики.</p>
        </div>
      )}
      {items.map((e) => (
        <article className="enrollment panel" key={e.id}>
          <div>
            <span
              className={"status " + (e.status === "Active" ? "success" : "")}
            >
              {status[e.status] ?? e.status}
            </span>
            <h3>{e.courseTitle}</h3>
            <p>{e.slot}</p>
            <p className="muted">
              {state[e.groupState]} · підтверджено {e.activeCount}/5 · зайнято з
              резервами {e.occupiedSeats}/5
            </p>
            <p className="muted">
              MathScore при записі: {e.scoreAtEntry} · {e.priceSnapshot} грн /
              заняття
            </p>
            {e.status === "Reserved" && (
              <small>
                Резерв до{" "}
                {new Date(e.reservedUntil).toLocaleTimeString("uk-UA", {
                  hour: "2-digit",
                  minute: "2-digit",
                })}
                . Після завершення строку оновіть записи.
              </small>
            )}
          </div>
          {e.status === "Reserved" && (
            <div className="enrollment-actions">
              {requiresParent ? (
                <p className="notice">
                  Оплату підтверджує хтось із батьків у своєму кабінеті.
                </p>
              ) : (
                <>
                  <small>Тестовий шлюз. Реальні кошти не списуються.</small>
                  <button
                    className="button primary"
                    disabled={busy}
                    onClick={() => pay(e)}
                  >
                    Тестова оплата · {e.priceSnapshot} грн
                  </button>
                </>
              )}
              {cancel && (
                <button
                  className="button secondary"
                  disabled={busy}
                  onClick={() => cancel(e)}
                >
                  Скасувати резерв
                </button>
              )}
            </div>
          )}
        </article>
      ))}
    </div>
  );
}
