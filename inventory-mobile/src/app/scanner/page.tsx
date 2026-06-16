"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Html5QrcodeScanner } from "html5-qrcode";

export default function ScannerPage() {
  const [scanResult, setScanResult] = useState<string | null>(null);
  const [item, setItem] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token) {
      router.replace("/login");
      return;
    }

    const scanner = new Html5QrcodeScanner(
      "reader",
      { fps: 10, qrbox: { width: 250, height: 100 } },
      /* verbose= */ false
    );

    scanner.render(
      (decodedText) => {
        setScanResult(decodedText);
        scanner.clear();
      },
      (errorMessage) => {
        // ignore errors during scanning
      }
    );

    return () => {
      scanner.clear().catch(e => console.error(e));
    };
  }, [router]);

  useEffect(() => {
    if (scanResult) {
      fetchItemDetails(scanResult);
    }
  }, [scanResult]);

  const fetchItemDetails = async (barcode: string) => {
    setLoading(true);
    setError(null);
    try {
      const token = localStorage.getItem("token");
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
      const res = await fetch(`${apiUrl}/api/stock/barcode/${barcode}`, {
        headers: { "Authorization": `Bearer ${token}` }
      });

      if (res.ok) {
        const data = await res.json();
        setItem(data);
      } else if (res.status === 404) {
        setError(`Item not found for barcode: ${barcode}`);
      } else {
        setError("Error fetching item details");
      }
    } catch (err) {
      setError("Network error fetching details.");
    } finally {
      setLoading(false);
    }
  };

  const handleAction = async (action: 'add' | 'remove') => {
    if (!item) return;
    try {
      const token = localStorage.getItem("token");
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
      const res = await fetch(`${apiUrl}/api/stock/${action}`, {
        method: "POST",
        headers: { 
          "Authorization": `Bearer ${token}`,
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ barcode: item.barcode, quantity: 1 })
      });

      if (res.ok) {
        alert(`Successfully ${action === 'add' ? 'added' : 'removed'} 1 item`);
        fetchItemDetails(item.barcode); // refresh
      } else {
        const errorData = await res.json();
        alert(`Error: ${errorData.message}`);
      }
    } catch (err) {
      alert("Network error.");
    }
  };

  return (
    <div className="container" style={{ paddingBottom: "5rem" }}>
      <h1 style={{ marginTop: "1rem" }}>Scan Barcode</h1>

      {!scanResult && (
        <div className="glass-panel" style={{ padding: "0" }}>
          <div id="reader" style={{ width: "100%", borderRadius: "var(--radius-lg)", overflow: "hidden" }}></div>
        </div>
      )}

      {loading && <p style={{ marginTop: "1rem", textAlign: "center" }}>Loading item details...</p>}

      {error && (
        <div style={{ marginTop: "1rem" }} className="glass-panel">
          <p style={{ color: "var(--danger)" }}>{error}</p>
          <button className="btn btn-secondary" onClick={() => { setScanResult(null); setItem(null); setError(null); }} style={{ marginTop: "1rem", width: "100%" }}>Scan Again</button>
        </div>
      )}

      {item && (
        <div className="glass-panel animate-slide-up" style={{ marginTop: "1rem" }}>
          <h2 style={{ fontSize: "1.5rem", color: "var(--primary)" }}>{item.description}</h2>
          <p style={{ opacity: 0.8, marginBottom: "0.5rem" }}>{item.partType?.name} - {item.partBrand?.name}</p>
          <p style={{ opacity: 0.8, marginBottom: "1rem" }}>{item.vehicleModel?.manufacturer?.name} {item.vehicleModel?.name}</p>
          
          <div style={{ display: "flex", justifyContent: "space-between", background: "rgba(0,0,0,0.1)", padding: "1rem", borderRadius: "var(--radius-md)", marginBottom: "1.5rem" }}>
            <div>
              <span style={{ fontSize: "0.85rem", opacity: 0.7, display: "block" }}>Stock Level</span>
              <span style={{ fontSize: "1.5rem", fontWeight: "bold" }}>{item.stock?.quantity || 0}</span>
            </div>
            <div style={{ textAlign: "right" }}>
              <span style={{ fontSize: "0.85rem", opacity: 0.7, display: "block" }}>Location</span>
              <span style={{ fontSize: "1.1rem", fontWeight: "500" }}>{item.rack?.locationCode || 'N/A'}</span>
            </div>
          </div>

          <div style={{ display: "flex", gap: "1rem" }}>
            <button className="btn btn-primary" style={{ flex: 1, backgroundColor: "var(--success)" }} onClick={() => handleAction('add')}>
              Add Stock (+1)
            </button>
            <button className="btn btn-primary" style={{ flex: 1, backgroundColor: "var(--danger)" }} onClick={() => handleAction('remove')}>
              Remove Stock (-1)
            </button>
          </div>

          <button className="btn btn-secondary" onClick={() => { setScanResult(null); setItem(null); setError(null); }} style={{ marginTop: "1rem", width: "100%" }}>
            Scan Another Item
          </button>
        </div>
      )}
    </div>
  );
}
