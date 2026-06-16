"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function Root() {
  const router = useRouter();

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (token) {
      router.replace("/dashboard");
    } else {
      router.replace("/login");
    }
  }, [router]);

  return (
    <div style={{ height: "100vh", display: "flex", justifyContent: "center", alignItems: "center" }}>
      <div className="logo animate-bounce" style={{ fontSize: "3rem" }}>📦</div>
    </div>
  );
}
