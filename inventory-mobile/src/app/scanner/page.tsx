"use client";

import { useEffect, useState, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Html5QrcodeScanner } from "html5-qrcode";
import { getActiveApiUrl } from "@/lib/apiConfig";

function ScannerContent() {
  const [scanResult, setScanResult] = useState<string | null>(null);
  const [item, setItem] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [quantity, setQuantity] = useState(1);
  const router = useRouter();
  const searchParams = useSearchParams();
  const action = searchParams.get('action'); // 'add' or 'remove' or null

  const pageTitle = action === 'add' ? 'Add Stock' : action === 'remove' ? 'Remove Stock' : 'Scan Barcode';

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
      const apiUrl = await getActiveApiUrl();
      const res = await fetch(`${apiUrl}/api/stock/barcode/${barcode}`, {
        headers: { "Authorization": `Bearer ${token}` }
      });

      if (res.ok) {
        const data = await res.json();
        setItem(data);
        setQuantity(1); // reset quantity
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

  const handleAction = async (performAction: string) => {
    if (!item) return;
    try {
      const token = localStorage.getItem("token");
      const apiUrl = await getActiveApiUrl();
      const res = await fetch(`${apiUrl}/api/stock/${performAction}`, {
        method: "POST",
        headers: { 
          "Authorization": `Bearer ${token}`,
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ barcode: item.barcode, quantity: quantity })
      });

      if (res.ok) {
        alert(`Successfully ${performAction === 'add' ? 'added' : 'removed'} ${quantity} item(s)`);
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
      <div style={{ marginTop: "1rem", display: "flex", alignItems: "center", gap: "1rem", marginBottom: "1rem" }}>
        <button className="btn btn-secondary" style={{ padding: "0.5rem", minWidth: "44px" }} onClick={() => router.back()}>
          Back
        </button>
        <h1 style={{ margin: 0, fontSize: "1.5rem" }}>{pageTitle}</h1>
      </div>

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
              <span style={{ fontSize: "0.85rem", opacity: 0.7, display: "block" }}>Current Stock</span>
              <span style={{ fontSize: "1.5rem", fontWeight: "bold" }}>{item.stock?.quantity || 0}</span>
            </div>
            <div style={{ textAlign: "right" }}>
              <span style={{ fontSize: "0.85rem", opacity: 0.7, display: "block" }}>Location</span>
              <span style={{ fontSize: "1.1rem", fontWeight: "500" }}>{item.rack?.locationCode || 'N/A'}</span>
            </div>
          </div>

          <div style={{ marginBottom: "1rem" }}>
            <label style={{ display: "block", marginBottom: "0.5rem", fontSize: "0.9rem" }}>Quantity:</label>
            <input 
              type="number" 
              className="input-control" 
              value={quantity} 
              onChange={(e) => setQuantity(Math.max(1, parseInt(e.target.value) || 1))}
              min="1"
            />
          </div>

          <div style={{ display: "flex", gap: "1rem" }}>
            {(!action || action === 'add') && (
              <button className="btn btn-primary" style={{ flex: 1, backgroundColor: "var(--success)" }} onClick={() => handleAction('add')}>
                Add Stock
              </button>
            )}
            {(!action || action === 'remove') && (
              <button className="btn btn-primary" style={{ flex: 1, backgroundColor: "var(--danger)" }} onClick={() => handleAction('remove')}>
                Remove Stock
              </button>
            )}
          </div>

          <button className="btn btn-secondary" onClick={() => { setScanResult(null); setItem(null); setError(null); }} style={{ marginTop: "1rem", width: "100%" }}>
            Scan Another Item
          </button>
        </div>
      )}
    </div>
  );
}

export default function ScannerPage() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <ScannerContent />
    </Suspense>
  );
}
