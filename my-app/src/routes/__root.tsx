import type { JSX } from "react";
import { Outlet } from "@tanstack/react-router";

export function RootLayout(): JSX.Element {
    return (
        <div className="min-h-screen bg-background">
            <Outlet />
        </div>
    );
}
