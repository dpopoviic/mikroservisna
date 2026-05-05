using EventPlatformAPI.DTO;
using EventPlatformAPI.Web.Patterns;
using EventPlatformAPI.Web.Services;
using EventPlatformAPI.Web.ViewModels.Locations;
using Microsoft.AspNetCore.Mvc;
using Polly.Timeout;

namespace EventPlatformAPI.Web.Controllers
{
    public class LocationsController : Controller
    {
        private readonly IReferencesApiClient _referencesApiClient;
        private readonly CircuitBreaker _circuitBreaker;

        public LocationsController(IReferencesApiClient referencesApiClient, CircuitBreaker circuitBreaker)
        {
            _referencesApiClient = referencesApiClient;
            _circuitBreaker = circuitBreaker;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var models = (await ExecuteWithCircuitBreakerAsync(() => _referencesApiClient.GetLocationsAsync()))
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
            catch (CircuitBreakerOpenException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan.");
                return View(new List<LocationViewModel>());
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

                var location = await ExecuteWithCircuitBreakerAsync(() => _referencesApiClient.GetLocationByIdAsync(id.Value));
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
            catch (CircuitBreakerOpenException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan.");
                return View();
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

                var ok = await ExecuteWithCircuitBreakerAsync(() => _referencesApiClient.CreateLocationAsync(new LocationDto
                {
                    Name = model.Name,
                    Address = model.Address,
                    Capacity = model.Capacity
                }));

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "Greška pri ?uvanju lokacije.");
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (CircuitBreakerOpenException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan.");
                return View(model);
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

                var location = await ExecuteWithCircuitBreakerAsync(() => _referencesApiClient.GetLocationByIdAsync(id.Value));
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
            catch (CircuitBreakerOpenException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan.");
                return View();
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

                var ok = await ExecuteWithCircuitBreakerAsync(() => _referencesApiClient.UpdateLocationAsync(id, new LocationDto
                {
                    Id = model.Id,
                    Name = model.Name,
                    Address = model.Address,
                    Capacity = model.Capacity
                }));

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "Greška pri izmeni lokacije.");
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (CircuitBreakerOpenException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan.");
                return View(model);
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

                var location = await ExecuteWithCircuitBreakerAsync(() => _referencesApiClient.GetLocationByIdAsync(id.Value));
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
            catch (CircuitBreakerOpenException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan.");
                return View();
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
                await ExecuteWithCircuitBreakerAsync(() => _referencesApiClient.DeleteLocationAsync(id));
                return RedirectToAction(nameof(Index));
            }
            catch (CircuitBreakerOpenException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan.");
                return RedirectToAction(nameof(Index));
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return RedirectToAction(nameof(Index));
            }
        }

        private Task<T> ExecuteWithCircuitBreakerAsync<T>(Func<Task<T>> action)
        {
            return _circuitBreaker.ExecuteAsync(action);
        }
    }
}
