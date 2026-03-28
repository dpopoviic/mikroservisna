using EventPlatformAPI.Web.Data;
using EventPlatformAPI.Web.Domains;
using EventPlatformAPI.Web.ViewModels.Lecturers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.Web.Controllers
{
    public class LecturersController : Controller
    {
        private readonly PlatformDbContext _context;

        public LecturersController(PlatformDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var models = await _context.Lecturers
                .OrderBy(l => l.LastName)
                .ThenBy(l => l.FirstName)
                .Select(l => new LecturerViewModel
                {
                    Id = l.Id,
                    FirstName = l.FirstName,
                    LastName = l.LastName,
                    Title = l.Title,
                    Field = l.Field
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

            var lecturer = await _context.Lecturers
                .Where(m => m.Id == id)
                .Select(m => new LecturerViewModel
                {
                    Id = m.Id,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Title = m.Title,
                    Field = m.Field
                })
                .FirstOrDefaultAsync();

            if (lecturer == null)
            {
                return NotFound();
            }

            return View(lecturer);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LecturerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var lecturer = new Lecturer
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Title = model.Title,
                Field = model.Field
            };

            _context.Add(lecturer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer == null)
            {
                return NotFound();
            }

            var model = new LecturerViewModel
            {
                Id = lecturer.Id,
                FirstName = lecturer.FirstName,
                LastName = lecturer.LastName,
                Title = lecturer.Title,
                Field = lecturer.Field
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LecturerViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer == null)
            {
                return NotFound();
            }

            lecturer.FirstName = model.FirstName;
            lecturer.LastName = model.LastName;
            lecturer.Title = model.Title;
            lecturer.Field = model.Field;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Lecturers.Any(e => e.Id == model.Id))
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

            var lecturer = await _context.Lecturers
                .Where(m => m.Id == id)
                .Select(m => new LecturerViewModel
                {
                    Id = m.Id,
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Title = m.Title,
                    Field = m.Field
                })
                .FirstOrDefaultAsync();

            if (lecturer == null)
            {
                return NotFound();
            }

            return View(lecturer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer != null)
            {
                _context.Lecturers.Remove(lecturer);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
