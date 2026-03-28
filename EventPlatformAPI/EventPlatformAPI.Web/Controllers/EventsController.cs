using EventPlatformAPI.Web.Data;
using EventPlatformAPI.Web.Domains;
using EventPlatformAPI.Web.ViewModels.Events;
using EventPlatformAPI.Web.ViewModels.EventLecturers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.Web.Controllers
{
    public class EventsController : Controller
    {
        private readonly PlatformDbContext _context;

        public EventsController(PlatformDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Include(e => e.Type)
                .Include(e => e.Location)
                .OrderBy(e => e.DateTime)
                .Select(e => new EventViewModel
                {
                    Id = e.Id,
                    Name = e.Name,
                    DateTime = e.DateTime,
                    DurationInHours = e.DurationInHours,
                    Price = e.Price,
                    Agenda = e.Agenda,
                    TypeId = e.TypeId,
                    LocationId = e.LocationId,
                    TypeName = e.Type != null ? e.Type.Name : string.Empty,
                    LocationName = e.Location != null ? e.Location.Name : string.Empty
                })
                .ToListAsync();

            return View(events);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var model = await _context.Events
                .Include(e => e.Type)
                .Include(e => e.Location)
                .Include(e => e.EventLecturers)
                    .ThenInclude(el => el.Lecturer)
                .Where(m => m.Id == id)
                .Select(e => new EventViewModel
                {
                    Id = e.Id,
                    Name = e.Name,
                    DateTime = e.DateTime,
                    DurationInHours = e.DurationInHours,
                    Price = e.Price,
                    Agenda = e.Agenda,
                    TypeId = e.TypeId,
                    LocationId = e.LocationId,
                    TypeName = e.Type != null ? e.Type.Name : string.Empty,
                    LocationName = e.Location != null ? e.Location.Name : string.Empty,
                    EventLecturers = e.EventLecturers.Select(el => new EventLecturerViewModel
                    {
                        Id = el.Id,
                        EventId = el.EventId,
                        LecturerId = el.LecturerId,
                        DateTime = el.DateTime,
                        Theme = el.Theme,
                        LecturerFullName = el.Lecturer != null ? el.Lecturer.FirstName + " " + el.Lecturer.LastName : string.Empty
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventViewModel model)
        {
            if (model.DurationInHours <= 0)
            {
                ModelState.AddModelError(nameof(EventViewModel.DurationInHours), "Trajanje mora biti ve?e od 0.");
            }

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model.TypeId, model.LocationId);
                return View(model);
            }

            var entity = new Event
            {
                Name = model.Name,
                DateTime = model.DateTime,
                DurationInHours = model.DurationInHours,
                Price = model.Price,
                Agenda = model.Agenda,
                TypeId = model.TypeId,
                LocationId = model.LocationId
            };

            _context.Add(entity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entity = await _context.Events.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            var model = new EventViewModel
            {
                Id = entity.Id,
                Name = entity.Name,
                DateTime = entity.DateTime,
                DurationInHours = entity.DurationInHours,
                Price = entity.Price,
                Agenda = entity.Agenda,
                TypeId = entity.TypeId,
                LocationId = entity.LocationId
            };

            PopulateDropdowns(model.TypeId, model.LocationId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (model.DurationInHours <= 0)
            {
                ModelState.AddModelError(nameof(EventViewModel.DurationInHours), "Trajanje mora biti ve?e od 0.");
            }

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model.TypeId, model.LocationId);
                return View(model);
            }

            var entity = await _context.Events.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            entity.Name = model.Name;
            entity.DateTime = model.DateTime;
            entity.DurationInHours = model.DurationInHours;
            entity.Price = model.Price;
            entity.Agenda = model.Agenda;
            entity.TypeId = model.TypeId;
            entity.LocationId = model.LocationId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Events.Any(e => e.Id == model.Id))
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

            var model = await _context.Events
                .Include(e => e.Type)
                .Include(e => e.Location)
                .Where(m => m.Id == id)
                .Select(e => new EventViewModel
                {
                    Id = e.Id,
                    Name = e.Name,
                    DateTime = e.DateTime,
                    DurationInHours = e.DurationInHours,
                    Price = e.Price,
                    Agenda = e.Agenda,
                    TypeId = e.TypeId,
                    LocationId = e.LocationId,
                    TypeName = e.Type != null ? e.Type.Name : string.Empty,
                    LocationName = e.Location != null ? e.Location.Name : string.Empty
                })
                .FirstOrDefaultAsync();

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private void PopulateDropdowns(int? typeId = null, int? locationId = null)
        {
            ViewData["TypeId"] = new SelectList(_context.EventTypes.OrderBy(t => t.Name), "Id", "Name", typeId);
            ViewData["LocationId"] = new SelectList(_context.Locations.OrderBy(l => l.Name), "Id", "Name", locationId);
        }
    }
}
