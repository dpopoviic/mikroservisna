using EventPlatformAPI.Web.Data;
using EventPlatformAPI.Web.Domains;
using EventPlatformAPI.Web.ViewModels.Locations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.Web.Controllers
{
    public class LocationsController : Controller
    {
        private readonly PlatformDbContext _context;

        public LocationsController(PlatformDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var models = await _context.Locations
                .OrderBy(l => l.Name)
                .Select(l => new LocationViewModel
                {
                    Id = l.Id,
                    Name = l.Name,
                    Address = l.Address,
                    Capacity = l.Capacity
                })
                .ToListAsync();

            return View(models);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _context.Locations
                .Where(m => m.Id == id)
                .Select(m => new LocationViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    Address = m.Address,
                    Capacity = m.Capacity
                })
                .FirstOrDefaultAsync();

            if (location == null)
            {
                return NotFound();
            }

            return View(location);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LocationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var location = new Location
            {
                Name = model.Name,
                Address = model.Address,
                Capacity = model.Capacity
            };

            _context.Add(location);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                return NotFound();
            }

            var model = new LocationViewModel
            {
                Id = location.Id,
                Name = location.Name,
                Address = location.Address,
                Capacity = location.Capacity
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LocationViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                return NotFound();
            }

            location.Name = model.Name;
            location.Address = model.Address;
            location.Capacity = model.Capacity;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Locations.Any(e => e.Id == model.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _context.Locations
                .Where(m => m.Id == id)
                .Select(m => new LocationViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    Address = m.Address,
                    Capacity = m.Capacity
                })
                .FirstOrDefaultAsync();

            if (location == null)
            {
                return NotFound();
            }

            return View(location);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location != null)
            {
                _context.Locations.Remove(location);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
