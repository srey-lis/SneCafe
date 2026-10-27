using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SneCafe.Data;
using SneCafe.Models;

namespace SneCafe.Controllers
{
    public class OrderController : Controller
    {
        private readonly ProductRepository _products;
        private readonly OrderRepository _orders;
        private readonly ReceiptRepository _receipts;

        public OrderController(ProductRepository products, OrderRepository orders, ReceiptRepository receipts)
        {
            _products = products;
            _orders = orders;
            _receipts = receipts;
        }

        // Shows the New Order screen
        public IActionResult Create()
        {
            return View(_products.GetAvailable());
        }

        // Receives the cart from the browser (JSON) and saves it
        [HttpPost]
        public IActionResult Save([FromBody] OrderRequest request)
        {
            if (request == null || request.Items.Count == 0)
                return BadRequest(new { message = "The cart is empty." });

            // The logged-in staff member's Id comes from the login cookie
            int? staffId = null;
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id))
                staffId = id;

            try
            {
                var (orderId, orderNo) = _orders.Create(request, staffId);
                return Json(new { success = true, orderId = orderId, orderNo = orderNo });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Shows the receipt page: /Order/Receipt/5
        public IActionResult Receipt(int id)
        {
            var receipt = _receipts.GetReceipt(id);
            if (receipt == null)
                return NotFound();

            return View(receipt);
        }
    }
}