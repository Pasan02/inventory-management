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

      let counts: Record<string, number> = {};
      let totalReads = 0;

      const handleDetected = (result: any) => {
        if (result && result.codeResult && result.codeResult.code) {
          let code = result.codeResult.code;
          
          // Auto-correction: If it's exactly 12 characters and starts with ITM, 
          // force the 4th character to be a hyphen (fixes ITM% misreads)
          if (code.length === 12 && code.startsWith("ITM")) {
            code = "ITM-" + code.substring(4);
          }

          counts[code] = (counts[code] || 0) + 1;
          totalReads++;

          // Clear buffer if we get too many reads without a consensus
          if (totalReads > 20) {
            counts = {};
            totalReads = 0;
          }
          
          // Require 3 consistent reads to eliminate transient noise/glare misreads
          if (counts[code] >= 3) {
            onResult(code);
            isScanning = false;
            Quagga.stop();
          }
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
