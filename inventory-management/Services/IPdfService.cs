using System.Collections.Generic;
using System.Threading.Tasks;
using inventory_management.ViewModels;

namespace inventory_management.Services
{
    public interface IPdfService
    {
        Task<bool> GenerateOrderPdfAsync(string filePath, List<ReportOrderRow> items);
        Task<bool> PrintOrderPdfSilentlyAsync(string filePath);
    }
}
