"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import styles from "./page.module.css";

export default function LoginPage() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
      const response = await fetch(`${apiUrl}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });

      const data = await response.json();

      if (response.ok) {
        // Store token and redirect
        localStorage.setItem("token", data.token);
        localStorage.setItem("username", data.username);
        router.push("/dashboard");
      } else {
        setError(data.message || "Login failed. Please check your credentials.");
      }
    } catch (err) {
      setError("Unable to connect to the server. Please check your connection.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      <div className={`glass-panel ${styles.loginCard} animate-slide-up`}>
        <div className={styles.header}>
          <div className={styles.logo}>📦</div>
          <h1 className={styles.title}>Inventory App</h1>
          <p className={styles.subtitle}>Sign in to manage your stock</p>
        </div>

        <form onSubmit={handleLogin} className={styles.form}>
          <div className="input-group">
            <label htmlFor="username">Username</label>
            <input
              id="username"
              type="text"
              className="input-control"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              placeholder="Enter your username"
            />
          </div>

          <div className="input-group">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              className="input-control"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              placeholder="Enter your password"
            />
          </div>

          <button
            type="submit"
            className={`btn btn-primary ${styles.submitBtn}`}
            disabled={loading}
          >
            {loading ? "Signing in..." : "Sign In"}
          </button>
        </form>

        {error && <div className={styles.error}>{error}</div>}
      </div>
    </div>
  );
}
