"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

export default function BottomNav() {
  const pathname = usePathname();

  const navItems = [
    {
      href: "/dashboard",
      label: "Home"
    },
    {
      href: "/search",
      label: "Search"
    },
    {
      href: "/reports",
      label: "Reports"
    }
  ];

  if (pathname === "/login" || pathname === "/") return null;

  return (
    <div className="bottom-nav">
      {navItems.map((item) => {
        const isActive = pathname === item.href || pathname.startsWith(item.href);
        return (
          <Link key={item.label} href={item.href} className={`nav-item ${isActive ? "active" : ""}`} style={{ fontSize: "1rem", fontWeight: "600", textDecoration: "none" }}>
            <span>{item.label}</span>
          </Link>
        );
      })}
    </div>
  );
}
