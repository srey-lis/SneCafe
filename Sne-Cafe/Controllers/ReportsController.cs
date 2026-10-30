using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sne_Cafe.Data;
using SneCafe.Data;
using System.Globalization;

namespace SneCafe.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ReportRepository _reports;

        public ReportsController(ReportRepository reports)
        {
            _reports = reports;
        }

        // /Reports                        last 7 days
        // /Reports?preset=30              last 30 days
        // /Reports?preset=month           this month
        // /Reports?from=2026-10-01&to=2026-10-15   custom dates
        public IActionResult Index(string? preset, string? from, string? to)
        {
            DateTime today = DateTime.Today;
            DateTime start, end;
            string active;

            if (preset == "30")
            {
                start = today.AddDays(-29); end = today; active = "30";
            }
            else if (preset == "month")
            {
                start = new DateTime(today.Year, today.Month, 1); end = today; active = "month";
            }
            else if (TryDate(from, out var f) && TryDate(to, out var t))
            {
                start = f; end = t; active = "custom";
            }
            else
            {
                start = today.AddDays(-6); end = today; active = "7";
            }

            // Keep the dates sensible
            if (end > today) end = today;
            if (start > end) start = end;
            if ((end - start).TotalDays > 365) start = end.AddDays(-365);

            return View(_reports.Build(start, end, active));
        }

        private static bool TryDate(string? text, out DateTime date)
        {
            return DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                          DateTimeStyles.None, out date);
        }
    }
}