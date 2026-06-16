using inventory_management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PrintController : ControllerBase
    {
        private readonly IPrintService _printService;

        public PrintController(IPrintService printService)
        {
            _printService = printService;
        }

        public class PrintRequest
        {
            public string Barcode { get; set; } = string.Empty;
            public int Copies { get; set; } = 1;
        }

        [HttpPost("barcode")]
        public async Task<IActionResult> PrintBarcode([FromBody] PrintRequest request)
        {
            try
            {
                bool success = await _printService.PrintBarcodeLabelAsync(request.Barcode, request.Copies);
                if (success) {
                    return Ok(new { message = "Print job successfully sent to local printer" });
                } else {
                    return StatusCode(500, new { message = "Failed to print label" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to print: {ex.Message}" });
            }
        }
    }
}
