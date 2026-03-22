import { createRouter, createRoute, createRootRoute, redirect } from "@tanstack/react-router";
import { RootLayout } from "./routes/__root";
import { LoginPage } from "./routes/login";
import { RegisterPage } from "./routes/register";
import { DashboardPage } from "./routes/dashboard";
import { isAuthenticated } from "./lib/auth";

const rootRoute = createRootRoute({ component: RootLayout });

const loginRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/login",
    component: LoginPage,
    beforeLoad: () => {
        if (isAuthenticated()) throw redirect({ to: "/" });
    },
});

const registerRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/register",
    component: RegisterPage,
    beforeLoad: () => {
        if (isAuthenticated()) throw redirect({ to: "/" });
    },
});

const dashboardRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/",
    component: DashboardPage,
    beforeLoad: () => {
        if (!isAuthenticated()) throw redirect({ to: "/login" });
    },
});

const routeTree = rootRoute.addChildren([loginRoute, registerRoute, dashboardRoute]);

export const router = createRouter({ routeTree });

declare module "@tanstack/react-router" {
    interface Register {
        router: typeof router;
    }
}
