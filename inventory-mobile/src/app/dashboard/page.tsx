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
    { 
      name: "Add Stock", 
      desc: "Increase inventory quantities", 
      path: "/add-stock", 
      color: "var(--accent)",
      icon: (
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" style={{width: 44, height: 44}}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
        </svg>
      )
    },
    { 
      name: "Remove Stock", 
      desc: "Decrease inventory quantities", 
      path: "/remove-stock", 
      color: "var(--danger)",
      icon: (
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" style={{width: 44, height: 44}}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 12h-15" />
        </svg>
      )
    },
    { 
      name: "Search / Items", 
      desc: "Find and view inventory", 
      path: "/search", 
      color: "var(--primary)",
      icon: (
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" style={{width: 44, height: 44}}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
        </svg>
      )
    },
    { 
      name: "Register Item", 
      desc: "Add new inventory item", 
      path: "/register-item", 
      color: "var(--purple)",
      icon: (
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" style={{width: 44, height: 44}}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v6m3-3H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      )
    },
    { 
      name: "Reports", 
      desc: "View analytics and reports", 
      path: "/reports", 
      color: "var(--warning)",
      icon: (
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" style={{width: 44, height: 44}}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 013 19.875v-6.75zM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V8.625zM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 01-1.125-1.125V4.125z" />
        </svg>
      )
    },
    { 
      name: "Sign Out", 
      desc: "Log out of your account", 
      path: "#", 
      color: "var(--slate)", 
      action: handleSignOut,
      icon: (
        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" style={{width: 44, height: 44}}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75" />
        </svg>
      )
    },
  ];

  return (
    <div className="container" style={{ paddingBottom: "5rem" }}>
      <div className="glass-panel" style={{ marginTop: "1.5rem", marginBottom: "1.5rem", display: "flex", justifyContent: "space-between", alignItems: "center", padding: "1.5rem" }}>
        <div>
          <h2 style={{ fontSize: "1.5rem", fontWeight: 700, color: "var(--primary)", marginBottom: "0.25rem" }}>Alpine Auto A/C</h2>
          <p style={{ fontSize: "1rem", color: "var(--text-secondary)" }}>Welcome, {userName}</p>
        </div>
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
              padding: "0", 
              overflow: "hidden",
              cursor: "pointer",
              userSelect: "none",
              WebkitUserSelect: "none"
            }}
            onClick={() => {
              if (item.action) item.action();
              else router.push(item.path);
            }}
          >
            <div style={{
              width: "100%",
              height: "180px",
              backgroundColor: item.color,
              color: "white",
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              justifyContent: "center",
              gap: "0.5rem",
              padding: "1rem",
              textAlign: "center"
            }}>
              {item.icon}
              <span style={{ fontSize: "1.1rem", fontWeight: 700 }}>{item.name}</span>
              <p style={{ fontSize: "0.85rem", opacity: 0.9, margin: 0 }}>{item.desc}</p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
