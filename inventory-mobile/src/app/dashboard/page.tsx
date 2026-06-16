"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

export default function DashboardPage() {
  const [lowStock, setLowStock] = useState<any[]>([]);
  const [orders, setOrders] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token) {
      router.replace("/login");
      return;
    }

    const fetchData = async () => {
      try {
        const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
        
        // Fetch low stock
        const resLowStock = await fetch(`${apiUrl}/api/reports/low-stock`, {
          headers: { "Authorization": `Bearer ${token}` }
        });
        if (resLowStock.ok) {
          const data = await resLowStock.json();
          setLowStock(data);
        } else if (resLowStock.status === 401) {
          localStorage.removeItem("token");
          router.replace("/login");
          return;
        }

        // Fetch pending orders
        const resOrders = await fetch(`${apiUrl}/api/reports/orders/pending`, {
          headers: { "Authorization": `Bearer ${token}` }
        });
        if (resOrders.ok) {
          const data = await resOrders.json();
          setOrders(data);
        }

      } catch (err) {
        console.error("Failed to fetch dashboard data", err);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [router]);

  if (loading) {
    return (
      <div style={{ padding: "2rem", textAlign: "center" }}>
        <div className="logo animate-bounce" style={{ fontSize: "2rem" }}>📦</div>
        <p>Loading dashboard...</p>
      </div>
    );
  }

  return (
    <div className="container" style={{ paddingBottom: "5rem" }}>
      <h1 style={{ marginTop: "1rem" }}>Dashboard</h1>

      <section style={{ marginBottom: "2rem" }}>
        <h2 style={{ fontSize: "1.25rem", color: "var(--warning)" }}>⚠️ Low Stock ({lowStock.length})</h2>
        {lowStock.length === 0 ? (
          <p>No items are low on stock!</p>
        ) : (
          <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
            {lowStock.map((item, idx) => (
              <div key={idx} className="glass-panel" style={{ padding: "1rem" }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "1rem" }}>
                  <div>
                    <h3 style={{ fontSize: "1.1rem", marginBottom: "0.25rem" }}>{item.description}</h3>
                    <p style={{ margin: 0, fontSize: "0.85rem", opacity: 0.8 }}>{item.manufacturer} {item.model}</p>
                    <p style={{ margin: 0, fontSize: "0.85rem", opacity: 0.8 }}>Barcode: {item.barcode}</p>
                  </div>
                  <div style={{ textAlign: "right" }}>
                    <span style={{ 
                      background: "rgba(239, 68, 68, 0.2)", 
                      color: "var(--danger)", 
                      padding: "0.25rem 0.5rem", 
                      borderRadius: "var(--radius-full)",
                      fontWeight: "bold"
                    }}>
                      {item.quantity} left
                    </span>
                  </div>
                </div>
                <button className="btn btn-secondary" style={{ width: "100%", fontSize: "0.875rem" }} onClick={async () => {
                   const token = localStorage.getItem("token");
                   const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
                   // In a real app we would call a specific order endpoint
                   alert("Order functionality to be implemented in API");
                }}>
                  Place Order
                </button>
              </div>
            ))}
          </div>
        )}
      </section>

      <section>
        <h2 style={{ fontSize: "1.25rem", color: "var(--primary)" }}>📦 Pending Orders ({orders.length})</h2>
        {orders.length === 0 ? (
          <p>No pending orders!</p>
        ) : (
          <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
            {orders.map((order, idx) => (
              <div key={idx} className="glass-panel" style={{ padding: "1rem" }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <div>
                    <h3 style={{ fontSize: "1.1rem", marginBottom: "0.25rem" }}>{order.itemName}</h3>
                    <p style={{ margin: 0, fontSize: "0.85rem", opacity: 0.8 }}>Order Qty: {order.totalQuantity}</p>
                  </div>
                  <button className="btn btn-primary" style={{ padding: "0.5rem 1rem", fontSize: "0.875rem" }}>
                    Mark Arrived
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
