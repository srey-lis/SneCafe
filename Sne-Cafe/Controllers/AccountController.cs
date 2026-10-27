using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SneCafe.Data;
using SneCafe.Models;

namespace SneCafe.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserRepository _users;
        private readonly PasswordHasher<AppUser> _hasher = new();

        public AccountController(UserRepository users)
        {
            _users = users;
        }

        // ---------- Login ----------
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl)
        {
            // No accounts yet? Go create the first one.
            if (_users.Count() == 0)
                return RedirectToAction(nameof(Setup));

            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _users.GetByUsername(model.Username.Trim());
            bool ok = false;

            if (user != null && user.IsActive)
            {
                var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
                ok = result != PasswordVerificationResult.Failed;
            }

            // Same message for "no such user" and "wrong password" on purpose
            if (!ok)
            {
                ViewBag.Error = "Wrong username or password.";
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user!.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        // ---------- Logout ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // ---------- First-time setup (works only while there are no users) ----------
        [AllowAnonymous]
        public IActionResult Setup()
        {
            if (_users.Count() > 0)
                return RedirectToAction(nameof(Login));

            return View(new NewStaffViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult Setup(NewStaffViewModel model)
        {
            if (_users.Count() > 0)
                return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
                return View(model);

            CreateUser(model.FullName, model.Username, model.Password, "Admin");

            TempData["Info"] = "Admin account created. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        // ---------- Staff management (Admin only) ----------
        [Authorize(Roles = "Admin")]
        public IActionResult Staff()
        {
            return View(BuildPage(new NewStaffViewModel()));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult AddStaff([Bind(Prefix = "Form")] NewStaffViewModel form)
        {
            if (form.Role != "Staff" && form.Role != "Admin")
                ModelState.AddModelError("Form.Role", "Choose a role.");

            if (ModelState.IsValid && _users.UsernameExists(form.Username.Trim()))
                ModelState.AddModelError("Form.Username", "That username is already taken.");

            if (!ModelState.IsValid)
                return View("Staff", BuildPage(form));

            CreateUser(form.FullName, form.Username, form.Password, form.Role);

            TempData["Info"] = "Account for " + form.FullName.Trim() + " created.";
            return RedirectToAction(nameof(Staff));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleActive(int id)
        {
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int myId);

            if (id == myId)
                TempData["Error"] = "You cannot disable your own account.";
            else
                _users.ToggleActive(id);

            return RedirectToAction(nameof(Staff));
        }

        public IActionResult Denied()
        {
            return View();
        }

        // ---------- helpers ----------
        private StaffPageViewModel BuildPage(NewStaffViewModel form)
        {
            return new StaffPageViewModel { Users = _users.GetAll(), Form = form };
        }

        private void CreateUser(string fullName, string username, string password, string role)
        {
            // The password is turned into a salted hash. The password itself is never stored.
            string hash = _hasher.HashPassword(new AppUser(), password);
            _users.Create(fullName.Trim(), username.Trim(), hash, role);
        }
    }
}