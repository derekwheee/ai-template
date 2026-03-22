import path from "path";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

// https://vite.dev/config/
export default defineConfig({
    plugins: [react(), tailwindcss()],
    resolve: {
        alias: {
            "@": path.resolve(__dirname, "./src"),
        },
    },
    server: {
        port: parseInt(process.env.PORT ?? "5173"),
        proxy: {
            "/api": {
                target:
                    process.env.services__api__https__0 ??
                    process.env.services__api__http__0 ??
                    "http://localhost:5000",
                changeOrigin: true,
            },
        },
    },
});
