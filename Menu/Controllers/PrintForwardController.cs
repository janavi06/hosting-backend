//// Controllers/PrintForwardController.cs
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Restaurant_Menu.DTOs;
//using Restaurant_Menu.Services;
//using Restaurant_Menu.Models;

//namespace Restaurant_Menu.Controllers
//{
//    [ApiController]
//    [Route("api/print")]
//    public class PrintForwardController : ControllerBase
//    {
//        private readonly ApplicationDbContext _db;
//        private readonly IPrintForwarder _forwarder;

//        public PrintForwardController(ApplicationDbContext db, IPrintForwarder forwarder)
//        {
//            _db = db;
//            _forwarder = forwarder;
//        }

//        [HttpPost("send/{orderId}/{type}")]
//        public async Task<IActionResult> SendPrintJob(string orderId, string type)
//        {
//            if (string.IsNullOrWhiteSpace(type))
//            {
//                return BadRequest("Type is required (KOT or BILL)");
//            }

//            if (!int.TryParse(orderId, out int parsedOrderId))
//            {
//                return BadRequest("Invalid Order ID format.");
//            }

//            var order = await _db.Orders
//                .Include(o => o.OrderItems)
//                .ThenInclude(i => i.Product)
//                .SingleOrDefaultAsync(o => o.OrderID == parsedOrderId);

//            if (order == null)
//            {
//                return NotFound("Order not found");
//            }

//            var rest = await _db.Restaurants.SingleOrDefaultAsync(r => r.RestaurantID == order.RestaurantID);
//            if (rest == null)
//            {
//                return BadRequest("Restaurant not found");
//            }

//            var dto = new PrintRequestDto
//            {
//                Type = type.ToUpperInvariant(),
//                KotPrinterName = rest.KotPrinterName,
//                BillPrinterName = rest.BillPrinterName,
//                RestaurantName = rest.Name,
//                RestaurantAddress = rest.Description,
//                Order = new PrintOrderDto
//                {
//                    OrderNumber = order.OrderID.ToString(),
//                    TableNo = order.RestaurantTableID?.ToString(),
//                    Items = order.OrderItems.Select(i => new PrintItemDto
//                    {
//                        Name = i.Product?.ProductName,
//                        Qty = i.Quantity,
//                        Price = i.UnitPrice,
//                        Modifiers = new System.Collections.Generic.List<string>()
//                    }).ToList(),
//                    ServiceCharge = order.ServiceCharge,
//                    Tax = (order.CGST ?? 0m) + (order.SGST ?? 0m),
//                    Discount = order.DiscountAmount,
//                    Total = order.TotalAmount,
//                    Notes = null
//                }
//            };

//            var result = await _forwarder.ForwardPrintAsync(dto, order.RestaurantID);

//            if (result.Success)
//            {
//                return Ok("Print job sent");
//            }
//            return StatusCode(500, $"Failed to send print job: {result.Message}");
//        }
//    }
//}