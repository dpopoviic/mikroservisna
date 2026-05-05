using EventPlatformAPI.DTO;
using EventPlatformAPI.Web.Services;
using EventPlatformAPI.Web.ViewModels.Locations;
using Microsoft.AspNetCore.Mvc;
using Polly.Timeout;

namespace EventPlatformAPI.Web.Controllers
{
    public class LocationsController : Controller
    {
        private readonly IReferencesApiClient _referencesApiClient;

        public LocationsController(IReferencesApiClient referencesApiClient)
        {
            _referencesApiClient = referencesApiClient;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var models = (await _referencesApiClient.GetLocationsAsync())
                    .OrderBy(l => l.Name)
                    .Select(l => new LocationViewModel
                    {
                        Id = l.Id,
                        Name = l.Name,
                        Address = l.Address,
                        Capacity = l.Capacity
                    })
                    .ToList();

                return View(models);
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View(new List<LocationViewModel>());
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

                var location = await _referencesApiClient.GetLocationByIdAsync(id.Value);
                if (location == null)
                {
                    return NotFound();
                }

                return View(new LocationViewModel
                {
                    Id = location.Id,
                    Name = location.Name,
                    Address = location.Address,
                    Capacity = location.Capacity
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
        public async Task<IActionResult> Create(LocationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var ok = await _referencesApiClient.CreateLocationAsync(new LocationDto
                {
                    Name = model.Name,
                    Address = model.Address,
                    Capacity = model.Capacity
                });

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "Greška pri ?uvanju lokacije.");
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

                var location = await _referencesApiClient.GetLocationByIdAsync(id.Value);
                if (location == null)
                {
                    return NotFound();
                }

                return View(new LocationViewModel
                {
                    Id = location.Id,
                    Name = location.Name,
                    Address = location.Address,
                    Capacity = location.Capacity
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
        public async Task<IActionResult> Edit(int id, LocationViewModel model)
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

                var ok = await _referencesApiClient.UpdateLocationAsync(id, new LocationDto
                {
                    Id = model.Id,
                    Name = model.Name,
                    Address = model.Address,
                    Capacity = model.Capacity
                });

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "Greška pri izmeni lokacije.");
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

                var location = await _referencesApiClient.GetLocationByIdAsync(id.Value);
                if (location == null)
                {
                    return NotFound();
                }

                return View(new LocationViewModel
                {
                    Id = location.Id,
                    Name = location.Name,
                    Address = location.Address,
                    Capacity = location.Capacity
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
                await _referencesApiClient.DeleteLocationAsync(id);
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
