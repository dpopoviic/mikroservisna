using EventPlatformAPI.Web.Data;
using EventPlatformAPI.Web.Domains;
using EventPlatformAPI.Web.ViewModels.EventTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.Web.Controllers
{
    public class EventTypesController : Controller
    {
        private readonly PlatformDbContext _context;

        public EventTypesController(PlatformDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var models = await _context.EventTypes
                .OrderBy(t => t.Name)
                .Select(t => new EventTypeViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description
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

            var eventType = await _context.EventTypes
                .Where(m => m.Id == id)
                .Select(m => new EventTypeViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description
                })
                .FirstOrDefaultAsync();

            if (eventType == null)
            {
                return NotFound();
            }

            return View(eventType);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var eventType = new EventType
            {
                Name = model.Name,
                Description = model.Description
            };

            _context.Add(eventType);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventType = await _context.EventTypes.FindAsync(id);
            if (eventType == null)
            {
                return NotFound();
            }

            var model = new EventTypeViewModel
            {
                Id = eventType.Id,
                Name = eventType.Name,
                Description = eventType.Description
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventTypeViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var eventType = await _context.EventTypes.FindAsync(id);
            if (eventType == null)
            {
                return NotFound();
            }

            eventType.Name = model.Name;
            eventType.Description = model.Description;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.EventTypes.Any(e => e.Id == model.Id))
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

            var eventType = await _context.EventTypes
                .Where(m => m.Id == id)
                .Select(m => new EventTypeViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description
                })
                .FirstOrDefaultAsync();

            if (eventType == null)
            {
                return NotFound();
            }

            return View(eventType);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventType = await _context.EventTypes.FindAsync(id);
            if (eventType != null)
            {
                _context.EventTypes.Remove(eventType);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
