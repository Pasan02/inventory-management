"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { getActiveApiUrl } from "@/lib/apiConfig";

export default function NewItemPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  
  // Reference Data
  const [partTypes, setPartTypes] = useState<any[]>([]);
  const [brands, setBrands] = useState<any[]>([]);
  const [manufacturers, setManufacturers] = useState<any[]>([]);
  const [racks, setRacks] = useState<any[]>([]);
  const [models, setModels] = useState<any[]>([]);

  // Form State
  const [formData, setFormData] = useState({
    partTypeId: "",
    partBrandId: "",
    manufacturerId: "",
    vehicleModelId: "",
    countryOfOrigin: "",
    description: "",
    lowStockThreshold: 5,
    rackId: "",
    customBarcode: "",
    secretPriceCode: ""
  });

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token) {
      router.replace("/login");
      return;
    }
    fetchReferenceData(token);
  }, [router]);

  const fetchReferenceData = async (token: string) => {
    try {
      const apiUrl = await getActiveApiUrl();
      const res = await fetch(`${apiUrl}/api/items/reference-data`, {
        headers: { "Authorization": `Bearer ${token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setPartTypes(data.partTypes);
        setBrands(data.brands);
        setManufacturers(data.manufacturers);
        setRacks(data.racks);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  const fetchModels = async (manufacturerId: string) => {
    const token = localStorage.getItem("token");
    const apiUrl = await getActiveApiUrl();
    try {
      const res = await fetch(`${apiUrl}/api/items/models/${manufacturerId}`, {
        headers: { "Authorization": `Bearer ${token}` }
      });
      if (res.ok) {
        setModels(await res.json());
      }
    } catch (e) {
      console.error(e);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
    
    if (name === "manufacturerId" && value) {
      fetchModels(value);
      setFormData(prev => ({ ...prev, vehicleModelId: "" })); // reset model
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const token = localStorage.getItem("token");
      const apiUrl = await getActiveApiUrl();
      const payload = {
        ...formData,
        partTypeId: parseInt(formData.partTypeId),
        partBrandId: parseInt(formData.partBrandId),
        vehicleModelId: parseInt(formData.vehicleModelId),
        rackId: formData.rackId ? parseInt(formData.rackId) : null,
      };

      const res = await fetch(`${apiUrl}/api/items`, {
        method: "POST",
        headers: { 
          "Authorization": `Bearer ${token}`,
          "Content-Type": "application/json"
        },
        body: JSON.stringify(payload)
      });

      if (res.ok) {
        const data = await res.json();
        alert(`Item created! Barcode: ${data.barcode}`);
        router.push("/dashboard");
      } else {
        const err = await res.json();
        alert(`Error: ${err.message}`);
      }
    } catch(err) {
      alert("Network error saving item.");
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="container" style={{ padding: "2rem", textAlign: "center" }}>Loading Reference Data...</div>;

  return (
    <div className="container" style={{ paddingBottom: "5rem" }}>
      <div style={{ marginTop: "1rem", display: "flex", alignItems: "center", gap: "1rem", marginBottom: "1.5rem" }}>
        <button className="btn btn-secondary" style={{ padding: "0.5rem", minWidth: "44px" }} onClick={() => router.back()}>
          Back
        </button>
        <h1 style={{ margin: 0, fontSize: "1.5rem", color: "var(--primary)" }}>New Item</h1>
      </div>

      <form onSubmit={handleSubmit} className="glass-panel">
        
        <div style={{ marginBottom: "1rem" }}>
          <label style={{ display: "block", marginBottom: "0.25rem", fontSize: "0.9rem" }}>Part Type *</label>
          <select required className="input-control" style={{ width: "100%" }} name="partTypeId" value={formData.partTypeId} onChange={handleChange}>
            <option value="">Select Part Type</option>
            {partTypes.map(pt => <option key={pt.id} value={pt.id}>{pt.name}</option>)}
          </select>
        </div>

        <div style={{ marginBottom: "1rem" }}>
          <label style={{ display: "block", marginBottom: "0.25rem", fontSize: "0.9rem" }}>Manufacturer *</label>
          <select required className="input-control" style={{ width: "100%" }} name="manufacturerId" value={formData.manufacturerId} onChange={handleChange}>
            <option value="">Select Manufacturer</option>
            {manufacturers.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
          </select>
        </div>

        <div style={{ marginBottom: "1rem" }}>
          <label style={{ display: "block", marginBottom: "0.25rem", fontSize: "0.9rem" }}>Model *</label>
          <select required className="input-control" style={{ width: "100%" }} name="vehicleModelId" value={formData.vehicleModelId} onChange={handleChange} disabled={!formData.manufacturerId}>
            <option value="">{formData.manufacturerId ? "Select Model" : "Select Manufacturer First"}</option>
            {models.map(m => <option key={m.id} value={m.id}>{m.name} ({m.yearRange})</option>)}
          </select>
        </div>

        <div style={{ marginBottom: "1rem" }}>
          <label style={{ display: "block", marginBottom: "0.25rem", fontSize: "0.9rem" }}>Brand *</label>
          <select required className="input-control" style={{ width: "100%" }} name="partBrandId" value={formData.partBrandId} onChange={handleChange}>
            <option value="">Select Brand</option>
            {brands.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
          </select>
        </div>

        <div style={{ marginBottom: "1rem" }}>
          <label style={{ display: "block", marginBottom: "0.25rem", fontSize: "0.9rem" }}>Country of Origin *</label>
          <input required type="text" className="input-control" style={{ width: "100%" }} name="countryOfOrigin" value={formData.countryOfOrigin} onChange={handleChange} placeholder="e.g. Japan" />
        </div>

        <div style={{ marginBottom: "1rem" }}>
          <label style={{ display: "block", marginBottom: "0.25rem", fontSize: "0.9rem" }}>Pcode (Secret Price Code)</label>
          <input type="text" className="input-control" style={{ width: "100%" }} name="secretPriceCode" value={formData.secretPriceCode} onChange={handleChange} />
        </div>

        <div style={{ marginBottom: "1rem" }}>
          <label style={{ display: "block", marginBottom: "0.25rem", fontSize: "0.9rem" }}>Custom Barcode (Optional)</label>
          <input type="text" className="input-control" style={{ width: "100%" }} name="customBarcode" value={formData.customBarcode} onChange={handleChange} placeholder="Leave blank to auto-generate" />
        </div>

        <div style={{ marginBottom: "1rem" }}>
          <label style={{ display: "block", marginBottom: "0.25rem", fontSize: "0.9rem" }}>Description / Notes *</label>
          <textarea required className="input-control" style={{ width: "100%", minHeight: "80px", resize: "vertical" }} name="description" value={formData.description} onChange={handleChange} placeholder="e.g. AC Compressor" />
        </div>

        <div style={{ marginBottom: "1rem" }}>
          <label style={{ display: "block", marginBottom: "0.25rem", fontSize: "0.9rem" }}>Rack Location</label>
          <select className="input-control" style={{ width: "100%" }} name="rackId" value={formData.rackId} onChange={handleChange}>
            <option value="">Unassigned</option>
            {racks.map(r => <option key={r.id} value={r.id}>{r.locationCode}</option>)}
          </select>
        </div>

        <div style={{ marginBottom: "1.5rem" }}>
          <label style={{ display: "block", marginBottom: "0.25rem", fontSize: "0.9rem" }}>Low Stock Threshold</label>
          <input type="number" className="input-control" style={{ width: "100%" }} name="lowStockThreshold" value={formData.lowStockThreshold} onChange={handleChange} min="1" />
        </div>

        <button type="submit" className="btn btn-primary" style={{ width: "100%" }} disabled={saving}>
          {saving ? "Saving..." : "Create Item"}
        </button>

      </form>
    </div>
  );
}
