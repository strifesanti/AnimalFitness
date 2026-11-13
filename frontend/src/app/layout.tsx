import type { Metadata } from "next";
import { Bebas_Neue, Inter } from "next/font/google";
import { AuthProvider } from "@/features/auth/auth-context";
import "./globals.css";

const display = Bebas_Neue({
  variable: "--font-display",
  subsets: ["latin"],
  weight: "400",
});

const body = Inter({
  variable: "--font-body",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "AnimalFitness",
  description: "Entrenamiento sin límites. Plataforma SaaS de gestión deportiva.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="es"
      className={`${display.variable} ${body.variable} h-full antialiased`}
    >
      <body className="min-h-full">
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
