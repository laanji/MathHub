import { useEffect, useState } from "react";
import { Landing } from "./Landing";
import { api, initializeSession, logout } from "./lib/api";
import type { Account } from "./lib/types";
import { AuthPage } from "./features/account/AuthPage";
import { StudentDashboard } from "./features/account/StudentDashboard";
import { ParentDashboard } from "./features/account/ParentDashboard";
import { AdminBank } from "./features/account/AdminBank";
import "./portal.css";
import {
  CoursesPage,
  TeachersPage,
  PaymentsPage,
  SignInPrompt,
} from "./features/catalog/CatalogPages";
export function App() {
  const [route, setRoute] = useState(location.hash);
  const [account, setAccount] = useState<Account | null>(null);
  const [ready, setReady] = useState(false);
  useEffect(() => {
    const change = () => {
      setRoute(location.hash);
      if (location.hash.startsWith("#/")) window.scrollTo(0, 0);
    };
    window.addEventListener("hashchange", change);
    initializeSession()
      .then(async (ok) => {
        if (ok) setAccount(await api<Account>("/auth/me"));
      })
      .catch(() => setAccount(null))
      .finally(() => setReady(true));
    return () => window.removeEventListener("hashchange", change);
  }, []);
  async function signOut() {
    await logout();
    setAccount(null);
    location.hash = "/login";
  }
  if (!route.startsWith("#/")) return <Landing />;
  const page = route.split("?")[0];
  const register = route === "#/register";
  const authRoute = register || route === "#/login";
  return (
    <>
      <header className="header portal-header">
        <a className="brand" href="#" aria-label="MathHub — на головну">
          <img src="/brand/mathhub.png" alt="MathHub.ua" />
        </a>
        <nav>
          <a href="#">Головна</a>
          <a
            href="#/courses"
            aria-current={page === "#/courses" ? "page" : undefined}
          >
            Курси
          </a>
          <a
            href="#/teachers"
            aria-current={page === "#/teachers" ? "page" : undefined}
          >
            Наші викладачі
          </a>
          <a
            href="#/groups"
            aria-current={page === "#/groups" ? "page" : undefined}
          >
            Групи та записи
          </a>
          <a
            href="#/payments"
            aria-current={page === "#/payments" ? "page" : undefined}
          >
            Оплата
          </a>
          {account && <a href="#/account">Мій кабінет</a>}
        </nav>
        {account ? (
          <div className="account-menu">
            <span>{account.displayName}</span>
            <button
              className="button secondary"
              onClick={() => {
                signOut().catch(() => {
                  setAccount(null);
                  location.hash = "/login";
                });
              }}
            >
              Вийти
            </button>
          </div>
        ) : (
          <a className="header-cta" href={register ? "#/login" : "#/register"}>
            {register ? "Увійти" : "Створити акаунт"}
          </a>
        )}
      </header>
      {page === "#/courses" ? (
        <CoursesPage />
      ) : page === "#/teachers" ? (
        <TeachersPage />
      ) : !ready ? (
        <main className="portal">
          <p role="status">Перевіряємо сесію…</p>
        </main>
      ) : (page === "#/groups" || page === "#/payments") && !account ? (
        <SignInPrompt
          title={page === "#/groups" ? "Групи та мої записи" : "Оплата"}
        />
      ) : page === "#/payments" && account ? (
        <PaymentsPage account={account} />
      ) : authRoute || !account ? (
        <AuthPage
          key={register ? "register" : "login"}
          register={register}
          onSignedIn={setAccount}
        />
      ) : account.role === "Student" ? (
        <StudentDashboard
          key={page}
          account={account}
          onAccount={setAccount}
          groupsOnly={page === "#/groups"}
        />
      ) : account.role === "Parent" ? (
        <ParentDashboard account={account} />
      ) : (
        <AdminBank />
      )}
      <footer>
        <a className="footer-brand" href="#">
          MathHub<span>.ua</span>
        </a>
        <p>Ваш наступний крок у математиці.</p>
        <span>Навчальний MVP · 2026</span>
      </footer>
    </>
  );
}
