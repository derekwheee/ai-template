const TOKEN_KEY = "auth_token";

export interface AuthUser {
    id: string;
    username: string;
}

export interface LoginResponse {
    token: string;
    username: string;
}

type ApiErrorBody = {
    errors?: string[];
};

type ApiRequestOptions = Omit<RequestInit, "headers"> & {
    headers?: Record<string, string>;
};

async function apiFetch<T>(path: string, options?: ApiRequestOptions): Promise<T> {
    const res = await fetch(path, {
        ...options,
        headers: {
            "Content-Type": "application/json",
            ...getAuthHeader(),
            ...options?.headers,
        },
    });

    if (!res.ok) {
        const body = await res.json().catch(() => ({}) as ApiErrorBody);
        const message =
            (body as ApiErrorBody).errors?.join(", ") ?? `Request failed: ${res.status}`;
        throw new Error(message);
    }

    return res.json() as Promise<T>;
}

function getAuthHeader(): Record<string, string> {
    const token = getToken();
    return token ? { Authorization: `Bearer ${token}` } : {};
}

export function getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
}

export function clearToken(): void {
    localStorage.removeItem(TOKEN_KEY);
}

export function isAuthenticated(): boolean {
    return getToken() !== null;
}

export const authApi = {
    register: (username: string, password: string) =>
        apiFetch<{ message: string }>("/api/auth/register", {
            method: "POST",
            body: JSON.stringify({ username, password }),
        }),

    login: (username: string, password: string) =>
        apiFetch<LoginResponse>("/api/auth/login", {
            method: "POST",
            body: JSON.stringify({ username, password }),
        }),

    me: () => apiFetch<AuthUser>("/api/auth/me"),
};
