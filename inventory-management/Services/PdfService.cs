using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using inventory_management.ViewModels;
using PdfiumViewer;
using System.Drawing.Printing;

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
                    var document = new PdfSharpCore.Pdf.PdfDocument();
                    document.Info.Title = "Purchase Order Slip";
                    document.Info.Author = "Alpine Auto A/C Inventory System";
                    document.Info.Subject = "Purchase Order generated when items were moved to Ordered status";

                    // Group items by OrderedAt
                    var groupedItems = items.GroupBy(i => i.OrderedAt?.ToString("yyyy-MM-dd HH:mm") ?? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")).ToList();

                    // Fonts (Standard WPF/Segoe UI)
                    var fontTitle = new XFont("Segoe UI", 18, XFontStyle.Bold);
                    var fontHeader = new XFont("Segoe UI", 9, XFontStyle.Bold);
                    var fontBody = new XFont("Segoe UI", 9, XFontStyle.Regular);
                    var fontFooter = new XFont("Segoe UI", 8, XFontStyle.Italic);
                    var fontSubtitle = new XFont("Segoe UI", 11, XFontStyle.Regular);

                    int pageNumber = 1;
                    double margin = 40;
                    var page = document.AddPage();
                    page.Size = PageSize.A4;
                    XGraphics gfx = XGraphics.FromPdfPage(page);
                    double pageWidth = page.Width.Point;
                    double printableWidth = pageWidth - 2 * margin;
                    double bottomMargin = page.Height.Point - margin;
                    double currentY = margin;

                    // 2. Draw page header text
                    gfx.DrawString($"Page {pageNumber}", fontFooter, XBrushes.DimGray, new XPoint(pageWidth - margin, margin - 5), new XStringFormat { Alignment = XStringAlignment.Far });

                    string printedOnText = $"Printed on: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
                    gfx.DrawString(printedOnText, fontFooter, XBrushes.LightGray, new XPoint(margin, bottomMargin + 10));

                    currentY += 20;

                    // Draw Title and Info on first page
                    gfx.DrawString("ALPINE AUTO A/C", fontHeader, XBrushes.Black, new XPoint(margin, currentY));
                    gfx.DrawString("PURCHASE ORDER SLIP", fontTitle, XBrushes.Black, new XRect(margin, currentY + 5, printableWidth, 30), XStringFormats.TopLeft);
                    currentY += 40;

                    bool isFirstGroup = true;

                    foreach (var group in groupedItems)
                    {
                        var groupItems = group.ToList();
                        string orderTimeText = group.First().OrderedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        int totalQuantity = groupItems.Sum(i => i.Quantity);

                        // Check if we need a new page for the group header (need about 80 height)
                        if (currentY + 80 > bottomMargin)
                        {
                            pageNumber++;
                            page = document.AddPage();
                            page.Size = PageSize.A4;
                            gfx = XGraphics.FromPdfPage(page);

                            gfx.DrawString($"Page {pageNumber}", fontFooter, XBrushes.DimGray, new XPoint(pageWidth - margin, margin - 5), new XStringFormat { Alignment = XStringAlignment.Far });
                            gfx.DrawString(printedOnText, fontFooter, XBrushes.LightGray, new XPoint(margin, bottomMargin + 10));

                            currentY = margin + 20;

                            gfx.DrawString("ALPINE AUTO A/C", fontHeader, XBrushes.Black, new XPoint(margin, currentY));
                            gfx.DrawString("PURCHASE ORDER SLIP (Continued)", fontTitle, XBrushes.Black, new XRect(margin, currentY + 5, printableWidth, 30), XStringFormats.TopLeft);
                            currentY += 40;

                            isFirstGroup = false; // Prevent separator on new page
                        }
                        else if (!isFirstGroup)
                        {
                            currentY += 15;
                            gfx.DrawLine(new XPen(XColor.FromArgb(148, 163, 184), 1.5) { DashStyle = XDashStyle.Dash }, margin, currentY, pageWidth - margin, currentY);
                            currentY += 15;
                        }

                        string dateText = $"Placed Date/Time: {orderTimeText}";
                        string summaryText = $"Total Items ordered: {groupItems.Count}  |  Total Qty: {totalQuantity}";
                        gfx.DrawString(dateText, fontSubtitle, XBrushes.Black, new XPoint(margin, currentY + 5));
                        gfx.DrawString(summaryText, fontSubtitle, XBrushes.Black, new XPoint(margin, currentY + 23));

                        currentY += 40;

                        // 5. Draw table headers
                        double headerHeight = 22;
                        if (currentY + headerHeight > bottomMargin)
                        {
                            pageNumber++;
                            page = document.AddPage();
                            page.Size = PageSize.A4;
                            gfx = XGraphics.FromPdfPage(page);

                            gfx.DrawString($"Page {pageNumber}", fontFooter, XBrushes.DimGray, new XPoint(pageWidth - margin, margin - 5), new XStringFormat { Alignment = XStringAlignment.Far });
                            gfx.DrawString(printedOnText, fontFooter, XBrushes.LightGray, new XPoint(margin, bottomMargin + 10));
                            
                            currentY = margin + 20;

                            gfx.DrawString("ALPINE AUTO A/C", fontHeader, XBrushes.Black, new XPoint(margin, currentY));
                            gfx.DrawString("PURCHASE ORDER SLIP (Continued)", fontTitle, XBrushes.Black, new XRect(margin, currentY + 5, printableWidth, 30), XStringFormats.TopLeft);
                            currentY += 40;
                        }

                        string[] headers = { "Type", "Brand", "Manufacturer", "Model", "Barcode", "Qty", "Date Removed" };
                        double[] colWidths = { 60, 60, 80, 80, 80, 40, 130 };

                        double headerX = margin;
                        for (int i = 0; i < headers.Length; i++)
                        {
                            gfx.DrawRectangle(new XPen(XColor.FromArgb(0, 0, 0), 0.5), headerX, currentY, colWidths[i], headerHeight);

                            var format = new XStringFormat { LineAlignment = XLineAlignment.Center };
                            if (headers[i] == "Qty")
                            {
                                format.Alignment = XStringAlignment.Center;
                                gfx.DrawString(headers[i], fontHeader, XBrushes.Black, new XRect(headerX, currentY, colWidths[i], headerHeight), format);
                            }
                            else
                            {
                                format.Alignment = XStringAlignment.Near;
                                gfx.DrawString(headers[i], fontHeader, XBrushes.Black, new XRect(headerX + 5, currentY, colWidths[i] - 10, headerHeight), format);
                            }
                            headerX += colWidths[i];
                        }

                        currentY += headerHeight;

                        // 6. Draw Table Rows
                        double rowHeight = 20;

                        foreach (var item in groupItems)
                        {
                            // Check if we need to paginate
                            if (currentY + rowHeight > bottomMargin)
                            {
                                pageNumber++;
                                page = document.AddPage();
                                page.Size = PageSize.A4;
                                gfx = XGraphics.FromPdfPage(page);

                                gfx.DrawString($"Page {pageNumber}", fontFooter, XBrushes.DimGray, new XPoint(pageWidth - margin, margin - 5), new XStringFormat { Alignment = XStringAlignment.Far });
                                gfx.DrawString(printedOnText, fontFooter, XBrushes.LightGray, new XPoint(margin, bottomMargin + 10));

                                currentY = margin + 20;

                                gfx.DrawString("ALPINE AUTO A/C", fontHeader, XBrushes.Black, new XPoint(margin, currentY));
                                gfx.DrawString("PURCHASE ORDER SLIP (Continued)", fontTitle, XBrushes.Black, new XRect(margin, currentY + 5, printableWidth, 30), XStringFormats.TopLeft);
                                currentY += 40;

                                double nextHeaderX = margin;
                                for (int i = 0; i < headers.Length; i++)
                                {
                                    gfx.DrawRectangle(new XPen(XColor.FromArgb(0, 0, 0), 0.5), nextHeaderX, currentY, colWidths[i], headerHeight);

                                    var format = new XStringFormat { LineAlignment = XLineAlignment.Center };
                                    if (headers[i] == "Qty")
                                    {
                                        format.Alignment = XStringAlignment.Center;
                                        gfx.DrawString(headers[i], fontHeader, XBrushes.Black, new XRect(nextHeaderX, currentY, colWidths[i], headerHeight), format);
                                    }
                                    else
                                    {
                                        format.Alignment = XStringAlignment.Near;
                                        gfx.DrawString(headers[i], fontHeader, XBrushes.Black, new XRect(nextHeaderX + 5, currentY, colWidths[i] - 10, headerHeight), format);
                                    }
                                    nextHeaderX += colWidths[i];
                                }

                                currentY += headerHeight;
                            }

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
                                gfx.DrawRectangle(new XPen(XColor.FromArgb(0, 0, 0), 0.5), cellX, currentY, colWidths[i], rowHeight);

                                var format = new XStringFormat { LineAlignment = XLineAlignment.Center };
                                string truncatedText = TruncateText(gfx, cellValues[i], fontBody, colWidths[i] - 10);

                                if (i == 5)
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

                            currentY += rowHeight;
                        }
                        
                        isFirstGroup = false;
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

        public Task<bool> PrintOrderPdfSilentlyAsync(string filePath)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(filePath)) return false;

                    using (var document = PdfiumViewer.PdfDocument.Load(filePath))
                    {
                        using (var printDocument = document.CreatePrintDocument())
                        {
                            printDocument.PrinterSettings.PrintToFile = false;
                            // Use StandardPrintController to avoid the "Printing..." popup dialog
                            printDocument.PrintController = new StandardPrintController(); 
                            printDocument.Print();
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error printing PDF silently: {ex.Message}");
                    return false;
                }
            });
        }
    }
}
