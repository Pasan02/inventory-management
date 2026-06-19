"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import BarcodeScanner from "@/components/BarcodeScanner";

type Step = "PARTS" | "MANUFACTURERS" | "MODELS" | "ITEMS" | "DETAILS";

export default function SearchPage() {
  const router = useRouter();
  
  const [step, setStep] = useState<Step>("PARTS");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Data state
  const [parts, setParts] = useState<any[]>([]);
  const [manufacturers, setManufacturers] = useState<any[]>([]);
  const [models, setModels] = useState<any[]>([]);
  const [items, setItems] = useState<any[]>([]);
  const [itemDetails, setItemDetails] = useState<any>(null);

  // Selection state
  const [selectedPart, setSelectedPart] = useState<any>(null);
  const [selectedManufacturer, setSelectedManufacturer] = useState<any>(null);
  const [selectedModel, setSelectedModel] = useState<any>(null);

  // Filter state
  const [filterText, setFilterText] = useState("");
  const [includeOos, setIncludeOos] = useState(false);
  const [quickBarcode, setQuickBarcode] = useState("");
  const [showScanner, setShowScanner] = useState(false);

  useEffect(() => {
    fetchParts();
  }, []);

  const fetchWithAuth = async (path: string, options: any = {}) => {
    const token = localStorage.getItem("token");
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
    const res = await fetch(`${apiUrl}${path}`, {
      ...options,
      headers: {
        ...options.headers,
        "Authorization": `Bearer ${token}`
      }
    });
    if (!res.ok) throw new Error("Failed to fetch data");
    return await res.json();
  };

  const fetchParts = async () => {
    setLoading(true); setError(null);
    try {
      const data = await fetchWithAuth("/api/search/parts");
      setParts(data);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectPart = async (part: any) => {
    setSelectedPart(part);
    setFilterText("");
    setStep("MANUFACTURERS");
    setLoading(true); setError(null);
    try {
      const data = await fetchWithAuth(`/api/search/manufacturers/${part.partTypeId}`);
      setManufacturers(data);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectManufacturer = async (manufacturer: any) => {
    setSelectedManufacturer(manufacturer);
    setFilterText("");
    setStep("MODELS");
    setLoading(true); setError(null);
    try {
      const data = await fetchWithAuth(`/api/search/models/${selectedPart.partTypeId}/${manufacturer.manufacturerId}`);
      setModels(data);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectModel = async (model: any) => {
    setSelectedModel(model);
    setFilterText("");
    setStep("ITEMS");
    setLoading(true); setError(null);
    try {
      const data = await fetchWithAuth(`/api/search/items/${model.modelId}?partTypeId=${selectedPart.partTypeId}`);
      setItems(data);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectPartAllItems = async () => {
    setFilterText("");
    setStep("ITEMS");
    setSelectedModel({ name: "All " + selectedPart.name });
    setLoading(true); setError(null);
    try {
      const data = await fetchWithAuth(`/api/search/items/part/${selectedPart.partTypeId}`);
      setItems(data);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectManufacturerAllItems = async () => {
    setFilterText("");
    setStep("ITEMS");
    setSelectedModel({ name: "All " + selectedManufacturer.name });
    setLoading(true); setError(null);
    try {
      const data = await fetchWithAuth(`/api/search/items/manufacturer/${selectedManufacturer.manufacturerId}?partTypeId=${selectedPart.partTypeId}`);
      setItems(data);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectItem = async (item: any) => {
    setStep("DETAILS");
    setLoading(true); setError(null);
    try {
      const data = await fetchWithAuth(`/api/stock/search/${item.barcode}`);
      setItemDetails(data);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleBack = () => {
    if (step === "DETAILS") setStep("ITEMS");
    else if (step === "ITEMS") { setStep("MODELS"); setFilterText(""); }
    else if (step === "MODELS") { setStep("MANUFACTURERS"); setFilterText(""); }
    else if (step === "MANUFACTURERS") { setStep("PARTS"); setFilterText(""); }
    else router.back();
  };

  const handlePrintBarcode = async (barcode: string) => {
    try {
      await fetchWithAuth("/api/print/barcode", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ barcode, copies: 1 })
      });
      alert("Print job sent to local printer successfully!");
    } catch (e: any) {
      alert("Error printing: " + e.message);
    }
  };

  return (
    <div className="container" style={{ paddingBottom: "5rem" }}>
      <div style={{ marginTop: "1rem", display: "flex", alignItems: "center", gap: "1rem", marginBottom: "1.5rem" }}>
        <button className="btn btn-secondary" style={{ padding: "0.5rem", minWidth: "44px" }} onClick={handleBack}>
          Back
        </button>
        <h1 style={{ margin: 0, fontSize: "1.5rem", color: "var(--primary)" }}>Search / Items</h1>
      </div>

      <div className="glass-panel" style={{ padding: "1.25rem", marginBottom: "1rem" }}>
        {step === "PARTS" && (
          <>
            <div className="input-group" style={{ marginBottom: "1.5rem" }}>
              <label style={{ fontSize: "0.95rem", fontWeight: "600", color: "var(--text-secondary)", display: "block", marginBottom: "0.5rem" }}>Quick Barcode Scan / Lookup</label>
              <div style={{ display: "flex", gap: "0.5rem" }}>
                <input type="text" className="input-control" style={{ flex: 1 }} placeholder="Enter barcode..." value={quickBarcode} onChange={e => setQuickBarcode(e.target.value)} onKeyDown={e => { if(e.key === "Enter") handleSelectItem({ barcode: quickBarcode }); }} />
                <button className="btn btn-secondary" onClick={() => setShowScanner(true)}>📷</button>
                <button className="btn btn-primary" onClick={() => handleSelectItem({ barcode: quickBarcode })}>Lookup</button>
              </div>
            </div>
            <h2 style={{ fontSize: "1.2rem", fontWeight: "700", color: "var(--text-primary)", marginBottom: "1rem" }}>Select Part Type</h2>
          </>
        )}
        {step === "MANUFACTURERS" && (
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "1rem" }}>
            <h2 style={{ fontSize: "1.2rem", fontWeight: "700", color: "var(--text-primary)", margin: 0 }}>Select Manufacturer</h2>
            <button className="btn btn-primary" style={{ padding: "0.35rem 0.75rem", fontSize: "0.85rem", fontWeight: "600", borderRadius: "6px" }} onClick={handleSelectPartAllItems}>View All Items</button>
          </div>
        )}
        {step === "MODELS" && (
          <>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "1rem" }}>
              <h2 style={{ fontSize: "1.2rem", fontWeight: "700", color: "var(--text-primary)", margin: 0 }}>Select Model for {selectedManufacturer?.name}</h2>
              <button className="btn btn-primary" style={{ padding: "0.35rem 0.75rem", fontSize: "0.85rem", fontWeight: "600", borderRadius: "6px" }} onClick={handleSelectManufacturerAllItems}>View All Items</button>
            </div>
            <div className="input-group" style={{ marginBottom: "1rem" }}>
              <input type="text" className="input-control" placeholder="Search models by name..." value={filterText} onChange={e => setFilterText(e.target.value)} />
            </div>
          </>
        )}
        {step === "ITEMS" && (
          <>
            <h2 style={{ fontSize: "1.2rem", fontWeight: "700", color: "var(--text-primary)", marginBottom: "1rem" }}>All Items for {selectedModel?.name}</h2>
            <div className="input-group" style={{ marginBottom: "1rem" }}>
              <input type="text" className="input-control" placeholder="Search items by brand, description..." value={filterText} onChange={e => setFilterText(e.target.value)} />
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: "0.5rem", marginBottom: "1rem" }}>
              <input type="checkbox" id="include-oos" checked={includeOos} onChange={e => setIncludeOos(e.target.checked)} />
              <label htmlFor="include-oos" style={{ fontWeight: "600", fontSize: "0.95rem", color: "var(--text-secondary)" }}>Include Out-of-Stock</label>
            </div>
          </>
        )}
        {step === "DETAILS" && (
          <h2 style={{ fontSize: "1.2rem", fontWeight: "700", color: "var(--text-primary)", marginBottom: "1rem" }}>Item Details</h2>
        )}

        {error && (
          <div style={{ color: "var(--danger)", textAlign: "center", marginBottom: "1rem", padding: "1rem", border: "1px solid var(--danger)", borderRadius: "var(--radius-md)" }}>
            {error}
          </div>
        )}

        {loading && (
          <div style={{ textAlign: "center", padding: "2rem", opacity: 0.7 }}>Loading data...</div>
        )}

        {!loading && step === "PARTS" && (
          <div style={{ display: "grid", gap: "0.75rem" }}>
            {parts.map(p => (
              <div key={p.partTypeId} style={{ padding: "1rem", border: "1px solid var(--border)", borderRadius: "var(--radius-md)", cursor: "pointer", background: "var(--surface)" }} className="hover-panel" onClick={() => handleSelectPart(p)}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <div style={{ display: "flex", alignItems: "center", gap: "1rem" }}>
                    {p.imageUrl && <img src={`${process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000"}${p.imageUrl}`} alt={p.name} style={{ width: "48px", height: "48px", objectFit: "contain" }} />}
                    <span style={{ fontWeight: 600, fontSize: "1.1rem" }}>{p.name}</span>
                  </div>
                  <div style={{ textAlign: "right" }}>
                    <span style={{ display: "block", fontSize: "0.85rem", opacity: 0.7 }}>{p.itemCount} models</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {!loading && step === "MANUFACTURERS" && (
          <div style={{ display: "grid", gap: "0.75rem" }}>
            {manufacturers.map(m => (
              <div key={m.manufacturerId} style={{ padding: "1rem", border: "1px solid var(--border)", borderRadius: "var(--radius-md)", cursor: "pointer", background: "var(--surface)" }} className="hover-panel" onClick={() => handleSelectManufacturer(m)}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <div style={{ display: "flex", alignItems: "center", gap: "1rem" }}>
                    {m.logoUrl && <img src={`${process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000"}${m.logoUrl}`} alt={m.name} style={{ width: "48px", height: "48px", objectFit: "contain" }} />}
                    <span style={{ fontWeight: 600, fontSize: "1.1rem" }}>{m.name}</span>
                  </div>
                  <span style={{ fontSize: "0.85rem", opacity: 0.7 }}>{m.itemCount} items</span>
                </div>
              </div>
            ))}
          </div>
        )}

        {!loading && step === "MODELS" && (
          <div style={{ display: "grid", gap: "0.75rem" }}>
            {models.filter(m => m.name.toLowerCase().includes(filterText.toLowerCase())).map(m => (
              <div key={m.modelId} style={{ padding: "1rem", border: "1px solid var(--border)", borderRadius: "var(--radius-md)", cursor: "pointer", background: "var(--surface)" }} className="hover-panel" onClick={() => handleSelectModel(m)}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <div style={{ display: "flex", alignItems: "center", gap: "1rem" }}>
                    {m.imageUrl && <img src={`${process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000"}${m.imageUrl}`} alt={m.name} style={{ width: "48px", height: "48px", objectFit: "cover", borderRadius: "4px", border: "1px solid var(--border)" }} />}
                    <div>
                      <span style={{ display: "block", fontWeight: 600, fontSize: "1.1rem" }}>{m.name}</span>
                      <span style={{ display: "block", fontSize: "0.85rem", opacity: 0.7 }}>Years: {m.yearRange}</span>
                    </div>
                  </div>
                  <div style={{ textAlign: "right" }}>
                    <span style={{ display: "block", fontSize: "0.85rem", opacity: 0.7 }}>{m.itemCount} variants</span>
                    {m.quantity > 0 ? (
                      <span style={{ display: "block", fontSize: "0.85rem", color: "var(--primary)", fontWeight: 600 }}>{m.quantity} in stock</span>
                    ) : (
                      <span style={{ display: "block", fontSize: "0.85rem", color: "var(--danger)", fontWeight: 600 }}>Out of stock</span>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {!loading && step === "ITEMS" && (
          <div style={{ display: "grid", gap: "0.75rem" }}>
            {items
              .filter(i => includeOos || i.quantity > 0)
              .filter(i => i.description.toLowerCase().includes(filterText.toLowerCase()) || i.barcode.toLowerCase().includes(filterText.toLowerCase()))
              .map(i => (
              <div key={i.id} style={{ padding: "1rem", border: "1px solid var(--border)", borderRadius: "var(--radius-md)", background: "var(--surface)" }}>
                <div style={{ marginBottom: "0.5rem" }}>
                  <span style={{ display: "block", fontWeight: 600, fontSize: "1.1rem", color: "var(--text-primary)" }}>{i.description}</span>
                  <span style={{ display: "block", fontSize: "0.85rem", color: "var(--text-secondary)", marginTop: "4px" }}>
                    <strong>Brand:</strong> {i.brandName} | <strong>Barcode:</strong> {i.barcode}
                  </span>
                </div>
                <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.5rem", fontSize: "0.85rem", marginBottom: "1rem", color: "var(--text-secondary)" }}>
                  <span><strong>Origin:</strong> <span style={{ color: "var(--text-primary)" }}>{i.countryOfOrigin || "-"}</span></span>
                  <span><strong>Pcode:</strong> <span style={{ color: "var(--text-primary)" }}>{i.secretPriceCode || "-"}</span></span>
                  <span><strong>Registered:</strong> <span style={{ color: "var(--text-primary)" }}>{i.registeredDate ? new Date(i.registeredDate).toLocaleDateString() : "-"}</span></span>
                  <span><strong>Rack:</strong> <span style={{ color: "var(--text-primary)" }}>{i.rackLocation}</span></span>
                  <span style={{ gridColumn: "span 2", borderTop: "1px solid var(--border)", paddingTop: "0.5rem", marginTop: "0.25rem" }}>
                    Stock: <strong style={{ color: "var(--primary)", fontSize: "1.1rem" }}>{i.quantity}</strong>
                  </span>
                </div>
                <div style={{ display: "flex", gap: "0.5rem" }}>
                  <button className="btn btn-primary" style={{ flex: 1, padding: "0.6rem", fontSize: "0.9rem", fontWeight: 600 }} onClick={() => handlePrintBarcode(i.barcode)}>
                    Print Barcode Label
                  </button>
                </div>
              </div>
            ))}
            {items.length === 0 && (
              <div style={{ textAlign: "center", padding: "2rem", opacity: 0.7 }}>No items found.</div>
            )}
          </div>
        )}

        {!loading && step === "DETAILS" && itemDetails && (
          <div className="animate-slide-up" style={{ padding: "1rem", border: "1px solid var(--border)", borderRadius: "var(--radius-md)", background: "var(--surface)" }}>
            <h2 style={{ fontSize: "1.25rem", color: "var(--primary)", marginBottom: "0.25rem" }}>{itemDetails.description}</h2>
            <p style={{ color: "var(--text-secondary)", marginBottom: "0.25rem", fontSize: "0.95rem" }}>{itemDetails.partType?.name} - {itemDetails.partBrand?.name}</p>
            <p style={{ color: "var(--text-secondary)", marginBottom: "1.25rem", fontSize: "0.95rem" }}>{itemDetails.vehicleModel?.manufacturer?.name} {itemDetails.vehicleModel?.name}</p>
            
            <div style={{ display: "flex", justifyContent: "space-between", background: "var(--bg-main)", border: "1px solid var(--border)", padding: "1rem", borderRadius: "var(--radius-md)", marginBottom: "1.5rem" }}>
              <div>
                <span style={{ fontSize: "0.85rem", color: "var(--text-secondary)", display: "block" }}>Stock Level</span>
                <span style={{ fontSize: "1.5rem", fontWeight: "700", color: "var(--primary)" }}>{itemDetails.stock?.quantity || 0}</span>
              </div>
              <div style={{ textAlign: "right" }}>
                <span style={{ fontSize: "0.85rem", color: "var(--text-secondary)", display: "block" }}>Location</span>
                <span style={{ fontSize: "1.1rem", fontWeight: "600", color: "var(--text-primary)" }}>{itemDetails.rack?.locationCode || 'N/A'}</span>
              </div>
            </div>
            
            <div className="data-row">
              <span className="data-label">Barcode</span>
              <span className="data-value">{itemDetails.barcode}</span>
            </div>
            <div className="data-row">
              <span className="data-label">Country of Origin</span>
              <span className="data-value">{itemDetails.countryOfOrigin}</span>
            </div>
            <div className="data-row">
              <span className="data-label">Threshold</span>
              <span className="data-value">{itemDetails.lowStockThreshold}</span>
            </div>
            <div className="data-row">
              <span className="data-label">Pcode</span>
              <span className="data-value">{itemDetails.secretPriceCode}</span>
            </div>

            <div style={{ display: "flex", gap: "1rem", marginTop: "1.5rem" }}>
              <button className="btn btn-primary" style={{ flex: 1, padding: "0.75rem" }} onClick={() => router.push(`/add-stock?barcode=${itemDetails.barcode}`)}>
                Add Stock
              </button>
              <button className="btn btn-secondary" style={{ flex: 1, padding: "0.75rem", border: "2px solid var(--danger)", color: "var(--danger)" }} onClick={() => router.push(`/remove-stock?barcode=${itemDetails.barcode}`)}>
                Remove
              </button>
            </div>
          </div>
        )}
      </div>

      {showScanner && (
        <BarcodeScanner 
          onResult={(result: string) => {
            setQuickBarcode(result);
            setShowScanner(false);
            handleSelectItem({ barcode: result });
          }}
          onClose={() => setShowScanner(false)}
        />
      )}
    </div>
  );
}
