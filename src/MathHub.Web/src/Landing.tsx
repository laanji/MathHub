import { useState } from "react";
import { PreviewCard } from "./features/assessment/PreviewCard";
const courses = [
  {
    n: "01",
    tag: "ПОЧАТКОВА ШКОЛА",
    title: "Впевнена база",
    text: "Числа, логіка та перші відкриття. Крок за кроком — до розуміння.",
    mark: "÷",
    color: "mint",
  },
  {
    n: "02",
    tag: "5–9 КЛАСИ",
    title: "Математика без прогалин",
    text: "Алгебра й геометрія, які нарешті складаються в зрозумілу картину.",
    mark: "√",
    color: "rose",
  },
  {
    n: "03",
    tag: "10–11 КЛАСИ",
    title: "Підготовка до НМТ",
    text: "Системна підготовка, практика та увага до складних саме для вас тем.",
    mark: "π",
    color: "lavender",
  },
];
export function Landing() {
  const [menu, setMenu] = useState(false);
  return (
    <>
      <header className="header">
        <a className="brand" href="#" aria-label="MathHub — на головну">
          <img src="/brand/mathhub.png" alt="MathHub.ua" />
        </a>
        <button
          className="menu-toggle"
          onClick={() => setMenu(!menu)}
          aria-expanded={menu}
          aria-controls="main-nav"
        >
          Меню ☰
        </button>
        <nav
          id="main-nav"
          className={menu ? "open" : ""}
          aria-label="Головна навігація"
        >
          <a href="#/courses" onClick={() => setMenu(false)}>
            Курси
          </a>
          <a href="#/teachers" onClick={() => setMenu(false)}>
            Наші викладачі
          </a>
          <a href="#/groups" onClick={() => setMenu(false)}>
            Групи та записи
          </a>
          <a href="#/payments" onClick={() => setMenu(false)}>
            Оплата
          </a>
          <a href="#approach" onClick={() => setMenu(false)}>
            Як це працює
          </a>
          <a href="#score" onClick={() => setMenu(false)}>
            MathScore
          </a>
          <a href="#faq" onClick={() => setMenu(false)}>
            Питання
          </a>
        </nav>
        <a className="header-cta" href="#/account">
          Мій кабінет ↗
        </a>
      </header>
      <main>
        <section className="hero">
          <div className="hero-copy">
            <div className="hero-badge">
              <span className="dot" /> МАТЕМАТИКА ДЛЯ ШКОЛЯРІВ І СТУДЕНТІВ
            </div>
            <p className="small-note">Зрозуміти. Спробувати. Змогти.</p>
            <h1>
              Математика
              <br />у <span>своєму</span>
              <br />
              темпі<span className="pink-period">.</span>
            </h1>
            <p className="hero-description">
              Почніть зі свого рівня, а не з номера класу.
              <br />
              Знайдіть упевненість у математиці — разом
              <br className="desktop" /> із тими, хто рухається поруч.
            </p>
            <div className="hero-actions">
              <a className="button primary" href="#/account">
                Визначити свій рівень <span>↗</span>
              </a>
              <a className="button secondary" href="#/courses">
                Обрати напрям <span>→</span>
              </a>
            </div>
            <div className="hero-proof">
              <span className="proof-icon">✳</span>
              <p>
                Близький рівень знань.
                <br />
                <strong>Більше уваги кожному.</strong>
              </p>
            </div>
            <span className="hero-decoration" aria-hidden="true">
              +
            </span>
          </div>
          <PreviewCard />
        </section>
        <div className="value-strip">
          <p>
            <strong>0–100</strong>
            <span>шкала MathScore</span>
          </p>
          <span className="strip-star">✳</span>
          <p>
            <strong>3–5</strong>
            <span>учнів у мінігрупі</span>
          </p>
          <span className="strip-star">✳</span>
          <p>
            <strong>Свій темп</strong>
            <span>навчання без порівнянь</span>
          </p>
        </div>
        <section id="courses" className="section">
          <div className="section-heading">
            <div>
              <p className="eyebrow">ВАША МАТЕМАТИКА ПОЧИНАЄТЬСЯ ТУТ</p>
              <h2>
                Для кожної цілі —<br />
                свій напрям.
              </h2>
            </div>
            <p>
              Від першого «а я зрозумів!»
              <br />
              до впевненого результату.
            </p>
          </div>
          <div className="courses">
            {courses.map((c) => (
              <article key={c.n} className={"course " + c.color}>
                <div className="course-meta">
                  <span>{c.tag}</span>
                  <span>{c.n}</span>
                </div>
                <div className="course-mark" aria-hidden="true">
                  {c.mark}
                </div>
                <h3>{c.title}</h3>
                <p>{c.text}</p>
                <a href="#/account">
                  Визначити свій рівень <span>↗</span>
                </a>
              </article>
            ))}
          </div>
          <p className="availability">
            Напрями навчання платформи. Запис у групи з’явиться після запуску
            особистого кабінету.
          </p>
        </section>
        <section className="approach section" id="approach">
          <div>
            <p className="eyebrow">МЕНШЕ ВИПАДКОВОСТІ. БІЛЬШЕ РОЗУМІННЯ.</p>
            <h2>
              Ваш рівень.
              <br />
              Ваша група.
              <br />
              <span>Ваш прогрес.</span>
            </h2>
            <p>
              Навчання починається з розуміння того,
              <br />
              що ви вже знаєте і куди хочете прийти.
            </p>
          </div>
          <ol className="steps">
            <li>
              <span>01</span>
              <div>
                <h3>Визначаємо старт</h3>
                <p>
                  Адаптивний тест із 15–20 завдань формуватиме MathScore й
                  покаже теми для роботи.
                </p>
              </div>
            </li>
            <li>
              <span>02</span>
              <div>
                <h3>Знаходимо своє коло</h3>
                <p>
                  Мінігрупа з близькими результатами, з урахуванням віку та
                  зручного часу.
                </p>
              </div>
            </li>
            <li>
              <span>03</span>
              <div>
                <h3>Рухаємося крок за кроком</h3>
                <p>
                  Практика, пояснення та зворотний зв’язок допомагатимуть бачити
                  власний прогрес.
                </p>
              </div>
            </li>
          </ol>
        </section>
        <section className="score-section section" id="score">
          <div>
            <p className="eyebrow">ЗРОЗУМІЛИЙ ОРІЄНТИР</p>
            <h2>
              MathScore —<br />
              ваша точка старту.
            </h2>
            <p>
              Від 0 до 100 — не шкільна оцінка й не ярлик.
              <br />
              Орієнтир, щоб підібрати навчання під вас.
            </p>
            <a className="button primary" href="#/account">
              Почати з однієї задачі ↗
            </a>
          </div>
          <div className="score-visual">
            <div className="score-title">
              ПРИКЛАД ПРОГРЕСУ<span>✦</span>
            </div>
            <div className="score-number">
              64<span>/100</span>
            </div>
            <p>
              Advanced <span className="rank-badge">Core</span>
            </p>
            <div className="score-track">
              <span />
            </div>
            <div className="score-axis">
              <span>0 · Starter</span>
              <span>100 · Master</span>
            </div>
            <p className="score-note">
              Ілюстрація шкали. Ваш результат визначатиметься після повної
              діагностики.
            </p>
          </div>
        </section>
        <section className="faq section" id="faq">
          <p className="eyebrow">ЩЕ ТРОХИ ЯСНОСТІ</p>
          <h2>Можливо, ви хотіли запитати</h2>
          {[
            [
              "Чи потрібно вже добре знати математику?",
              "Почати можна зі свого рівня. Мета діагностики — визначити старт і теми, яким потрібна увага.",
            ],
            [
              "Чим MathScore відрізняється від оцінки в школі?",
              "Це окрема шкала 0–100 для діагностики й підбору груп. Ранги показують прогрес, а підбір виконується за різницею балів.",
            ],
            [
              "Чи можна зараз записатися на навчання?",
              "Так. Створіть акаунт, пройдіть діагностику й оберіть курс та розклад у кабінеті. Підтвердження запису використовує тестову оплату без списання грошей.",
            ],
          ].map(([q, a]) => (
            <details key={q}>
              <summary>
                {q}
                <span>+</span>
              </summary>
              <p>{a}</p>
            </details>
          ))}
        </section>
      </main>
      <footer>
        <a className="footer-brand" href="#">
          MathHub<span>.ua</span>
        </a>
        <p>Математика, яка стає зрозумілою.</p>
        <span>Навчальний проєкт · КПІ · 2026</span>
      </footer>
    </>
  );
}
