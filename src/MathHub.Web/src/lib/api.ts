let accessToken: string | null = null;
let refreshing: Promise<boolean> | null = null;
export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public code: string,
  ) {
    super(message);
  }
}
async function parseError(response: Response) {
  const body = await response.json().catch(() => null);
  return new ApiError(
    body?.detail ??
      (response.status === 429
        ? "Забагато спроб. Зачекайте хвилину."
        : response.status === 401
          ? "Сесію завершено. Увійдіть знову."
          : "Не вдалося виконати запит. Спробуйте ще раз."),
    response.status,
    body?.code ?? "request.failed",
  );
}
export function refreshSession(): Promise<boolean> {
  if (refreshing) return refreshing;
  refreshing = (async () => {
    try {
      const r = await fetch("/api/auth/refresh", {
        method: "POST",
        credentials: "include",
      });
      if (!r.ok) {
        accessToken = null;
        return false;
      }
      const t = await r.json();
      accessToken = t.accessToken;
      return true;
    } catch {
      accessToken = null;
      return false;
    }
  })().finally(() => {
    refreshing = null;
  });
  return refreshing;
}
export async function api<T>(
  path: string,
  method = "GET",
  body?: unknown,
  retry = true,
): Promise<T> {
  let response: Response;
  try {
    response = await fetch("/api" + path, {
      method,
      credentials: "include",
      headers: {
        ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
        ...(accessToken ? { Authorization: "Bearer " + accessToken } : {}),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(
      "Немає зв’язку із сервером. Перевірте підключення та повторіть спробу.",
      0,
      "network",
    );
  }
  if (
    response.status === 401 &&
    retry &&
    !path.startsWith("/auth/") &&
    (await refreshSession())
  )
    return api<T>(path, method, body, false);
  if (!response.ok) throw await parseError(response);
  if (response.status === 204) return undefined as T;
  const text = await response.text();
  return text ? (JSON.parse(text) as T) : (null as T);
}
export async function login(email: string, password: string) {
  const t = await api<{ accessToken: string }>("/auth/login", "POST", {
    email,
    password,
  });
  accessToken = t.accessToken;
}
export async function logout() {
  try {
    await api("/auth/logout", "POST");
  } finally {
    accessToken = null;
  }
}
let initialSession: Promise<boolean> | null = null;
export function initializeSession() {
  return (initialSession ??= refreshSession());
}
