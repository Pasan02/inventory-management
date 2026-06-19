"use client";

import { useEffect, useRef, useState } from "react";
import { BrowserMultiFormatReader, BarcodeFormat, DecodeHintType } from "@zxing/library";

interface BarcodeScannerProps {
  onResult: (result: string) => void;
  onClose: () => void;
}

export default function BarcodeScanner({ onResult, onClose }: BarcodeScannerProps) {
  const [error, setError] = useState<string | null>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const codeReaderRef = useRef<BrowserMultiFormatReader | null>(null);

  useEffect(() => {
    // Configure scanner hints for better accuracy and support for Code 128 Subset C
    const hints = new Map();
    hints.set(DecodeHintType.POSSIBLE_FORMATS, [
      BarcodeFormat.CODE_128,
      BarcodeFormat.CODE_39,
      BarcodeFormat.UPC_A,
      BarcodeFormat.UPC_E,
      BarcodeFormat.EAN_13,
      BarcodeFormat.EAN_8
    ]);
    hints.set(DecodeHintType.TRY_HARDER, true);

    const codeReader = new BrowserMultiFormatReader(hints);
    codeReaderRef.current = codeReader;

    if (videoRef.current) {
      codeReader.decodeFromConstraints(
        { 
          video: { 
            facingMode: "environment",
            width: { ideal: 1920 },
            height: { ideal: 1080 }
          } 
        },
        videoRef.current,
        (result, err) => {
          if (result) {
            onResult(result.getText());
          }
        }
      ).catch((e: any) => {
        setError(`Camera access denied or unavailable: ${e.message}`);
      });
    }

    return () => {
      if (codeReaderRef.current) {
        codeReaderRef.current.reset();
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
          <div style={{ width: "100%", overflow: "hidden", borderRadius: "4px", background: "#000" }}>
            <video ref={videoRef} style={{ width: "100%", height: "auto", display: "block" }} />
          </div>
        )}
      </div>
    </div>
  );
}
