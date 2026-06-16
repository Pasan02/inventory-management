using inventory_management.Data.Entities;
using inventory_management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;

        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItems()
        {
            var items = await _stockService.GetItemsAsync();
            return Ok(items);
        }

        [HttpGet("barcode/{barcode}")]
        public async Task<IActionResult> FindItemByBarcode(string barcode)
        {
            var item = await _stockService.FindItemByBarcodeAsync(barcode);
            if (item == null)
            {
                return NotFound(new { message = "Item not found" });
            }
            return Ok(item);
        }

        [HttpGet("search/{searchText}")]
        public async Task<IActionResult> FindItemByBarcodeOrName(string searchText)
        {
            var item = await _stockService.FindItemByBarcodeOrNameAsync(searchText);
            if (item == null)
            {
                return NotFound(new { message = "Item not found" });
            }
            return Ok(item);
        }

        public class StockOperationRequest
        {
            public string Barcode { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }

        public class AddStockWithPriceRequest : StockOperationRequest
        {
            public string SecretPriceCode { get; set; } = string.Empty;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddStock([FromBody] StockOperationRequest request)
        {
            var result = await _stockService.AddStockAsync(request.Barcode, request.Quantity);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            return Ok(result);
        }

        [HttpPost("add-with-price")]
        public async Task<IActionResult> AddStockWithPrice([FromBody] AddStockWithPriceRequest request)
        {
            var result = await _stockService.AddStockWithPriceAsync(request.Barcode, request.Quantity, request.SecretPriceCode);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            return Ok(result);
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveStock([FromBody] StockOperationRequest request)
        {
            var result = await _stockService.RemoveStockAsync(request.Barcode, request.Quantity, isReplacement: false);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            return Ok(result);
        }
        
        [HttpPost("orders/{orderId}/arrive")]
        public async Task<IActionResult> MarkOrderAsArrived(int orderId)
        {
            await _stockService.MarkOrderAsArrivedAsync(orderId);
            return Ok(new { message = "Order marked as arrived successfully" });
        }
    }
}
