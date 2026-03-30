using EventPlatformAPI.Web.Data;
using EventPlatformAPI.Web.Domains;
using EventPlatformAPI.Web.ViewModels.EventLecturers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.Web.Controllers
{
    public class EventLecturersController : Controller
    {
        private readonly PlatformDbContext _context;

        public EventLecturersController(PlatformDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var eventLecturers = await _context.EventLecturers
                .Include(e => e.Event)
                .Include(e => e.Lecturer)
                .OrderBy(e => e.DateTime)
                .Select(e => new EventLecturerViewModel
                {
                    Id = e.Id,
                    EventId = e.EventId,
                    LecturerId = e.LecturerId,
                    DateTime = e.DateTime,
                    Theme = e.Theme,
                    EventName = e.Event != null ? e.Event.Name : string.Empty,
                    LecturerFullName = e.Lecturer != null ? e.Lecturer.FirstName + " " + e.Lecturer.LastName : string.Empty
                })
                .ToListAsync();

            return View(eventLecturers);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventLecturer = await _context.EventLecturers
                .Include(e => e.Event)
                .Include(e => e.Lecturer)
                .Where(m => m.Id == id)
                .Select(e => new EventLecturerViewModel
                {
                    Id = e.Id,
                    EventId = e.EventId,
                    LecturerId = e.LecturerId,
                    DateTime = e.DateTime,
                    Theme = e.Theme,
                    EventName = e.Event != null ? e.Event.Name : string.Empty,
                    LecturerFullName = e.Lecturer != null ? e.Lecturer.FirstName + " " + e.Lecturer.LastName : string.Empty
                })
                .FirstOrDefaultAsync();

            if (eventLecturer == null)
            {
                return NotFound();
            }

            return View(eventLecturer);
        }

        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventLecturerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model.EventId, model.LecturerId);
                return View(model);
            }

            var eventLecturer = new EventLecturer
            {
                EventId = model.EventId,
                LecturerId = model.LecturerId,
                DateTime = model.DateTime,
                Theme = model.Theme
            };

            _context.Add(eventLecturer);
            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Veza za isti događaj, predavača i termin već postoji.");
                PopulateDropdowns(model.EventId, model.LecturerId);
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventLecturer = await _context.EventLecturers.FindAsync(id);
            if (eventLecturer == null)
            {
                return NotFound();
            }

            var model = new EventLecturerViewModel
            {
                Id = eventLecturer.Id,
                EventId = eventLecturer.EventId,
                LecturerId = eventLecturer.LecturerId,
                DateTime = eventLecturer.DateTime,
                Theme = eventLecturer.Theme
            };

            PopulateDropdowns(model.EventId, model.LecturerId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventLecturerViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model.EventId, model.LecturerId);
                return View(model);
            }

            var entity = await _context.EventLecturers.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            entity.EventId = model.EventId;
            entity.LecturerId = model.LecturerId;
            entity.DateTime = model.DateTime;
            entity.Theme = model.Theme;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.EventLecturers.Any(e => e.Id == model.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Veza za isti događaj, predavača i termin već postoji.");
                PopulateDropdowns(model.EventId, model.LecturerId);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventLecturer = await _context.EventLecturers
                .Include(e => e.Event)
                .Include(e => e.Lecturer)
                .Where(m => m.Id == id)
                .Select(e => new EventLecturerViewModel
                {
                    Id = e.Id,
                    EventId = e.EventId,
                    LecturerId = e.LecturerId,
                    DateTime = e.DateTime,
                    Theme = e.Theme,
                    EventName = e.Event != null ? e.Event.Name : string.Empty,
                    LecturerFullName = e.Lecturer != null ? e.Lecturer.FirstName + " " + e.Lecturer.LastName : string.Empty
                })
                .FirstOrDefaultAsync();

            if (eventLecturer == null)
            {
                return NotFound();
            }

            return View(eventLecturer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventLecturer = await _context.EventLecturers.FindAsync(id);
            if (eventLecturer != null)
            {
                _context.EventLecturers.Remove(eventLecturer);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private void PopulateDropdowns(int? eventId = null, int? lecturerId = null)
        {
            ViewData["EventId"] = new SelectList(_context.Events.OrderBy(e => e.Name), "Id", "Name", eventId);
            ViewData["LecturerId"] = new SelectList(
                _context.Lecturers
                    .OrderBy(l => l.LastName)
                    .ThenBy(l => l.FirstName)
                    .Select(l => new { l.Id, FullName = l.FirstName + " " + l.LastName }),
                "Id",
                "FullName",
                lecturerId);
        }
    }
}
