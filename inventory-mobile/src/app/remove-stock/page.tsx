"use client";

import { useState, useEffect, useRef, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import BarcodeScanner from "@/components/BarcodeScanner";

function RemoveStockContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const initialBarcode = searchParams?.get("barcode") || "";

  const [barcode, setBarcode] = useState(initialBarcode);
  const [quantity, setQuantity] = useState<number | string>(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  
  const [showScanner, setShowScanner] = useState(false);
  
  const [item, setItem] = useState<any>(null);

  // Autocomplete state
  const [searchQuery, setSearchQuery] = useState("");
  const [suggestions, setSuggestions] = useState<any[]>([]);
  const autocompleteTimeout = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    if (initialBarcode) {
      handleLookup(initialBarcode);
    }
  }, [initialBarcode]);

  const fetchWithAuth = async (path: string, options: any = {}) => {
    const token = localStorage.getItem("token");
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
    const res = await fetch(`${apiUrl}${path}`, {
      cache: "no-store",
      ...options,
      headers: { 
        ...options.headers,
        "Authorization": `Bearer ${token}` 
      }
    });
    if (!res.ok) {
      if (res.status === 404) throw new Error("Item not found");
      throw new Error("Failed to process request");
    }
    return await res.json();
  };

  const handleAutocomplete = async (query: string) => {
    setSearchQuery(query);
    if (autocompleteTimeout.current) clearTimeout(autocompleteTimeout.current);
    
    if (query.trim().length < 2) {
      setSuggestions([]);
      return;
    }

    autocompleteTimeout.current = setTimeout(async () => {
      try {
        const data = await fetchWithAuth(`/api/search/autocomplete/${encodeURIComponent(query)}`);
        setSuggestions(data);
      } catch (err) {
        // silently ignore autocomplete errors
      }
    }, 300);
  };

  const selectSuggestion = (suggestion: any) => {
    setBarcode(suggestion.barcode);
    setSearchQuery("");
    setSuggestions([]);
    handleLookup(suggestion.barcode);
  };

  const handleLookup = async (lookupBarcode: string) => {
    if (!lookupBarcode.trim()) return;
    setLoading(true); setError(null); setSuccess(null);
    try {
      const data = await fetchWithAuth(`/api/stock/search/${encodeURIComponent(lookupBarcode)}`);
      setItem(data);
    } catch (err: any) {
      setError(err.message);
      setItem(null);
    } finally {
      setLoading(false);
    }
  };

  const handleRemoveStock = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!item) return;
    setLoading(true); setError(null); setSuccess(null);
    try {
      const result = await fetchWithAuth("/api/stock/remove", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ barcode: item.barcode, quantity, actionType: "REMOVE" })
      });
      alert(result.message || `Successfully removed ${quantity} from ${item.barcode}`);
      setBarcode("");
      handleLookup(item.barcode); // Refresh item details
      setQuantity(1);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container" style={{ paddingBottom: "5rem" }}>
      <div style={{ marginTop: "1rem", display: "flex", alignItems: "center", gap: "1rem", marginBottom: "1.5rem" }}>
        <button className="btn btn-secondary" style={{ padding: "0.5rem", minWidth: "44px" }} onClick={() => router.back()}>
          Back
        </button>
        <h1 style={{ margin: 0, fontSize: "1.5rem", color: "var(--danger)" }}>Remove Stock</h1>
      </div>

      <div className="glass-panel" style={{ marginBottom: "1.5rem", position: "relative" }}>
        <div className="input-group">
          <label>Search Item</label>
          <input 
            type="text" 
            className="input-control" 
            placeholder="Type barcode or name to search..." 
            value={searchQuery}
            onChange={(e) => handleAutocomplete(e.target.value)}
          />
        </div>
        {suggestions.length > 0 && (
          <div style={{ 
            position: "absolute", top: "100%", left: 0, right: 0, 
            background: "var(--surface)", border: "1px solid var(--border)", 
            borderRadius: "var(--radius-sm)", zIndex: 10, maxHeight: "200px", overflowY: "auto",
            boxShadow: "var(--shadow-md)", marginTop: "4px"
          }}>
            {suggestions.map((s, idx) => (
              <div 
                key={idx} 
                className="hover-panel"
                style={{ padding: "0.75rem", borderBottom: "1px solid var(--border)" }}
                onClick={() => selectSuggestion(s)}
              >
                <span style={{ fontWeight: "bold", display: "block" }}>{s.barcode}</span>
                <span style={{ fontSize: "0.85rem", opacity: 0.7 }}>{s.description}</span>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="glass-panel" style={{ marginBottom: "1.5rem" }}>
        <div className="input-group">
          <label>Scan / Enter Barcode</label>
          <div style={{ display: "flex", gap: "0.5rem" }}>
            <input 
              type="text" 
              className="input-control" 
              style={{ flex: 1 }}
              value={barcode}
              onChange={(e) => setBarcode(e.target.value)}
              onKeyDown={(e) => { if (e.key === "Enter") handleLookup(barcode); }}
            />
            <button className="btn btn-secondary" onClick={() => setShowScanner(true)}>
              📷
            </button>
            <button className="btn btn-secondary" onClick={() => handleLookup(barcode)}>Lookup</button>
          </div>
        </div>
      </div>

      {showScanner && (
        <BarcodeScanner 
          onResult={(result: string) => {
            setBarcode(result);
            setShowScanner(false);
            handleLookup(result);
          }}
          onClose={() => setShowScanner(false)}
        />
      )}

      {error && <div className="glass-panel" style={{ color: "var(--danger)", textAlign: "center", marginBottom: "1rem" }}>{error}</div>}
      {success && <div className="glass-panel" style={{ color: "var(--success)", textAlign: "center", marginBottom: "1rem" }}>{success}</div>}

      {item && (
        <form onSubmit={handleRemoveStock} className="glass-panel animate-slide-up" style={{ padding: "1.5rem", border: "1px solid var(--border)", borderTop: "4px solid var(--danger)", background: "var(--surface)", borderRadius: "var(--radius-lg)" }}>
          <h2 style={{ fontSize: "1.25rem", color: "var(--primary)", marginBottom: "0.25rem" }}>{item?.vehicleModel?.manufacturer?.name} {item?.vehicleModel?.name}</h2>
          <p style={{ color: "var(--text-secondary)", marginBottom: "0.25rem", fontSize: "0.95rem" }}>{item?.partType?.name} - {item?.partBrand?.name}</p>
          <p style={{ color: "var(--text-secondary)", marginBottom: "1.25rem", fontSize: "0.95rem" }}>{item.description}</p>
          
          <div style={{ display: "flex", justifyContent: "space-between", background: "var(--bg-main)", border: "1px solid var(--border)", padding: "1rem", borderRadius: "var(--radius-md)", marginBottom: "1.5rem" }}>
            <div>
              <span style={{ fontSize: "0.85rem", color: "var(--text-secondary)", display: "block" }}>Current Stock</span>
              <span style={{ fontSize: "1.5rem", fontWeight: "700", color: "var(--primary)" }}>{item.stock?.quantity || 0}</span>
            </div>
            <div style={{ textAlign: "right" }}>
              <span style={{ fontSize: "0.85rem", color: "var(--text-secondary)", display: "block" }}>Location</span>
              <span style={{ fontSize: "1.1rem", fontWeight: "600", color: "var(--text-primary)" }}>{item.rack?.locationCode || 'N/A'}</span>
            </div>
          </div>
          
          <div className="data-row">
            <span className="data-label">Barcode</span>
            <span className="data-value">{item.barcode}</span>
          </div>
          <div className="data-row">
            <span className="data-label">Pcode</span>
            <span className="data-value">{item.secretPriceCode || "-"}</span>
          </div>
          <div className="data-row">
            <span className="data-label">Registered Date</span>
            <span className="data-value">{item.registeredDate ? new Date(item.registeredDate).toLocaleDateString() : "-"}</span>
          </div>

          <div style={{ marginTop: "1.5rem" }}>
            <div className="input-group">
              <label>Quantity to Remove</label>
              <input 
                type="number" 
                className="input-control" 
                value={quantity}
                onChange={(e) => {
                  const val = e.target.value;
                  if (val === '') setQuantity('');
                  else {
                    const num = Number(val);
                    if (num >= 1) setQuantity(num);
                  }
                }}
                onKeyDown={(e) => {
                  if (['-', '+', 'e', 'E', '.'].includes(e.key)) {
                    e.preventDefault();
                  }
                }}
                min="1"
                required
              />
            </div>

            <button type="submit" className="btn btn-danger" style={{ width: "100%", marginTop: "1rem", backgroundColor: "var(--danger)", color: "white" }} disabled={loading}>
              {loading ? "Processing..." : "Remove Stock"}
            </button>
          </div>
        </form>
      )}
    </div>
  );
}

export default function RemoveStockPage() {
  return (
    <Suspense fallback={<div style={{ padding: "2rem", textAlign: "center" }}>Loading...</div>}>
      <RemoveStockContent />
    </Suspense>
  );
}
