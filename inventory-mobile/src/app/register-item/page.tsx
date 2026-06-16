"use client";

import { useState, useEffect, useRef } from "react";
import { useRouter } from "next/navigation";

export default function RegisterItemPage() {
  const router = useRouter();
  
  const [step, setStep] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Reference Data
  const [partTypes, setPartTypes] = useState<any[]>([]);
  const [brands, setBrands] = useState<any[]>([]);
  const [manufacturers, setManufacturers] = useState<any[]>([]);
  const [racks, setRacks] = useState<any[]>([]);
  const [models, setModels] = useState<any[]>([]);

  // Form State
  const [partTypeId, setPartTypeId] = useState<number | "">("");
  const [brandId, setBrandId] = useState<number | "">("");
  const [manufacturerId, setManufacturerId] = useState<number | "">("");
  const [modelId, setModelId] = useState<number | "">("");
  const [rackId, setRackId] = useState<number | "">("");
  
  const [description, setDescription] = useState("");
  const [countryOfOrigin, setCountryOfOrigin] = useState("");
  const [lowStockThreshold, setLowStockThreshold] = useState("5");
  const [customBarcode, setCustomBarcode] = useState("");
  const [secretPriceCode, setSecretPriceCode] = useState("");

  const [compatibleModels, setCompatibleModels] = useState<any[]>([]);
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);

  // Modals state
  const [showModal, setShowModal] = useState<string | null>(null);
  const [modalInput1, setModalInput1] = useState("");
  const [modalInput2, setModalInput2] = useState(""); // For year range or other extra field

  useEffect(() => {
    fetchReferenceData();
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
    if (!res.ok) {
      const data = await res.json().catch(() => null);
      throw new Error(data?.message || "Failed to process request");
    }
    return await res.json();
  };

  const fetchReferenceData = async () => {
    try {
      const data = await fetchWithAuth("/api/items/reference-data");
      setPartTypes(data.partTypes);
      setBrands(data.brands);
      setManufacturers(data.manufacturers);
      setRacks(data.racks);
    } catch (err: any) {
      setError(err.message);
    }
  };

  const fetchModelsForManufacturer = async (mId: number) => {
    try {
      const data = await fetchWithAuth(`/api/items/models/${mId}`);
      setModels(data);
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleManufacturerChange = (e: any) => {
    const mId = parseInt(e.target.value);
    setManufacturerId(mId);
    setModelId("");
    if (!isNaN(mId)) {
      fetchModelsForManufacturer(mId);
    } else {
      setModels([]);
    }
  };

  const handleImageCapture = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      setImageFile(file);
      const reader = new FileReader();
      reader.onload = (e) => {
        setImagePreview(e.target?.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleCreateReferenceData = async () => {
    if (!showModal) return;
    setLoading(true);
    setError(null);
    try {
      let endpoint = "";
      let payload: any = { name: modalInput1 };
      
      if (showModal === "part-type") endpoint = "/api/items/reference-data/part-types";
      if (showModal === "brand") endpoint = "/api/items/reference-data/brands";
      if (showModal === "manufacturer") endpoint = "/api/items/reference-data/manufacturers";
      if (showModal === "rack") {
        endpoint = "/api/items/reference-data/racks";
        payload = { locationCode: modalInput1 };
      }
      if (showModal === "model") {
        if (!manufacturerId) throw new Error("Select a manufacturer first");
        endpoint = "/api/items/reference-data/models";
        payload = { name: modalInput1, manufacturerId: manufacturerId, yearRange: modalInput2 };
      }

      const res = await fetchWithAuth(endpoint, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });

      if (showModal === "part-type") {
        setPartTypes([...partTypes, res]);
        setPartTypeId(res.id);
      }
      if (showModal === "brand") {
        setBrands([...brands, res]);
        setBrandId(res.id);
      }
      if (showModal === "manufacturer") {
        setManufacturers([...manufacturers, res]);
        setManufacturerId(res.id);
        fetchModelsForManufacturer(res.id);
      }
      if (showModal === "rack") {
        setRacks([...racks, res]);
        setRackId(res.id);
      }
      if (showModal === "model") {
        setModels([...models, res]);
        setModelId(res.id);
      }

      setShowModal(null);
      setModalInput1("");
      setModalInput2("");
    } catch (err: any) {
      alert("Error: " + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async () => {
    if (!partTypeId || !brandId || !manufacturerId || !modelId) {
      alert("Please fill in all required classification fields.");
      return;
    }
    
    setLoading(true);
    setError(null);
    try {
      const payload = {
        partTypeId,
        partBrandId: brandId,
        vehicleModelId: modelId,
        countryOfOrigin: countryOfOrigin.trim() || "N/A",
        description: description.trim() || "N/A",
        lowStockThreshold: parseInt(lowStockThreshold) || 0,
        rackId: rackId || null,
        customBarcode,
        secretPriceCode,
        compatibleModels
      };

      const res = await fetchWithAuth("/api/items", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });

      const generatedBarcode = res.barcode;

      if (imageFile) {
        const formData = new FormData();
        formData.append("image", imageFile);
        
        const token = localStorage.getItem("token");
        const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";
        await fetch(`${apiUrl}/api/items/${encodeURIComponent(generatedBarcode)}/image`, {
          method: "POST",
          headers: { "Authorization": `Bearer ${token}` },
          body: formData
        });
      }

      alert(`Item Saved Successfully!\nBarcode: ${generatedBarcode}`);
      router.push("/dashboard");
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container" style={{ paddingBottom: "5rem" }}>
      <div style={{ marginTop: "1rem", display: "flex", alignItems: "center", gap: "1rem", marginBottom: "1.5rem" }}>
        <button className="btn btn-secondary" style={{ padding: "0.5rem", minWidth: "44px" }} onClick={() => step > 1 ? setStep(step - 1) : router.back()}>
          Back
        </button>
        <h1 style={{ margin: 0, fontSize: "1.5rem", color: "var(--primary)" }}>Register Item</h1>
      </div>

      {error && (
        <div className="glass-panel" style={{ borderLeft: "4px solid var(--danger)", marginBottom: "1rem" }}>
          <p style={{ color: "var(--danger)", margin: 0 }}>{error}</p>
        </div>
      )}

      {/* STEP 1: Classification */}
      {step === 1 && (
        <div className="glass-panel slide-in">
          <h2 style={{ fontSize: "1.2rem", marginBottom: "1rem" }}>1. Classification</h2>
          
          <div className="input-group">
            <label>Part Type *</label>
            <div style={{ display: "flex", gap: "0.5rem" }}>
              <select className="input-control" value={partTypeId} onChange={e => setPartTypeId(parseInt(e.target.value))}>
                <option value="">-- Select --</option>
                {partTypes.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
              </select>
              <button className="btn btn-secondary" onClick={() => setShowModal("part-type")}>+ New</button>
            </div>
          </div>

          <div className="input-group">
            <label>Brand *</label>
            <div style={{ display: "flex", gap: "0.5rem" }}>
              <select className="input-control" value={brandId} onChange={e => setBrandId(parseInt(e.target.value))}>
                <option value="">-- Select --</option>
                {brands.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
              </select>
              <button className="btn btn-secondary" onClick={() => setShowModal("brand")}>+ New</button>
            </div>
          </div>

          <div className="input-group">
            <label>Manufacturer *</label>
            <div style={{ display: "flex", gap: "0.5rem" }}>
              <select className="input-control" value={manufacturerId} onChange={handleManufacturerChange}>
                <option value="">-- Select --</option>
                {manufacturers.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
              </select>
              <button className="btn btn-secondary" onClick={() => setShowModal("manufacturer")}>+ New</button>
            </div>
          </div>

          <div className="input-group">
            <label>Model *</label>
            <div style={{ display: "flex", gap: "0.5rem" }}>
              <select className="input-control" value={modelId} onChange={e => setModelId(parseInt(e.target.value))} disabled={!manufacturerId}>
                <option value="">-- Select --</option>
                {models.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
              </select>
              <button className="btn btn-secondary" disabled={!manufacturerId} onClick={() => setShowModal("model")}>+ New</button>
            </div>
          </div>

          <button className="btn btn-primary" style={{ width: "100%", marginTop: "1rem" }} onClick={() => setStep(2)}>Next</button>
        </div>
      )}

      {/* STEP 2: Details */}
      {step === 2 && (
        <div className="glass-panel slide-in">
          <h2 style={{ fontSize: "1.2rem", marginBottom: "1rem" }}>2. Item Details</h2>
          
          <div className="input-group">
            <label>Description (Optional)</label>
            <input type="text" className="input-control" value={description} onChange={e => setDescription(e.target.value)} />
          </div>

          <div className="input-group">
            <label>Country of Origin (Optional)</label>
            <input type="text" className="input-control" value={countryOfOrigin} onChange={e => setCountryOfOrigin(e.target.value)} />
          </div>

          <div className="input-group">
            <label>Rack Location</label>
            <div style={{ display: "flex", gap: "0.5rem" }}>
              <select className="input-control" value={rackId} onChange={e => setRackId(parseInt(e.target.value))}>
                <option value="">-- Select --</option>
                {racks.map(r => <option key={r.id} value={r.id}>{r.locationCode}</option>)}
              </select>
              <button className="btn btn-secondary" onClick={() => setShowModal("rack")}>+ New</button>
            </div>
          </div>

          <div className="input-group">
            <label>Low Stock Threshold</label>
            <input type="number" className="input-control" value={lowStockThreshold} onChange={e => setLowStockThreshold(e.target.value)} />
          </div>

          <div className="input-group">
            <label>Custom Barcode (Optional)</label>
            <input type="text" className="input-control" value={customBarcode} onChange={e => setCustomBarcode(e.target.value)} placeholder="Leave blank to auto-generate" />
          </div>

          <div className="input-group">
            <label>Secret Price Code</label>
            <input type="text" className="input-control" value={secretPriceCode} onChange={e => setSecretPriceCode(e.target.value)} />
          </div>

          <div style={{ display: "flex", gap: "1rem", marginTop: "1rem" }}>
             <button className="btn btn-secondary" style={{ flex: 1 }} onClick={() => setStep(1)}>Back</button>
             <button className="btn btn-primary" style={{ flex: 1 }} onClick={() => setStep(3)}>Next</button>
          </div>
        </div>
      )}

      {/* STEP 3: Compatibility */}
      {step === 3 && (
        <div className="glass-panel slide-in">
          <h2 style={{ fontSize: "1.2rem", marginBottom: "1rem" }}>3. Compatible Models</h2>
          <p style={{ fontSize: "0.9rem", color: "var(--text-secondary)", marginBottom: "1rem" }}>
            Add compatible vehicle models for this item.
          </p>

          <div className="input-group">
            <label>Manufacturer</label>
            <input type="text" className="input-control" value={modalInput1} onChange={e => setModalInput1(e.target.value)} placeholder="e.g. Toyota" />
          </div>
          <div className="input-group">
            <label>Model</label>
            <input type="text" className="input-control" value={modalInput2} onChange={e => setModalInput2(e.target.value)} placeholder="e.g. Camry" />
          </div>
          <div className="input-group">
            <label>Year Range</label>
            <input type="text" className="input-control" id="compYear" placeholder="e.g. 2015-2020" />
          </div>
          
          <button className="btn btn-secondary" style={{ width: "100%", marginBottom: "1rem" }} onClick={() => {
            const year = (document.getElementById("compYear") as HTMLInputElement)?.value || "";
            if (modalInput1 || modalInput2 || year) {
              setCompatibleModels([...compatibleModels, { manufacturer: modalInput1, model: modalInput2, yearRange: year }]);
              setModalInput1("");
              setModalInput2("");
              (document.getElementById("compYear") as HTMLInputElement).value = "";
            }
          }}>
            Add Compatible Model
          </button>

          {compatibleModels.length > 0 && (
            <div style={{ marginBottom: "1rem", display: "flex", flexDirection: "column", gap: "0.5rem" }}>
              {compatibleModels.map((cm, idx) => (
                <div key={idx} style={{ padding: "0.5rem", background: "var(--bg-main)", borderRadius: "4px", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <span>{cm.manufacturer} {cm.model} {cm.yearRange}</span>
                  <button className="btn btn-secondary" style={{ padding: "0.25rem 0.5rem" }} onClick={() => {
                    setCompatibleModels(compatibleModels.filter((_, i) => i !== idx));
                  }}>Remove</button>
                </div>
              ))}
            </div>
          )}

          <div style={{ display: "flex", gap: "1rem", marginTop: "1rem" }}>
             <button className="btn btn-secondary" style={{ flex: 1 }} onClick={() => setStep(2)}>Back</button>
             <button className="btn btn-primary" style={{ flex: 1 }} onClick={() => setStep(4)}>Next</button>
          </div>
        </div>
      )}

      {/* STEP 4: Image */}
      {step === 4 && (
        <div className="glass-panel slide-in">
          <h2 style={{ fontSize: "1.2rem", marginBottom: "1rem" }}>4. Image Capture</h2>
          <p style={{ fontSize: "0.9rem", color: "var(--text-secondary)", marginBottom: "1rem" }}>
            Take a photo of the item using your camera.
          </p>

          <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: "1rem" }}>
            {imagePreview ? (
              <img src={imagePreview} alt="Preview" style={{ width: "100%", maxHeight: "300px", objectFit: "cover", borderRadius: "8px", border: "2px solid var(--border)" }} />
            ) : (
              <div style={{ width: "100%", height: "200px", border: "2px dashed var(--border)", borderRadius: "8px", display: "flex", alignItems: "center", justifyContent: "center", background: "var(--bg-main)" }}>
                <span style={{ color: "var(--text-secondary)" }}>No Image Captured</span>
              </div>
            )}
            
            <label className="btn btn-primary" style={{ width: "100%", textAlign: "center", cursor: "pointer" }}>
              <input 
                type="file" 
                accept="image/*" 
                capture="environment" 
                onChange={handleImageCapture} 
                style={{ display: "none" }} 
              />
              {imagePreview ? "Retake Photo" : "Open Camera"}
            </label>
          </div>

          <div style={{ display: "flex", gap: "1rem", marginTop: "1rem" }}>
             <button className="btn btn-secondary" style={{ flex: 1 }} onClick={() => setStep(3)}>Back</button>
             <button className="btn btn-primary" style={{ flex: 1 }} onClick={() => setStep(5)}>Next</button>
          </div>
        </div>
      )}

      {/* STEP 5: Review */}
      {step === 5 && (
        <div className="glass-panel slide-in">
          <h2 style={{ fontSize: "1.2rem", marginBottom: "1rem" }}>5. Review & Submit</h2>
          
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.5rem", fontSize: "0.9rem", marginBottom: "1rem" }}>
            <div style={{ color: "var(--text-secondary)" }}>Part Type:</div>
            <div>{partTypes.find(p => p.id === partTypeId)?.name}</div>
            
            <div style={{ color: "var(--text-secondary)" }}>Brand:</div>
            <div>{brands.find(b => b.id === brandId)?.name}</div>
            
            <div style={{ color: "var(--text-secondary)" }}>Manufacturer:</div>
            <div>{manufacturers.find(m => m.id === manufacturerId)?.name}</div>
            
            <div style={{ color: "var(--text-secondary)" }}>Model:</div>
            <div>{models.find(m => m.id === modelId)?.name}</div>

            <div style={{ color: "var(--text-secondary)" }}>Description:</div>
            <div>{description}</div>
            
            <div style={{ color: "var(--text-secondary)" }}>Image:</div>
            <div>{imageFile ? "Attached" : "None"}</div>
          </div>

          <div style={{ display: "flex", gap: "1rem", marginTop: "1rem" }}>
             <button className="btn btn-secondary" style={{ flex: 1 }} onClick={() => setStep(4)}>Back</button>
             <button className="btn btn-primary" style={{ flex: 1 }} onClick={handleSubmit} disabled={loading}>
               {loading ? "Registering..." : "Register Item"}
             </button>
          </div>
        </div>
      )}

      {/* Modals */}
      {showModal && (
        <div style={{ position: "fixed", top: 0, left: 0, right: 0, bottom: 0, backgroundColor: "rgba(0,0,0,0.5)", zIndex: 100, display: "flex", alignItems: "center", justifyContent: "center", padding: "1rem" }}>
          <div className="glass-panel slide-in" style={{ width: "100%", maxWidth: "400px" }}>
            <h3 style={{ marginTop: 0 }}>Add New {showModal.replace("-", " ")}</h3>
            <div className="input-group">
              <label>Name / Code</label>
              <input type="text" className="input-control" value={modalInput1} onChange={e => setModalInput1(e.target.value)} autoFocus />
            </div>
            {showModal === "model" && (
              <div className="input-group">
                <label>Year Range (optional)</label>
                <input type="text" className="input-control" value={modalInput2} onChange={e => setModalInput2(e.target.value)} placeholder="e.g. 2015-2020" />
              </div>
            )}
            <div style={{ display: "flex", gap: "1rem", marginTop: "1rem" }}>
              <button className="btn btn-secondary" style={{ flex: 1 }} onClick={() => setShowModal(null)}>Cancel</button>
              <button className="btn btn-primary" style={{ flex: 1 }} onClick={handleCreateReferenceData} disabled={!modalInput1 || loading}>Save</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
