import { test, expect, type Page } from "@playwright/test";

const FAKE_TOKEN = "fake.jwt.token";
const FAKE_USER = { id: "1", username: "alice" };

/** Sets auth_token in localStorage before the page scripts run. */
async function setAuthToken(page: Page, token = FAKE_TOKEN): Promise<void> {
    await page.addInitScript((t) => localStorage.setItem("auth_token", t), token);
}

/** Mocks GET /api/auth/me with a successful user response. */
async function mockMe(page: Page, user = FAKE_USER): Promise<void> {
    await page.route("**/api/auth/me", (route) => route.fulfill({ json: user }));
}

test.describe("Login page", () => {
    test("renders the sign-in form", async ({ page }) => {
        await page.goto("/login");
        await expect(page.locator('[data-slot="card-title"]')).toContainText("Sign in");
        await expect(page.getByLabel("Username")).toBeVisible();
        await expect(page.getByLabel("Password")).toBeVisible();
        await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
    });

    test("shows validation error for empty fields", async ({ page }) => {
        await page.goto("/login");
        await page.getByRole("button", { name: "Sign in" }).click();
        await expect(page.getByText("Username is required")).toBeVisible();
    });

    test("shows validation error for short password", async ({ page }) => {
        await page.goto("/login");
        await page.getByLabel("Username").fill("alice");
        await page.getByLabel("Password").fill("abc");
        await page.getByRole("button", { name: "Sign in" }).click();
        await expect(page.getByText("Password must be at least 6 characters")).toBeVisible();
    });

    test("redirects to dashboard on successful login", async ({ page }) => {
        await page.route("**/api/auth/login", (route) =>
            route.fulfill({ json: { token: FAKE_TOKEN, username: "alice" } })
        );
        await mockMe(page);

        await page.goto("/login");
        await page.getByLabel("Username").fill("alice");
        await page.getByLabel("Password").fill("password123");
        await page.getByRole("button", { name: "Sign in" }).click();

        await expect(page).toHaveURL("/");
        await expect(page.getByText("Welcome, alice!")).toBeVisible();
    });

    test("shows error message on failed login", async ({ page }) => {
        await page.route("**/api/auth/login", (route) =>
            route.fulfill({ status: 401, json: {} })
        );

        await page.goto("/login");
        await page.getByLabel("Username").fill("alice");
        await page.getByLabel("Password").fill("wrongpass");
        await page.getByRole("button", { name: "Sign in" }).click();

        await expect(page.getByText(/request failed/i)).toBeVisible();
    });

    test("redirects to dashboard if already authenticated", async ({ page }) => {
        await setAuthToken(page);
        await mockMe(page);

        await page.goto("/login");
        await expect(page).toHaveURL("/");
    });
});

test.describe("Register page", () => {
    test("renders the registration form", async ({ page }) => {
        await page.goto("/register");
        await expect(page.locator('[data-slot="card-title"]')).toContainText("Create an account");
        await expect(page.getByLabel("Username")).toBeVisible();
        await expect(page.getByLabel("Password", { exact: true })).toBeVisible();
        await expect(page.getByLabel("Confirm password")).toBeVisible();
    });

    test("shows validation error when passwords do not match", async ({ page }) => {
        await page.goto("/register");
        await page.getByLabel("Username").fill("newuser");
        await page.getByLabel("Password", { exact: true }).fill("password123");
        await page.getByLabel("Confirm password").fill("different123");
        await page.getByRole("button", { name: "Create account" }).click();
        await expect(page.getByText("Passwords do not match")).toBeVisible();
    });

    test("redirects to dashboard after successful registration", async ({ page }) => {
        await page.route("**/api/auth/register", (route) =>
            route.fulfill({ json: { message: "User created" } })
        );
        await page.route("**/api/auth/login", (route) =>
            route.fulfill({ json: { token: FAKE_TOKEN, username: "newuser" } })
        );
        await page.route("**/api/auth/me", (route) =>
            route.fulfill({ json: { id: "2", username: "newuser" } })
        );

        await page.goto("/register");
        await page.getByLabel("Username").fill("newuser");
        await page.getByLabel("Password", { exact: true }).fill("password123");
        await page.getByLabel("Confirm password").fill("password123");
        await page.getByRole("button", { name: "Create account" }).click();

        await expect(page).toHaveURL("/");
        await expect(page.getByText("Welcome, newuser!")).toBeVisible();
    });
});

test.describe("Dashboard", () => {
    test("redirects to login when not authenticated", async ({ page }) => {
        await page.goto("/");
        await expect(page).toHaveURL("/login");
    });

    test("shows username when authenticated", async ({ page }) => {
        await setAuthToken(page);
        await mockMe(page);

        await page.goto("/");
        await expect(page.getByText("Welcome, alice!")).toBeVisible();
    });

    test("signing out redirects to login", async ({ page }) => {
        await setAuthToken(page);
        await mockMe(page);

        await page.goto("/");
        await page.getByRole("button", { name: "Sign out" }).click();
        await expect(page).toHaveURL("/login");
    });
});
