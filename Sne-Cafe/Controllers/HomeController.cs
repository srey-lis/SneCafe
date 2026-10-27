using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sne_Cafe.Models;
using SneCafe.Data;
using SneCafe.Models;

namespace SneCafe.Controllers
{
    public class HomeController : Controller
    {
        private readonly OrderListRepository _orders;

        public HomeController(OrderListRepository orders)
        {
            _orders = orders;
        }

        // The Orders page. /?range=all shows every order, otherwise today only.
        public IActionResult Index(string? range)
        {
            bool all = range == "all";

            var vm = new OrdersViewModel
            {
                Orders = _orders.GetOrders(all),
                Range = all ? "all" : "today"
            };

            return View(vm);
        }

        // Called by the Start / Complete / Cancel buttons
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetStatus(int id, string status, string? range)
        {
            if (!_orders.ChangeStatus(id, status))
            {
                TempData["Error"] = "That order could not be changed. It may have been updated already.";
            }

            return RedirectToAction(nameof(Index), new { range });
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}