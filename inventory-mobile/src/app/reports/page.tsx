"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

type Tab = "lowstock" | "pending" | "arrived" | "activity";

export default function ReportsPage() {
  const router = useRouter();
  
  const [lowStock, setLowStock] = useState<any[]>([]);
  const [pendingOrders, setPendingOrders] = useState<any[]>([]);
  const [arrivedOrders, setArrivedOrders] = useState<any[]>([]);
  const [activityLog, setActivityLog] = useState<any[]>([]);
  
  const [activeTab, setActiveTab] = useState<Tab>("lowstock");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchReports();
  }, [activeTab]);

  const fetchWithAuth = async (path: string) => {
    const token = localStorage.getItem("token");
    if (!token) {
      router.replace("/login");
      throw new Error("No token");
    }
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
    const res = await fetch(`${apiUrl}${path}`, {
      headers: { "Authorization": `Bearer ${token}` }
    });
    if (!res.ok) return [];
    return await res.json();
  };

  const fetchReports = async () => {
    setLoading(true);
    try {
      if (activeTab === "lowstock") {
        setLowStock(await fetchWithAuth("/api/reports/low-stock"));
      } else if (activeTab === "pending") {
        setPendingOrders(await fetchWithAuth("/api/reports/orders/pending"));
      } else if (activeTab === "arrived") {
        setArrivedOrders(await fetchWithAuth("/api/reports/orders/arrived"));
      } else if (activeTab === "activity") {
        setActivityLog(await fetchWithAuth("/api/reports/activity"));
      }
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  const markOrderArrived = async (orderIds: number[]) => {
    const token = localStorage.getItem("token");
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
    try {
      // Desktop sends multiple orderIds but our API might expect one. Let's assume we mark the first one for now or loop.
      // Wait, API `POST /api/stock/orders/{id}/arrive`.
      for (const id of orderIds) {
        await fetch(`${apiUrl}/api/stock/orders/${id}/arrive`, {
          method: "POST",
          headers: { "Authorization": `Bearer ${token}` }
        });
      }
      alert("Marked as arrived!");
      fetchReports();
    } catch(e) {
      alert("Network error");
    }
  };

  return (
    <div className="container" style={{ paddingBottom: "5rem" }}>
      <div style={{ marginTop: "1rem", display: "flex", alignItems: "center", gap: "1rem", marginBottom: "1rem" }}>
        <button className="btn btn-secondary" style={{ padding: "0.5rem", minWidth: "44px" }} onClick={() => router.back()}>
          Back
        </button>
        <h1 style={{ margin: 0, fontSize: "1.5rem", color: "var(--primary)" }}>Reports</h1>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.5rem", marginBottom: "1rem" }}>
        <button 
          style={{ padding: "0.5rem", borderRadius: "var(--radius-sm)", border: "1px solid var(--border)", backgroundColor: activeTab === 'lowstock' ? "var(--primary)" : "var(--surface)", color: activeTab === 'lowstock' ? "white" : "var(--foreground)", fontWeight: "600", fontSize: "0.85rem" }}
          onClick={() => setActiveTab('lowstock')}
        >
          Low Stock
        </button>
        <button 
          style={{ padding: "0.5rem", borderRadius: "var(--radius-sm)", border: "1px solid var(--border)", backgroundColor: activeTab === 'pending' ? "var(--primary)" : "var(--surface)", color: activeTab === 'pending' ? "white" : "var(--foreground)", fontWeight: "600", fontSize: "0.85rem" }}
          onClick={() => setActiveTab('pending')}
        >
          Pending
        </button>
        <button 
          style={{ padding: "0.5rem", borderRadius: "var(--radius-sm)", border: "1px solid var(--border)", backgroundColor: activeTab === 'arrived' ? "var(--primary)" : "var(--surface)", color: activeTab === 'arrived' ? "white" : "var(--foreground)", fontWeight: "600", fontSize: "0.85rem" }}
          onClick={() => setActiveTab('arrived')}
        >
          Arrived
        </button>
        <button 
          style={{ padding: "0.5rem", borderRadius: "var(--radius-sm)", border: "1px solid var(--border)", backgroundColor: activeTab === 'activity' ? "var(--primary)" : "var(--surface)", color: activeTab === 'activity' ? "white" : "var(--foreground)", fontWeight: "600", fontSize: "0.85rem" }}
          onClick={() => setActiveTab('activity')}
        >
          Activity Log
        </button>
      </div>

      {loading ? (
        <p style={{ textAlign: "center", opacity: 0.7 }}>Loading reports...</p>
      ) : (
        <div className="animate-slide-up" style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          
          {activeTab === 'lowstock' && (
            <>
              {lowStock.length === 0 && <p style={{ textAlign: "center", opacity: 0.7 }}>No low stock items.</p>}
              {lowStock.map((item, idx) => (
                <div key={idx} className="glass-panel hover-panel">
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "0.5rem" }}>
                    <div>
                      <h3 style={{ fontSize: "1.1rem", marginBottom: "0.25rem" }}>{item.description}</h3>
                      <p style={{ margin: 0, fontSize: "0.85rem", opacity: 0.8 }}>{item.manufacturer} {item.model}</p>
                    </div>
                    <span style={{ background: "rgba(239, 68, 68, 0.1)", color: "var(--danger)", padding: "0.25rem 0.5rem", borderRadius: "var(--radius-sm)", fontWeight: "bold", fontSize: "0.85rem" }}>
                      {item.quantity} left
                    </span>
                  </div>
                  <div className="data-row">
                    <span className="data-label">Barcode</span>
                    <span className="data-value">{item.barcode}</span>
                  </div>
                  <div className="data-row">
                    <span className="data-label">Threshold</span>
                    <span className="data-value">{item.lowStockThreshold}</span>
                  </div>
                </div>
              ))}
            </>
          )}

          {activeTab === 'pending' && (
            <>
              {pendingOrders.length === 0 && <p style={{ textAlign: "center", opacity: 0.7 }}>No pending orders.</p>}
              {pendingOrders.map((order, idx) => (
                <div key={idx} className="glass-panel hover-panel">
                  <h3 style={{ fontSize: "1.1rem", marginBottom: "0.25rem" }}>{order.itemName}</h3>
                  <div className="data-row">
                    <span className="data-label">Qty Ordered</span>
                    <span className="data-value">{order.totalQuantity}</span>
                  </div>
                  <div className="data-row">
                    <span className="data-label">Barcode</span>
                    <span className="data-value">{order.barcode}</span>
                  </div>
                  <button className="btn btn-primary" style={{ width: "100%", marginTop: "1rem" }} onClick={() => markOrderArrived(order.orderIds)}>
                    Mark as Arrived
                  </button>
                </div>
              ))}
            </>
          )}

          {activeTab === 'arrived' && (
            <>
              {arrivedOrders.length === 0 && <p style={{ textAlign: "center", opacity: 0.7 }}>No arrived orders.</p>}
              {arrivedOrders.map((order, idx) => (
                <div key={idx} className="glass-panel hover-panel">
                  <h3 style={{ fontSize: "1.1rem", marginBottom: "0.25rem" }}>{order.itemName}</h3>
                  <div className="data-row">
                    <span className="data-label">Qty Arrived</span>
                    <span className="data-value">{order.totalQuantity}</span>
                  </div>
                  <div className="data-row">
                    <span className="data-label">Arrived On</span>
                    <span className="data-value">{new Date(order.createdAt).toLocaleDateString()}</span>
                  </div>
                </div>
              ))}
            </>
          )}

          {activeTab === 'activity' && (
            <>
              {activityLog.length === 0 && <p style={{ textAlign: "center", opacity: 0.7 }}>No recent activity.</p>}
              {activityLog.map((log, idx) => (
                <div key={idx} className="glass-panel hover-panel">
                  <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "0.5rem" }}>
                    <span style={{ fontWeight: "600", color: log.actionType === "REMOVE" ? "var(--danger)" : "var(--success)" }}>
                      {log.actionType} {Math.abs(log.quantityChange)}
                    </span>
                    <span style={{ fontSize: "0.85rem", opacity: 0.7 }}>{new Date(log.timestamp).toLocaleString()}</span>
                  </div>
                  <div style={{ fontSize: "1rem", marginBottom: "0.25rem" }}>{log.description}</div>
                  <div style={{ fontSize: "0.85rem", opacity: 0.7 }}>Barcode: {log.barcode}</div>
                  <div style={{ fontSize: "0.75rem", opacity: 0.5, marginTop: "0.5rem" }}>User/Machine: {log.machineName}</div>
                </div>
              ))}
            </>
          )}

        </div>
      )}
    </div>
  );
}
