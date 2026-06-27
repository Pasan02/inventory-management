"use client";

import { useState, useEffect, useRef, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import BarcodeScanner from "@/components/BarcodeScanner";
import { getActiveApiUrl } from "@/lib/apiConfig";

function AddStockContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const initialBarcode = searchParams?.get("barcode") || "";
  const initialQuantity = searchParams?.get("quantity") ? parseInt(searchParams.get("quantity")!) : 1;
  const initialOrderIds = searchParams?.get("orderIds") || "";

  const [barcode, setBarcode] = useState(initialBarcode);
  const [quantity, setQuantity] = useState<number | string>(initialQuantity);
  const [orderIds, setOrderIds] = useState(initialOrderIds);
  const [pcode, setPcode] = useState("");
  const [printQuantity, setPrintQuantity] = useState<number | string>(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  
  const [showScanner, setShowScanner] = useState(false);
  
  const [item, setItem] = useState<any>(null);

  // Autocomplete state
  const [searchQuery, setSearchQuery] = useState("");
  const [suggestions, setSuggestions] = useState<any[]>([]);
  const autocompleteTimeout = useRef<NodeJS.Timeout | null>(null);
  const autocompleteAbortController = useRef<AbortController | null>(null);

  useEffect(() => {
    return () => {
      if (autocompleteTimeout.current) clearTimeout(autocompleteTimeout.current);
      if (autocompleteAbortController.current) autocompleteAbortController.current.abort();
    };
  }, []);

  useEffect(() => {
    if (initialBarcode) {
      handleLookup(initialBarcode);
    }
  }, [initialBarcode]);

  const fetchWithAuth = async (path: string, options: any = {}) => {
    const token = localStorage.getItem("token");
    const apiUrl = await getActiveApiUrl();
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
    
    if (autocompleteAbortController.current) {
      autocompleteAbortController.current.abort();
    }
    
    if (query.trim().length < 2) {
      setSuggestions([]);
      return;
    }

    autocompleteTimeout.current = setTimeout(async () => {
      autocompleteAbortController.current = new AbortController();
      try {
        const data = await fetchWithAuth(`/api/search/autocomplete/${encodeURIComponent(query)}`, {
          signal: autocompleteAbortController.current.signal
        });
        setSuggestions(data);
      } catch (err: any) {
        if (err.name !== 'AbortError') {
          // silently ignore autocomplete errors
        }
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
      setPcode(data.secretPriceCode || "");
    } catch (err: any) {
      setError(err.message);
      setItem(null);
    } finally {
      setLoading(false);
    }
  };

  const handleAddStock = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!item) return;
    setLoading(true); setError(null); setSuccess(null);
    try {
      let result;
      if (pcode.trim()) {
        result = await fetchWithAuth("/api/stock/add-with-price", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ barcode: item.barcode, quantity, secretPriceCode: pcode.trim() })
        });
      } else {
        result = await fetchWithAuth("/api/stock/add", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ barcode: item.barcode, quantity })
        });
      }

      if (result.success) {
        if (result.newBarcode) {
          // PCode changed, new barcode generated
          try {
            await fetchWithAuth("/api/print/barcode", {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({ barcode: result.newBarcode, copies: quantity })
            });
            alert(`${result.message}\n\nSuccessfully printed ${quantity} barcode label(s) for the new item.`);
          } catch (e) {
            alert(`${result.message}\n\nFailed to automatically print barcode labels. Please check your Zebra printer connection.`);
          }
          
          setBarcode(result.newBarcode);
          handleLookup(result.newBarcode);
        } else {
          // Standard update
          alert(result.message || `Successfully added ${quantity} to ${item.barcode}`);
          setBarcode("");
          handleLookup(item.barcode);
        }
        
        // Clear pending orders if any
        if (orderIds) {
          const ids = orderIds.split(',').filter(id => id.trim() !== "");
          for (const id of ids) {
            try {
              await fetchWithAuth(`/api/stock/orders/${id}/arrive`, { method: "POST" });
            } catch (err) {
              console.error("Failed to mark order as arrived", id, err);
            }
          }
          setOrderIds("");
        }

        setQuantity(1);
      } else {
        setError(result.message || "Failed to add stock.");
      }
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handlePrintBarcode = async () => {
    if (!item) return;
    try {
      await fetchWithAuth("/api/print/barcode", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ barcode: item.barcode, copies: printQuantity })
      });
      alert(`Print job for ${printQuantity} label(s) sent to local printer successfully!`);
    } catch (e) {
      alert("Error printing: " + (e as Error).message);
    }
  };

  return (
    <div className="container" style={{ paddingBottom: "5rem" }}>
      <div style={{ marginTop: "1rem", display: "flex", alignItems: "center", gap: "1rem", marginBottom: "1.5rem" }}>
        <button className="btn btn-secondary" style={{ padding: "0.5rem", minWidth: "44px" }} onClick={() => router.back()}>
          Back
        </button>
        <h1 style={{ margin: 0, fontSize: "1.5rem", color: "var(--primary)" }}>Add Stock</h1>
      </div>

      {orderIds && (
        <div className="glass-panel slide-in" style={{ borderLeft: "4px solid var(--warning)", marginBottom: "1.5rem" }}>
          <p style={{ margin: 0, color: "var(--warning)", fontWeight: "bold" }}>
            Fulfilling Order ({orderIds.split(',').length} items)
          </p>
          <p style={{ margin: "0.5rem 0 0 0", fontSize: "0.85rem", color: "var(--text-secondary)" }}>
            Adding this stock will automatically mark these items as arrived.
          </p>
        </div>
      )}

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
        <form onSubmit={handleAddStock} className="glass-panel animate-slide-up" style={{ padding: "1.5rem", border: "1px solid var(--border)", background: "var(--surface)", borderRadius: "var(--radius-lg)" }}>
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



          <div style={{ marginTop: "1.5rem" }}>
            <div className="input-group">
              <label>Quantity to Add</label>
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
            
            <div className="input-group">
              <label>Pcode</label>
              <input 
                type="text" 
                className="input-control" 
                value={pcode}
                onChange={(e) => setPcode(e.target.value)}
              />
            </div>

            <button type="submit" className="btn btn-primary" style={{ width: "100%", marginTop: "1rem" }} disabled={loading}>
              {loading ? "Processing..." : "Add Stock"}
            </button>
          </div>
        </form>
      )}

      {item && (
        <div className="glass-panel animate-slide-up" style={{ marginTop: "1.5rem", padding: "1.5rem", border: "1px solid var(--border)", background: "var(--surface)", borderRadius: "var(--radius-lg)" }}>
          <h3 style={{ fontSize: "1.1rem", color: "var(--text-primary)", marginBottom: "1rem", marginTop: 0 }}>Print Labels</h3>
          <div style={{ display: "flex", gap: "0.5rem", alignItems: "flex-end" }}>
            <div className="input-group" style={{ flex: 1, marginBottom: 0 }}>
              <label>Quantity</label>
              <input 
                type="number" 
                className="input-control" 
                value={printQuantity}
                onChange={(e) => {
                  const val = e.target.value;
                  if (val === '') setPrintQuantity('');
                  else {
                    const num = Number(val);
                    if (num >= 1) setPrintQuantity(num);
                  }
                }}
                onKeyDown={(e) => {
                  if (['-', '+', 'e', 'E', '.'].includes(e.key)) {
                    e.preventDefault();
                  }
                }}
                min="1"
              />
            </div>
            <button 
              type="button" 
              className="btn btn-secondary" 
              style={{ flex: 2, padding: "0.75rem", fontSize: "0.95rem" }} 
              onClick={handlePrintBarcode}
            >
              🖨️ Print Barcode
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

export default function AddStockPage() {
  return (
    <Suspense fallback={<div style={{ padding: "2rem", textAlign: "center" }}>Loading...</div>}>
      <AddStockContent />
    </Suspense>
  );
}
