"use client";

import { useEffect, useRef, useState } from "react";
import Quagga from "@ericblade/quagga2";

interface BarcodeScannerProps {
  onResult: (result: string) => void;
  onClose: () => void;
}

export default function BarcodeScanner({ onResult, onClose }: BarcodeScannerProps) {
  const [error, setError] = useState<string | null>(null);
  const scannerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let isScanning = false;

    if (scannerRef.current) {
      Quagga.init(
        {
          inputStream: {
            type: "LiveStream",
            target: scannerRef.current,
            constraints: {
              width: { ideal: 1920 },
              height: { ideal: 1080 },
              facingMode: "environment",
            },
            area: {
              top: "10%",
              right: "5%",
              left: "5%",
              bottom: "10%"
            }
          },
          locator: {
            patchSize: "medium",
            halfSample: true,
          },
          numOfWorkers: typeof navigator !== 'undefined' && navigator.hardwareConcurrency ? navigator.hardwareConcurrency : 2,
          decoder: {
            readers: [
              "code_128_reader",
              "ean_reader",
              "upc_reader",
              "code_39_reader"
            ],
            multiple: false
          },
          locate: true
        },
        (err) => {
          if (err) {
            setError(`Camera access denied or unavailable: ${err.message || err}`);
            return;
          }
          Quagga.start();
          isScanning = true;
        }
      );

      const handleDetected = (result: any) => {
        if (result && result.codeResult && result.codeResult.code) {
          onResult(result.codeResult.code);
        }
      };

      Quagga.onDetected(handleDetected);

      return () => {
        if (isScanning) {
          Quagga.stop();
          Quagga.offDetected(handleDetected);
        }
      };
    }
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
          <div style={{ width: "100%", overflow: "hidden", borderRadius: "4px", background: "#000", position: "relative" }}>
            <div ref={scannerRef} className="quagga-container" style={{ width: "100%" }} />
            <style jsx>{`
              .quagga-container :global(video) {
                width: 100% !important;
                height: auto !important;
                display: block;
              }
              .quagga-container :global(canvas) {
                position: absolute;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
              }
            `}</style>
          </div>
        )}
      </div>
    </div>
  );
}
