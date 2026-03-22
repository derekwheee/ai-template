import type { JSX } from "react";
import { useQuery } from "@tanstack/react-query";
import { useRouter } from "@tanstack/react-router";
import { authApi, clearToken } from "@/lib/auth";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

export function DashboardPage(): JSX.Element | null {
    const router = useRouter();

    const { data: user, isLoading } = useQuery({
        queryKey: ["me"],
        queryFn: authApi.me,
        retry: false,
    });

    // Token expired or invalid — clear it and redirect to login
    if (!isLoading && !user) {
        clearToken();
        router.navigate({ to: "/login" });
        return null;
    }

    const handleLogout = (): void => {
        clearToken();
        router.navigate({ to: "/login" });
    };

    if (isLoading) {
        return (
            <div className="flex min-h-screen items-center justify-center">
                <p className="text-muted-foreground">Loading…</p>
            </div>
        );
    }

    return (
        <div className="flex min-h-screen items-center justify-center p-4">
            <Card className="w-full max-w-sm">
                <CardHeader>
                    <CardTitle>Welcome, {user?.username}!</CardTitle>
                </CardHeader>
                <CardContent>
                    <Button variant="outline" onClick={handleLogout} className="w-full">
                        Sign out
                    </Button>
                </CardContent>
            </Card>
        </div>
    );
}
