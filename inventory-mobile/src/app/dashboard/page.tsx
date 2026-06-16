"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

export default function DashboardPage() {
  const router = useRouter();
  const [userName, setUserName] = useState("User");

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token) {
      router.replace("/login");
      return;
    }
    // Simple decode of JWT payload
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      if (payload.unique_name) {
        setUserName(payload.unique_name);
      }
    } catch(e) {}
  }, [router]);

  const handleSignOut = () => {
    localStorage.removeItem("token");
    router.replace("/login");
  };

    const navItems = [
    { name: "Add Stock", desc: "Increase inventory", path: "/add-stock", color: "#10B981" },
    { name: "Remove Stock", desc: "Decrease inventory", path: "/remove-stock", color: "#EF4444" },
    { name: "Search / Items", desc: "Find inventory", path: "/search", color: "#2563EB" },
    { name: "New Item", desc: "Create inventory", path: "/items/new", color: "#8B5CF6" },
    { name: "Reports", desc: "Analytics & alerts", path: "/reports", color: "#F59E0B" },
    { name: "Sign Out", desc: "Log out", path: "#", color: "#64748B", action: handleSignOut },
  ];

  return (
    <div className="container" style={{ paddingBottom: "5rem" }}>
      <div style={{ marginTop: "2rem", marginBottom: "2rem" }}>
        <h1 style={{ margin: 0, fontSize: "2rem", color: "var(--primary)" }}>Alpine Auto A/C</h1>
        <p style={{ opacity: 0.8, fontSize: "1.1rem" }}>Welcome, {userName}. Select an action below.</p>
      </div>

      <div style={{
        display: "grid",
        gridTemplateColumns: "1fr 1fr",
        gap: "1rem"
      }}>
        {navItems.map((item, idx) => (
          <div 
            key={idx} 
            className="glass-panel" 
            style={{ 
              padding: "1.5rem 1rem", 
              backgroundColor: item.color, 
              color: "white", 
              textAlign: "center",
              cursor: "pointer",
              border: "none",
              display: "flex",
              flexDirection: "column",
              justifyContent: "center",
              alignItems: "center"
            }}
            onClick={() => {
              if (item.action) item.action();
              else router.push(item.path);
            }}
          >
            <div style={{ fontWeight: "bold", fontSize: "1.2rem", marginBottom: "0.5rem" }}>
              {item.name}
            </div>
            <div style={{ fontSize: "0.85rem", opacity: 0.9 }}>
              {item.desc}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
