using EventPlatformAPI.DTO;
using EventPlatformAPI.Web.Services;
using EventPlatformAPI.Web.ViewModels.Lecturers;
using Microsoft.AspNetCore.Mvc;
using Polly.Timeout;

namespace EventPlatformAPI.Web.Controllers
{
    public class LecturersController : Controller
    {
        private readonly IReferencesApiClient _referencesApiClient;

        public LecturersController(IReferencesApiClient referencesApiClient)
        {
            _referencesApiClient = referencesApiClient;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var models = (await _referencesApiClient.GetLecturersAsync())
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
                    .ToList();

                return View(models);
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View(new List<LecturerViewModel>());
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                var lecturer = await _referencesApiClient.GetLecturerByIdAsync(id.Value);
                if (lecturer == null)
                {
                    return NotFound();
                }

                return View(new LecturerViewModel
                {
                    Id = lecturer.Id,
                    FirstName = lecturer.FirstName,
                    LastName = lecturer.LastName,
                    Title = lecturer.Title,
                    Field = lecturer.Field
                });
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View();
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LecturerViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var ok = await _referencesApiClient.CreateLecturerAsync(new LecturerDto
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Title = model.Title,
                    Field = model.Field
                });

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "Greška pri ?uvanju predava?a.");
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                var lecturer = await _referencesApiClient.GetLecturerByIdAsync(id.Value);
                if (lecturer == null)
                {
                    return NotFound();
                }

                return View(new LecturerViewModel
                {
                    Id = lecturer.Id,
                    FirstName = lecturer.FirstName,
                    LastName = lecturer.LastName,
                    Title = lecturer.Title,
                    Field = lecturer.Field
                });
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LecturerViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    return NotFound();
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var ok = await _referencesApiClient.UpdateLecturerAsync(id, new LecturerDto
                {
                    Id = model.Id,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Title = model.Title,
                    Field = model.Field
                });

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "Greška pri izmeni predava?a.");
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                var lecturer = await _referencesApiClient.GetLecturerByIdAsync(id.Value);
                if (lecturer == null)
                {
                    return NotFound();
                }

                return View(new LecturerViewModel
                {
                    Id = lecturer.Id,
                    FirstName = lecturer.FirstName,
                    LastName = lecturer.LastName,
                    Title = lecturer.Title,
                    Field = lecturer.Field
                });
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View();
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _referencesApiClient.DeleteLecturerAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
