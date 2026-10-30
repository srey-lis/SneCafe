using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SneCafe.Data;
using SneCafe.Models;
using SneCafe.Services;

namespace SneCafe.Controllers
{
    // Only admins can open anything in this controller
    [Authorize(Roles = "Admin")]
    public class MenuController : Controller
    {
        private const int MaxRequestBytes = 5 * 1024 * 1024;   // stops huge uploads early

        private readonly MenuRepository _menu;
        private readonly ImageStorage _images;

        public MenuController(MenuRepository menu, ImageStorage images)
        {
            _menu = menu;
            _images = images;
        }

        // ---------- List ----------
        public IActionResult Index()
        {
            return View(_menu.GetAll());
        }

        // ---------- Add ----------
        public IActionResult Create()
        {
            return View("Form", Build(new ProductEditModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxRequestBytes)]
        public async Task<IActionResult> Create([Bind(Prefix = "Form")] ProductEditModel form)
        {
            Check(form);
            string? uploaded = null;

            if (ModelState.IsValid && form.ImageFile is { Length: > 0 })
            {
                var (url, error) = await _images.SaveAsync(form.ImageFile);
                if (error != null)
                {
                    ModelState.AddModelError("Form.ImageFile", error);
                }
                else
                {
                    uploaded = url;
                    form.ImageUrl = url;       // the uploaded file wins over a typed link
                }
            }

            if (!ModelState.IsValid)
                return View("Form", Build(form));

            try
            {
                _menu.Create(form);
            }
            catch
            {
                _images.DeleteIfOurs(uploaded);   // do not leave an unused file behind
                throw;
            }

            TempData["Info"] = form.Name + " was added to the menu.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Edit ----------
        public IActionResult Edit(int id)
        {
            var product = _menu.GetById(id);
            if (product == null) return NotFound();

            return View("Form", Build(product));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxRequestBytes)]
        public async Task<IActionResult> Edit([Bind(Prefix = "Form")] ProductEditModel form)
        {
            var existing = _menu.GetById(form.Id);
            if (existing == null) return NotFound();

            Check(form);
            string? uploaded = null;

            if (ModelState.IsValid)
            {
                if (form.ImageFile is { Length: > 0 })
                {
                    // 1) a new file wins over everything
                    var (url, error) = await _images.SaveAsync(form.ImageFile);
                    if (error != null)
                    {
                        ModelState.AddModelError("Form.ImageFile", error);
                    }
                    else
                    {
                        uploaded = url;
                        form.ImageUrl = url;
                    }
                }
                else if (form.RemoveImage)
                {
                    // 2) "remove the picture" was ticked
                    form.ImageUrl = null;
                }
                // 3) otherwise we keep whatever is in the link box
            }

            if (!ModelState.IsValid)
                return View("Form", Build(form));

            if (!_menu.Update(form))
            {
                _images.DeleteIfOurs(uploaded);
                return NotFound();
            }

            // The old picture is no longer used by this item
            if (existing.ImageUrl != form.ImageUrl)
                DeleteFileIfUnused(existing.ImageUrl, form.Id);

            TempData["Info"] = form.Name + " was updated.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Delete ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var product = _menu.GetById(id);
            if (product == null)
            {
                TempData["Error"] = "That item no longer exists.";
                return RedirectToAction(nameof(Index));
            }

            if (_menu.Delete(id))
            {
                DeleteFileIfUnused(product.ImageUrl, id);
                TempData["Info"] = product.Name + " was deleted.";
            }
            else
            {
                TempData["Error"] = product.Name + " has order history, so it cannot be deleted. Use Hide instead.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ---------- Hide / Show ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Toggle(int id)
        {
            _menu.ToggleAvailable(id);
            return RedirectToAction(nameof(Index));
        }

        // ---------- New category ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCategory(string? categoryName)
        {
            string name = (categoryName ?? "").Trim();

            if (name.Length < 2 || name.Length > 50 || !Regex.IsMatch(name, "^[^<>\"&`]+$"))
            {
                TempData["Error"] = "Category name must be 2 to 50 characters and cannot contain < > \" & or a backtick.";
            }
            else if (!_menu.AddCategory(name))
            {
                TempData["Error"] = "That category already exists.";
            }
            else
            {
                TempData["Info"] = "Category \"" + name + "\" was added. Now you can put items in it.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ---------- helpers ----------
        private ProductFormViewModel Build(ProductEditModel form)
        {
            return new ProductFormViewModel { Form = form, Categories = _menu.GetCategories() };
        }

        // Deletes an uploaded file, but only if no other item still uses it
        private void DeleteFileIfUnused(string? url, int exceptId)
        {
            if (!string.IsNullOrEmpty(url) && !_menu.IsImageUsed(url, exceptId))
                _images.DeleteIfOurs(url);
        }

        // Extra checks on top of the [Required] / [Range] rules in the model
        private void Check(ProductEditModel f)
        {
            f.Name = (f.Name ?? "").Trim();

            if (!MenuIcons.IsValid(f.Icon))
                ModelState.AddModelError("Form.Icon", "Choose an icon.");

            if (!_menu.GetCategories().Any(c => c.Id == f.CategoryId))
                ModelState.AddModelError("Form.CategoryId", "Choose a category.");

            if (f.Name.Length > 0 && _menu.NameExists(f.Name, f.Id))
                ModelState.AddModelError("Form.Name", "An item with this name already exists.");

            if (f.ImageUrl != null && f.ImageUrl.Contains(".."))
                ModelState.AddModelError("Form.ImageUrl", "That picture link is not allowed.");
        }
    }
}