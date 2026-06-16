"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";

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

  useEffect(() => {
    fetchParts();
  }, []);

  const fetchWithAuth = async (path: string) => {
    const token = localStorage.getItem("token");
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
    const res = await fetch(`${apiUrl}${path}`, {
      headers: { "Authorization": `Bearer ${token}` }
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
    else if (step === "ITEMS") setStep("MODELS");
    else if (step === "MODELS") setStep("MANUFACTURERS");
    else if (step === "MANUFACTURERS") setStep("PARTS");
    else router.back();
  };

  return (
    <div className="container" style={{ paddingBottom: "5rem" }}>
      <div style={{ marginTop: "1rem", display: "flex", alignItems: "center", gap: "1rem", marginBottom: "1.5rem" }}>
        <button className="btn btn-secondary" style={{ padding: "0.5rem", minWidth: "44px" }} onClick={handleBack}>
          Back
        </button>
        <div>
          <h1 style={{ margin: 0, fontSize: "1.25rem", color: "var(--primary)" }}>Search Inventory</h1>
          <p style={{ margin: 0, fontSize: "0.75rem", opacity: 0.7 }}>
            {step === "PARTS" && "Select Part Type"}
            {step === "MANUFACTURERS" && `${selectedPart?.name} > Select Make`}
            {step === "MODELS" && `${selectedPart?.name} > ${selectedManufacturer?.name} > Select Model`}
            {step === "ITEMS" && `${selectedPart?.name} > ${selectedModel?.name} > Select Item`}
            {step === "DETAILS" && "Item Details"}
          </p>
        </div>
      </div>

      {error && (
        <div className="glass-panel" style={{ color: "var(--danger)", textAlign: "center", marginBottom: "1rem" }}>
          {error}
        </div>
      )}

      {loading && (
        <div style={{ textAlign: "center", padding: "2rem", opacity: 0.7 }}>Loading...</div>
      )}

      {!loading && step === "PARTS" && (
        <div style={{ display: "grid", gap: "0.75rem" }}>
          {parts.map(p => (
            <div key={p.partTypeId} className="glass-panel hover-panel" onClick={() => handleSelectPart(p)}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <span style={{ fontWeight: 600, fontSize: "1.1rem" }}>{p.name}</span>
                <div style={{ textAlign: "right" }}>
                  <span style={{ display: "block", fontSize: "0.85rem", opacity: 0.7 }}>{p.itemCount} models</span>
                  <span style={{ display: "block", fontSize: "0.85rem", color: "var(--primary)", fontWeight: 600 }}>{p.quantity} in stock</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {!loading && step === "MANUFACTURERS" && (
        <div style={{ display: "grid", gap: "0.75rem" }}>
          {manufacturers.map(m => (
            <div key={m.manufacturerId} className="glass-panel hover-panel" onClick={() => handleSelectManufacturer(m)}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <span style={{ fontWeight: 600, fontSize: "1.1rem" }}>{m.name}</span>
                <span style={{ fontSize: "0.85rem", opacity: 0.7 }}>{m.itemCount} items</span>
              </div>
            </div>
          ))}
        </div>
      )}

      {!loading && step === "MODELS" && (
        <div style={{ display: "grid", gap: "0.75rem" }}>
          {models.map(m => (
            <div key={m.modelId} className="glass-panel hover-panel" onClick={() => handleSelectModel(m)}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <div>
                  <span style={{ display: "block", fontWeight: 600, fontSize: "1.1rem" }}>{m.name}</span>
                  <span style={{ display: "block", fontSize: "0.85rem", opacity: 0.7 }}>Years: {m.yearRange}</span>
                </div>
                <span style={{ fontSize: "0.85rem", opacity: 0.7 }}>{m.itemCount} variants</span>
              </div>
            </div>
          ))}
        </div>
      )}

      {!loading && step === "ITEMS" && (
        <div style={{ display: "grid", gap: "0.75rem" }}>
          {items.map(i => (
            <div key={i.id} className="glass-panel hover-panel" onClick={() => handleSelectItem(i)}>
              <div style={{ marginBottom: "0.5rem" }}>
                <span style={{ display: "block", fontWeight: 600, fontSize: "1rem", color: "var(--foreground)" }}>{i.description}</span>
                <span style={{ display: "block", fontSize: "0.85rem", opacity: 0.7 }}>Brand: {i.brandName} | Barcode: {i.barcode}</span>
              </div>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: "0.85rem" }}>
                <span>Rack: <strong style={{ color: "var(--accent)" }}>{i.rackLocation}</strong></span>
                <span>Stock: <strong style={{ color: "var(--primary)", fontSize: "1.1rem" }}>{i.quantity}</strong></span>
              </div>
            </div>
          ))}
          {items.length === 0 && (
            <div style={{ textAlign: "center", padding: "2rem", opacity: 0.7 }}>No items found.</div>
          )}
        </div>
      )}

      {!loading && step === "DETAILS" && itemDetails && (
        <div className="glass-panel animate-slide-up">
          <h2 style={{ fontSize: "1.25rem", color: "var(--primary)" }}>{itemDetails.description}</h2>
          <p style={{ opacity: 0.8, marginBottom: "0.5rem" }}>{itemDetails.partType?.name} - {itemDetails.partBrand?.name}</p>
          <p style={{ opacity: 0.8, marginBottom: "1rem" }}>{itemDetails.vehicleModel?.manufacturer?.name} {itemDetails.vehicleModel?.name}</p>
          
          <div style={{ display: "flex", justifyContent: "space-between", background: "var(--surface-hover)", padding: "1rem", borderRadius: "var(--radius-md)", marginBottom: "1.5rem" }}>
            <div>
              <span style={{ fontSize: "0.85rem", opacity: 0.7, display: "block" }}>Stock Level</span>
              <span style={{ fontSize: "1.5rem", fontWeight: "bold" }}>{itemDetails.stock?.quantity || 0}</span>
            </div>
            <div style={{ textAlign: "right" }}>
              <span style={{ fontSize: "0.85rem", opacity: 0.7, display: "block" }}>Location</span>
              <span style={{ fontSize: "1.1rem", fontWeight: "500" }}>{itemDetails.rack?.locationCode || 'N/A'}</span>
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
            <button className="btn btn-primary" style={{ flex: 1 }} onClick={() => router.push(`/add-stock?barcode=${itemDetails.barcode}`)}>
              Add Stock
            </button>
            <button className="btn btn-accent" style={{ flex: 1 }} onClick={() => router.push(`/remove-stock?barcode=${itemDetails.barcode}`)}>
              Remove Stock
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
