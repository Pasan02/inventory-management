using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using inventory_management.ViewModels;

namespace inventory_management.Services
{
    public class PdfService : IPdfService
    {
        public Task<bool> GenerateOrderPdfAsync(string filePath, List<ReportOrderRow> items)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Create document
                    var document = new PdfDocument();
                    document.Info.Title = "Purchase Order Slip";
                    document.Info.Author = "Alpine Auto A/C Inventory System";
                    document.Info.Subject = "Purchase Order generated when items were moved to Ordered status";

                    // Define page sizes and margins
                    double pageHeight = 792; // Letter height
                    double pageWidth = 612;  // Letter width
                    double margin = 40;
                    double printableWidth = pageWidth - (margin * 2); // 532
                    double bottomMargin = pageHeight - margin; // 752

                    // Column configurations matching the table in Order Queue
                    double[] colWidths = { 80, 80, 80, 80, 100, 32, 80 }; // Sum = 532
                    string[] headers = { "Type", "Brand", "Manufacturer", "Model", "Barcode", "Qty", "Date Removed" };

                    // Fonts (Standard WPF/Segoe UI)
                    var fontTitle = new XFont("Segoe UI", 18, XFontStyleEx.Bold);
                    var fontHeader = new XFont("Segoe UI", 9, XFontStyleEx.Bold);
                    var fontBody = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
                    var fontFooter = new XFont("Segoe UI", 8, XFontStyleEx.Italic);
                    var fontSubtitle = new XFont("Segoe UI", 11, XFontStyleEx.Regular);

                    int totalQuantity = items.Sum(i => i.Quantity);
                    int pageNumber = 1;

                    // First Page Setup
                    var page = document.AddPage();
                    page.Size = PageSize.Letter;
                    var gfx = XGraphics.FromPdfPage(page);

                    // 1. Draw elegant top border bar
                    gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(30, 58, 138)), margin, margin, printableWidth, 4);

                    // 2. Draw Page number (Page 1)
                    gfx.DrawString("Page 1", fontFooter, XBrushes.DimGray, new XPoint(pageWidth - margin, margin - 5), new XStringFormat { Alignment = XStringAlignment.Far });

                    // 3. Draw Document Header and Info
                    gfx.DrawString("ALPINE AUTO A/C", fontHeader, XBrushes.Navy, new XPoint(margin, margin + 20));
                    gfx.DrawString("PURCHASE ORDER SLIP", fontTitle, XBrushes.Black, new XRect(margin, margin + 25, printableWidth, 30), XStringFormats.TopLeft);

                    string dateText = $"Placed Date/Time: {DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")}";
                    string summaryText = $"Total Items ordered: {items.Count}  |  Total Qty: {totalQuantity}";
                    gfx.DrawString(dateText, fontSubtitle, XBrushes.DimGray, new XPoint(margin, margin + 65));
                    gfx.DrawString(summaryText, fontSubtitle, XBrushes.DimGray, new XPoint(margin, margin + 83));

                    // 4. Draw a divider
                    gfx.DrawLine(XPens.LightGray, margin, margin + 100, pageWidth - margin, margin + 100);

                    // 5. Draw table headers on first page
                    double currentY = margin + 115;
                    double headerHeight = 22;
                    gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(30, 58, 138)), margin, currentY, printableWidth, headerHeight);

                    double headerX = margin;
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var format = new XStringFormat { LineAlignment = XLineAlignment.Center };
                        if (headers[i] == "Qty")
                        {
                            format.Alignment = XStringAlignment.Center;
                            gfx.DrawString(headers[i], fontHeader, XBrushes.White, new XRect(headerX, currentY, colWidths[i], headerHeight), format);
                        }
                        else
                        {
                            format.Alignment = XStringAlignment.Near;
                            gfx.DrawString(headers[i], fontHeader, XBrushes.White, new XRect(headerX + 5, currentY, colWidths[i] - 10, headerHeight), format);
                        }
                        headerX += colWidths[i];
                    }

                    currentY += headerHeight;

                    // 6. Draw Table Rows
                    double rowHeight = 20;
                    int rowIndex = 0;

                    foreach (var item in items)
                    {
                        // Check if we need to paginate
                        if (currentY + rowHeight > bottomMargin)
                        {
                            // Add a new page
                            pageNumber++;
                            page = document.AddPage();
                            page.Size = PageSize.Letter;
                            gfx = XGraphics.FromPdfPage(page);

                            // Draw top accent bar on new page
                            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(30, 58, 138)), margin, margin, printableWidth, 4);

                            // Page header text and page number
                            gfx.DrawString($"Page {pageNumber}", fontFooter, XBrushes.DimGray, new XPoint(pageWidth - margin, margin - 5), new XStringFormat { Alignment = XStringAlignment.Far });
                            gfx.DrawString("PURCHASE ORDER SLIP (Continued)", fontHeader, XBrushes.DimGray, new XPoint(margin, margin + 15));

                            // Re-draw table headers
                            currentY = margin + 25;
                            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(30, 58, 138)), margin, currentY, printableWidth, headerHeight);

                            double nextHeaderX = margin;
                            for (int i = 0; i < headers.Length; i++)
                            {
                                var format = new XStringFormat { LineAlignment = XLineAlignment.Center };
                                if (headers[i] == "Qty")
                                {
                                    format.Alignment = XStringAlignment.Center;
                                    gfx.DrawString(headers[i], fontHeader, XBrushes.White, new XRect(nextHeaderX, currentY, colWidths[i], headerHeight), format);
                                }
                                else
                                {
                                    format.Alignment = XStringAlignment.Near;
                                    gfx.DrawString(headers[i], fontHeader, XBrushes.White, new XRect(nextHeaderX + 5, currentY, colWidths[i] - 10, headerHeight), format);
                                }
                                nextHeaderX += colWidths[i];
                            }

                            currentY += headerHeight;
                        }

                        // Zebra striping for row background
                        if (rowIndex % 2 == 1)
                        {
                            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(248, 250, 252)), margin, currentY, printableWidth, rowHeight);
                        }

                        // Draw cells
                        double cellX = margin;
                        string[] cellValues = {
                            item.PartType,
                            item.Brand,
                            item.Manufacturer,
                            item.Model,
                            item.Barcode,
                            item.Quantity.ToString(),
                            item.CreatedAtLocal
                        };

                        for (int i = 0; i < cellValues.Length; i++)
                        {
                            var format = new XStringFormat { LineAlignment = XLineAlignment.Center };
                            string truncatedText = TruncateText(gfx, cellValues[i], fontBody, colWidths[i] - 10);

                            if (i == 5) // Qty column
                            {
                                format.Alignment = XStringAlignment.Center;
                                gfx.DrawString(truncatedText, fontBody, XBrushes.Black, new XRect(cellX, currentY, colWidths[i], rowHeight), format);
                            }
                            else
                            {
                                format.Alignment = XStringAlignment.Near;
                                gfx.DrawString(truncatedText, fontBody, XBrushes.Black, new XRect(cellX + 5, currentY, colWidths[i] - 10, rowHeight), format);
                            }
                            cellX += colWidths[i];
                        }

                        // Draw bottom border line for row
                        gfx.DrawLine(new XPen(XColor.FromArgb(226, 232, 240), 0.5), margin, currentY + rowHeight, pageWidth - margin, currentY + rowHeight);

                        currentY += rowHeight;
                        rowIndex++;
                    }

                    // Save document
                    document.Save(filePath);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        private string TruncateText(XGraphics gfx, string text, XFont font, double maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            var size = gfx.MeasureString(text, font);
            if (size.Width <= maxWidth) return text;

            string suffix = "...";
            double suffixWidth = gfx.MeasureString(suffix, font).Width;
            if (suffixWidth >= maxWidth) return string.Empty;

            int len = text.Length;
            while (len > 0)
            {
                string part = text.Substring(0, len) + suffix;
                if (gfx.MeasureString(part, font).Width <= maxWidth)
                {
                    return part;
                }
                len--;
            }
            return suffix;
        }
    }
}
