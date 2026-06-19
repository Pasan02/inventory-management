"use client";

import { useEffect, useRef, useState } from "react";
import { Html5Qrcode, Html5QrcodeSupportedFormats } from "html5-qrcode";

interface BarcodeScannerProps {
  onResult: (result: string) => void;
  onClose: () => void;
}

export default function BarcodeScanner({ onResult, onClose }: BarcodeScannerProps) {
  const [error, setError] = useState<string | null>(null);
  const scannerRef = useRef<Html5Qrcode | null>(null);

  useEffect(() => {
    // Initialize scanner
    const html5Qrcode = new Html5Qrcode("reader");
    scannerRef.current = html5Qrcode;

    const config = { 
      fps: 10, 
      qrbox: { width: 250, height: 150 }, 
      aspectRatio: 1.0,
      useBarCodeDetectorIfSupported: true,
      formatsToSupport: [
        Html5QrcodeSupportedFormats.CODE_128,
        Html5QrcodeSupportedFormats.CODE_39,
        Html5QrcodeSupportedFormats.UPC_A,
        Html5QrcodeSupportedFormats.UPC_E,
        Html5QrcodeSupportedFormats.EAN_13,
        Html5QrcodeSupportedFormats.EAN_8
      ]
    };
    
    html5Qrcode.start(
      { facingMode: "environment" },
      config,
      (decodedText) => {
        // Success callback
        html5Qrcode.stop().then(() => {
          onResult(decodedText);
        }).catch(err => {
          console.error("Failed to stop scanner", err);
          onResult(decodedText);
        });
      },
      (errorMessage) => {
        // Warning/Ignored callbacks (e.g. no barcode detected in frame)
        // We do not set error state here to avoid flickering.
      }
    ).catch(err => {
      setError(`Camera access denied or unavailable: ${err}`);
    });

    return () => {
      if (scannerRef.current && scannerRef.current.isScanning) {
        scannerRef.current.stop().catch(console.error);
      }
    };
  }, [onResult]);

  return (
    <div style={{
      position: "fixed",
      top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: "rgba(0,0,0,0.8)",
      zIndex: 9999,
      display: "flex",
      flexDirection: "column",
      justifyContent: "center",
      alignItems: "center",
      padding: "1rem"
    }}>
      <div style={{ background: "white", padding: "1rem", borderRadius: "8px", width: "100%", maxWidth: "400px" }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "1rem" }}>
          <h3 style={{ margin: 0, color: "var(--text-primary)" }}>Scan Barcode</h3>
          <button className="btn btn-secondary" onClick={onClose} style={{ padding: "0.25rem 0.5rem" }}>Close</button>
        </div>
        
        {error ? (
          <div style={{ color: "var(--danger)", padding: "1rem", textAlign: "center" }}>{error}</div>
        ) : (
          <div id="reader" style={{ width: "100%" }}></div>
        )}
      </div>
    </div>
  );
}
