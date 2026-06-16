import type { Metadata, Viewport } from "next";
import { Fira_Code, Fira_Sans } from "next/font/google";
import "./globals.css";

const firaCode = Fira_Code({ subsets: ["latin"], variable: "--font-fira-code" });
const firaSans = Fira_Sans({ subsets: ["latin"], weight: ["300", "400", "500", "600", "700"], variable: "--font-fira-sans" });

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  maximumScale: 1,
  userScalable: false,
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#FAF5FF" },
    { media: "(prefers-color-scheme: dark)", color: "#0f172a" },
  ],
};

export const metadata: Metadata = {
  title: "Inventory Mobile",
  description: "Enterprise Data Dashboard",
  appleWebApp: {
    capable: true,
    statusBarStyle: "default",
    title: "Inventory",
  },
};

import BottomNav from "@/components/BottomNav";

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={`${firaSans.variable} ${firaCode.variable}`}>
        {children}
        <BottomNav />
      </body>
    </html>
  );
}
