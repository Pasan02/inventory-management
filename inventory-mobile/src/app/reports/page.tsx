"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";
import { getActiveApiUrl } from "@/lib/apiConfig";

type Tab = "pending" | "ordered" | "snapshot" | "low-stock" | "activity";

export default function ReportsPage() {
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<Tab>("pending");
  
  const [data, setData] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Multi-select state for Pending / Ordered tabs
  const [selectedIds, setSelectedIds] = useState<Set<number>>(new Set());

  useEffect(() => {
    fetchData(activeTab);
  }, [activeTab]);

  const fetchWithAuth = async (path: string, options: any = {}) => {
    const token = localStorage.getItem("token");
    const apiUrl = await getActiveApiUrl();
    const res = await fetch(`${apiUrl}${path}`, {
      ...options,
      headers: { 
        ...options.headers,
        "Authorization": `Bearer ${token}` 
      }
    });
    if (!res.ok) {
      const errorData = await res.json().catch(() => null);
      throw new Error(errorData?.message || "Failed to fetch data");
    }
    return await res.json();
  };

  const fetchData = async (tab: Tab) => {
    setLoading(true);
    setError(null);
    setSelectedIds(new Set()); // Reset selections when switching tabs
    setData([]);
    try {
      let endpoint = "";
      if (tab === "snapshot") endpoint = "/api/reports/snapshot";
      else if (tab === "low-stock") endpoint = "/api/reports/low-stock";
      else if (tab === "activity") endpoint = "/api/reports/activity";
      else if (tab === "pending") endpoint = "/api/reports/orders/pending";
      else if (tab === "ordered") endpoint = "/api/reports/orders/arrived"; // Our backend endpoint for 'Ordered' status

      const result = await fetchWithAuth(endpoint);
      setData(result || []);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const toggleSelection = (orderIdList: number[]) => {
    const newSelection = new Set(selectedIds);
    // Use the first ID as the representative key for the group selection
    const key = orderIdList[0];
    if (newSelection.has(key)) {
      newSelection.delete(key);
    } else {
      newSelection.add(key);
    }
    setSelectedIds(newSelection);
  };

  const selectAll = () => {
    if (selectedIds.size === data.length) {
      setSelectedIds(new Set());
    } else {
      setSelectedIds(new Set(data.map(d => d.orderIds[0])));
    }
  };

  const handlePlaceOrders = async () => {
    if (selectedIds.size === 0) return;
    setLoading(true);
    try {
      // Gather all actual order IDs from the selected groups
      let allIds: number[] = [];
      data.forEach(d => {
        if (selectedIds.has(d.orderIds[0])) {
          allIds = [...allIds, ...d.orderIds];
        }
      });

      await fetchWithAuth("/api/reports/orders/place", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ orderIds: allIds })
      });
      alert("Orders placed successfully.");
      
      const now = new Date().toISOString();
      const selectedItems = data.filter(d => selectedIds.has(d.orderIds[0]));
      const itemsToPrint = selectedItems.map(item => ({...item, orderedAt: now}));
      handleGeneratePDF("download", itemsToPrint);

      fetchData("pending");
    } catch (err: any) {
      alert("Failed to place orders: " + err.message);
      setLoading(false);
    }
  };

  const handleArriveOrders = async () => {
    if (selectedIds.size === 0) return;
    setLoading(true);
    try {
      // Gather all actual order IDs from the selected groups
      let allIds: number[] = [];
      data.forEach(d => {
        if (selectedIds.has(d.orderIds[0])) {
          allIds = [...allIds, ...d.orderIds];
        }
      });

      await fetchWithAuth("/api/reports/orders/arrive", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ orderIds: allIds })
      });
      alert("Orders marked as arrived.");
      fetchData("ordered");
    } catch (err: any) {
      alert("Failed to arrive orders: " + err.message);
      setLoading(false);
    }
  };

  const handleGeneratePDF = (action: "download" | "print", itemsToPrint?: any[]) => {
    const items = itemsToPrint || (activeTab === "ordered" ? data : data.filter(d => selectedIds.has(d.orderIds[0])));
    if (items.length === 0) {
        alert("No items to process.");
        return;
    }
    
    try {
        const doc = new jsPDF({ format: 'a4' });
        
        // Group items by orderedAt
        const groupedItems = items.reduce((acc, item) => {
            const key = item.orderedAt ? new Date(item.orderedAt).toLocaleString('sv').substring(0, 16) : new Date().toLocaleString('sv').substring(0, 16);
            if (!acc[key]) acc[key] = [];
            acc[key].push(item);
            return acc;
        }, {} as Record<string, any[]>);

        let isFirstGroup = true;
        let finalY = 10;
        const pageHeight = doc.internal.pageSize.height || doc.internal.pageSize.getHeight();

        for (const [key, groupItems] of Object.entries(groupedItems) as [string, any[]][]) {
            const orderTimeText = groupItems[0].orderedAt ? new Date(groupItems[0].orderedAt).toLocaleString() : new Date().toLocaleString();

            if (!isFirstGroup) {
                if (finalY + 40 > pageHeight - 20) {
                    doc.addPage();
                    finalY = 10;
                } else {
                    finalY += 10;
                    // Draw separator
                    doc.setDrawColor(148, 163, 184);
                    doc.setLineWidth(0.5);
                    doc.setLineDashPattern([2, 2], 0);
                    doc.line(14, finalY, 196, finalY);
                    doc.setLineDashPattern([], 0);
                    finalY += 10;
                }
            } else {
                finalY = 10;
            }

            if (finalY === 10) {
                // Title block
                doc.setFont("helvetica", "bold");
                doc.setFontSize(18);
                doc.setTextColor(0, 0, 0); // Black
                doc.text("ALPINE AUTO A/C", 14, finalY + 10);
                
                doc.setFontSize(22);
                doc.text("PURCHASE ORDER SLIP", 14, finalY + 20);
                
                finalY += 30;
            } else {
                finalY += 10;
            }
            
            // Date and Time (Local)
            doc.setFont("helvetica", "normal");
            doc.setFontSize(10);
            doc.setTextColor(0, 0, 0);
            doc.text(`Placed Date/Time: ${orderTimeText}`, 14, finalY);

            // Print footer
            const currentPrintTime = new Date().toLocaleString();
            doc.text(`Printed on: ${currentPrintTime}`, 14, pageHeight - 10);
            
            // Horizontal line separator
            doc.setDrawColor(0, 0, 0);
            doc.setLineWidth(0.5);
            doc.line(14, finalY + 6, 196, finalY + 6);
            
            // Headers and Rows mapping matching WPF PDF exactly
            const headers = [["Type", "Brand", "Manufacturer", "Model", "Barcode", "Qty", "Date Removed"]];
            const rows = groupItems.map((item: any) => [
                item.partType || "N/A",
                item.brand || "N/A",
                item.manufacturer || "N/A",
                item.model || "N/A",
                item.barcode || "N/A",
                `${item.quantity || item.totalQuantity || 0}`,
                item.createdAt ? new Date(item.createdAt).toLocaleString() : "N/A"
            ]);
            
            // Draw Table
            autoTable(doc, {
                startY: finalY + 10,
                head: headers,
                body: rows,
                theme: "grid",
                headStyles: { fillColor: [255, 255, 255], textColor: [0, 0, 0], fontStyle: "bold", lineWidth: 0.1, lineColor: [0, 0, 0] },
                styles: { fontSize: 9, cellPadding: 4, valign: "middle", textColor: [0, 0, 0], lineWidth: 0.1, lineColor: [0, 0, 0] },
                margin: { top: 20 },
                columnStyles: {
                    5: { halign: "center" }
                }
            });

            finalY = (doc as any).lastAutoTable.finalY;
            isFirstGroup = false;
        }
        
        if (action === "download") {
            const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
            doc.save(`Purchase_Order_${timestamp}.pdf`);
            alert("PDF Order slip generated and downloaded successfully!");
        } else if (action === "print") {
            doc.autoPrint();
            window.open(doc.output('bloburl'), '_blank');
        }
    } catch (e: any) {
        console.error("PDF generation error", e);
        alert("Error generating PDF: " + e.message);
    }
  };

  const routeToAddStock = (item: any) => {
    const firstBarcode = item.barcode.split(',')[0].trim();
    router.push(`/add-stock?barcode=${encodeURIComponent(firstBarcode)}&quantity=${item.totalQuantity}&orderIds=${item.orderIds.join(',')}`);
  };

  return (
    <div className="container" style={{ paddingBottom: "6rem", display: "flex", flexDirection: "column", height: "100vh" }}>
      <div style={{ marginTop: "1rem", marginBottom: "1rem", flexShrink: 0 }}>
        <h1 style={{ margin: 0, fontSize: "1.5rem", color: "var(--primary)" }}>Reports</h1>
      </div>

      {/* Tabs */}
      <div style={{ 
        display: "flex", 
        overflowX: "auto", 
        gap: "0.5rem", 
        paddingBottom: "0.5rem", 
        marginBottom: "1rem",
        borderBottom: "1px solid var(--border)",
        scrollbarWidth: "none", // Hide scrollbar for clean look
        flexShrink: 0
      }}>
        {(["pending", "ordered", "snapshot", "low-stock", "activity"] as Tab[]).map((tab) => (
          <button 
            key={tab}
            className={`btn ${activeTab === tab ? "btn-primary" : "btn-secondary"}`}
            style={{ 
              whiteSpace: "nowrap", 
              padding: "0.5rem 1rem",
              borderRadius: "20px",
              opacity: activeTab === tab ? 1 : 0.7
            }}
            onClick={() => setActiveTab(tab)}
          >
            {tab === "pending" && "Pending Orders"}
            {tab === "ordered" && "Ordered"}
            {tab === "snapshot" && "Snapshot"}
            {tab === "low-stock" && "Low Stock"}
            {tab === "activity" && "Activity"}
          </button>
        ))}
      </div>

      {error && (
        <div className="glass-panel" style={{ borderLeft: "4px solid var(--danger)", marginBottom: "1rem", flexShrink: 0 }}>
          <p style={{ color: "var(--danger)", margin: 0 }}>{error}</p>
        </div>
      )}

      {/* Main Content Area */}
      <div style={{ flexGrow: 1, overflowY: "auto" }}>
        {loading ? (
          <div style={{ textAlign: "center", padding: "2rem", color: "var(--text-secondary)" }}>Loading data...</div>
        ) : data.length === 0 ? (
          <div style={{ textAlign: "center", padding: "2rem", color: "var(--text-secondary)" }}>No data available for this report.</div>
        ) : (
          <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
            
            {/* MULTI-SELECT HEADER */}
            {(activeTab === "pending" || activeTab === "ordered") && (
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "0 0.5rem" }}>
                <span style={{ fontSize: "0.9rem", color: "var(--text-secondary)" }}>
                  {data.length} items found
                </span>
                <button className="btn btn-secondary" style={{ padding: "0.25rem 0.75rem", fontSize: "0.85rem" }} onClick={selectAll}>
                  {selectedIds.size === data.length ? "Deselect All" : "Select All"}
                </button>
              </div>
            )}

            {/* RENDER CARDS */}
            {data.map((item, idx) => (
              <div key={idx} className="glass-panel slide-in" style={{ 
                padding: "1rem", 
                borderLeft: (activeTab === "pending" || activeTab === "ordered") && selectedIds.has(item.orderIds?.[0]) 
                            ? "4px solid var(--primary)" : "4px solid transparent",
                cursor: (activeTab === "pending" || activeTab === "ordered") ? "pointer" : "default"
              }}
              onClick={() => {
                if (activeTab === "pending" || activeTab === "ordered") toggleSelection(item.orderIds);
              }}
              >
                {/* Header Row */}
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "0.5rem" }}>
                  <div style={{ fontWeight: "bold", fontSize: "1.1rem", wordBreak: "break-all", paddingRight: "1rem" }}>
                    {item.barcode || "N/A"}
                  </div>
                  {(activeTab === "snapshot" || activeTab === "low-stock") && (
                    <div style={{ 
                      background: item.quantity <= item.lowStockThreshold ? "var(--danger)" : "var(--primary)", 
                      color: "white", 
                      padding: "0.2rem 0.5rem", 
                      borderRadius: "12px", 
                      fontSize: "0.85rem",
                      fontWeight: "bold"
                    }}>
                      Qty: {item.quantity}
                    </div>
                  )}
                  {(activeTab === "pending" || activeTab === "ordered") && (
                    <div style={{ 
                      background: "var(--warning)", 
                      color: "black", 
                      padding: "0.2rem 0.5rem", 
                      borderRadius: "12px", 
                      fontSize: "0.85rem",
                      fontWeight: "bold"
                    }}>
                      Order Qty: {item.totalQuantity}
                    </div>
                  )}
                  {activeTab === "activity" && (
                    <div style={{ 
                      background: item.quantityChange > 0 ? "var(--success)" : (item.quantityChange < 0 ? "var(--danger)" : "var(--primary)"), 
                      color: item.quantityChange > 0 ? "black" : "white", 
                      padding: "0.2rem 0.5rem", 
                      borderRadius: "12px", 
                      fontSize: "0.85rem",
                      fontWeight: "bold"
                    }}>
                      {item.quantityChange > 0 ? `+${item.quantityChange}` : item.quantityChange}
                    </div>
                  )}
                </div>

                {/* Description */}
                <div style={{ fontSize: "0.95rem", marginBottom: "0.5rem", color: "var(--text-main)" }}>
                  {item.description || item.itemName}
                </div>

                {/* Sub-details grid */}
                <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.5rem", fontSize: "0.85rem", color: "var(--text-secondary)" }}>
                  
                  {item.partType && <div><strong>Type:</strong> {item.partType}</div>}
                  {item.brand && <div><strong>Brand:</strong> {item.brand}</div>}
                  {item.manufacturer && <div><strong>Mfr:</strong> {item.manufacturer}</div>}
                  {item.model && <div><strong>Model:</strong> {item.model}</div>}
                  {item.rack && <div><strong>Rack:</strong> {item.rack}</div>}
                  {item.countryOfOrigin && <div><strong>Origin:</strong> {item.countryOfOrigin}</div>}
                  
                  {activeTab === "activity" && (
                     <>
                       <div><strong>Action:</strong> {item.actionType}</div>
                       <div><strong>Machine:</strong> {item.machineName}</div>
                       <div style={{ gridColumn: "span 2" }}><strong>Time:</strong> {new Date(item.timestamp).toLocaleString()}</div>
                     </>
                  )}

                  {activeTab === "pending" && item.createdAt && (
                     <div style={{ gridColumn: "span 2" }}><strong>Requested:</strong> {new Date(item.createdAt).toLocaleString()}</div>
                  )}

                  {activeTab === "ordered" && item.orderedAt && (
                     <div style={{ gridColumn: "span 2" }}><strong>Ordered On:</strong> {new Date(item.orderedAt).toLocaleString()}</div>
                  )}

                  {(activeTab === "snapshot" || activeTab === "low-stock") && item.compatibleModelsText && item.compatibleModelsText !== "None" && (
                     <div style={{ gridColumn: "span 2", marginTop: "0.25rem" }}>
                       <strong>Compatibles:</strong> <span style={{ opacity: 0.8 }}>{item.compatibleModelsText}</span>
                     </div>
                  )}
                </div>

                {/* Individual Actions for Ordered Tab */}
                {activeTab === "ordered" && (
                  <div style={{ marginTop: "1rem", display: "flex", justifyContent: "flex-end" }}>
                    <button 
                      className="btn btn-secondary" 
                      style={{ fontSize: "0.85rem", padding: "0.4rem 1rem", borderColor: "var(--success)", color: "var(--success)" }}
                      onClick={(e) => {
                        e.stopPropagation(); // Prevent toggling selection
                        routeToAddStock(item);
                      }}
                    >
                      Arrive & Add Stock →
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Sticky Action Bar for Pending / Ordered multi-select */}
      {((activeTab === "pending" && selectedIds.size > 0) || (activeTab === "ordered" && data.length > 0)) && (
        <div className="slide-in" style={{ 
          position: "fixed", 
          bottom: "70px", 
          left: "1rem", 
          right: "1rem", 
          background: "var(--surface)", 
          padding: "1rem", 
          borderRadius: "var(--radius-md)", 
          boxShadow: "0 -4px 20px rgba(0,0,0,0.5)",
          border: "1px solid var(--border)",
          display: "flex",
          flexDirection: "column",
          gap: "0.75rem",
          zIndex: 50
        }}>
          <div style={{ textAlign: "center" }}>
            {activeTab === "ordered" ? (
              <span style={{ fontSize: "0.85rem", color: "var(--text-dim)" }}>
                {selectedIds.size > 0 ? <><span style={{ fontWeight: "bold", color: "var(--primary)" }}>{selectedIds.size}</span> selected</> : `${data.length} total orders`}
              </span>
            ) : (
              <span style={{ fontSize: "0.85rem", color: "var(--text-dim)" }}><span style={{ fontWeight: "bold", color: "var(--primary)" }}>{selectedIds.size}</span> selected</span>
            )}
          </div>
          <div style={{ display: "flex", gap: "0.5rem", width: "100%" }}>
            {activeTab === "ordered" && (
              <>
                <button 
                  className="btn btn-secondary" 
                  style={{ flex: 1, fontSize: "0.85rem", padding: "0.5rem" }}
                  onClick={() => handleGeneratePDF("download", data)}
                  disabled={loading}
                >
                  Download PDF
                </button>
                <button 
                  className="btn btn-secondary" 
                  style={{ flex: 1, fontSize: "0.85rem", padding: "0.5rem" }}
                  onClick={() => handleGeneratePDF("print", data)}
                  disabled={loading}
                >
                  Print
                </button>
              </>
            )}
            <button 
              className="btn btn-primary" 
              style={{ flex: 1, backgroundColor: activeTab === "ordered" ? "var(--success)" : "var(--primary)" }}
              onClick={activeTab === "pending" ? handlePlaceOrders : handleArriveOrders}
              disabled={loading || (activeTab === "ordered" && selectedIds.size === 0)}
            >
              {loading ? "Processing..." : activeTab === "pending" ? "Place Orders" : "Arrive Selected"}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
