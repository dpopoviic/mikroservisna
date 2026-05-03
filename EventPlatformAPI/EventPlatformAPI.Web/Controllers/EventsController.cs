using EventPlatformAPI.DTO;
using EventPlatformAPI.Web.Services;
using EventPlatformAPI.Web.ViewModels.Events;
using EventPlatformAPI.Web.ViewModels.EventLecturers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace EventPlatformAPI.Web.Controllers
{
    public class EventsController : Controller
    {
        private readonly IEventsApiClient _eventsApiClient;
        private readonly IReferencesApiClient _referencesApiClient;

        public EventsController(IEventsApiClient eventsApiClient, IReferencesApiClient referencesApiClient)
        {
            _eventsApiClient = eventsApiClient;
            _referencesApiClient = referencesApiClient;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var events = await _eventsApiClient.GetEventsAsync();
                var eventTypes = await _eventsApiClient.GetEventTypesAsync();
                var locations = await _referencesApiClient.GetLocationsAsync();

                var typeLookup = eventTypes.ToDictionary(x => x.Id, x => x.Name);
                var locationLookup = locations.ToDictionary(x => x.Id, x => x.Name);

                var models = events
                    .OrderBy(e => e.DateTime)
                    .Select(e => new EventViewModel
                    {
                        Id = e.Id,
                        Name = e.Name,
                        DateTime = e.DateTime,
                        DurationInHours = e.DurationInHours,
                        Price = e.Price,
                        TypeId = e.TypeId,
                        LocationId = e.LocationId,
                        TypeName = typeLookup.GetValueOrDefault(e.TypeId, string.Empty),
                        LocationName = locationLookup.GetValueOrDefault(e.LocationId, string.Empty)
                    })
                    .ToList();

                return View(models);
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View(new List<EventViewModel>());
            }
            catch (BrokenCircuitException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan. Pokušajte kasnije.");
                return View(new List<EventViewModel>());
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"Greška pri komunikaciji sa servisom: {ex.Message}");
                return View(new List<EventViewModel>());
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

                var eventDto = await _eventsApiClient.GetEventByIdAsync(id.Value);
                if (eventDto == null)
                {
                    return NotFound();
                }

                var location = await _referencesApiClient.GetLocationByIdAsync(eventDto.LocationId);
                var lecturers = await _referencesApiClient.GetLecturersAsync();
                var lecturerLookup = lecturers.ToDictionary(x => x.Id, x => $"{x.FirstName} {x.LastName}");

                var model = new EventViewModel
                {
                    Id = eventDto.Id,
                    Name = eventDto.Name,
                    DateTime = eventDto.DateTime,
                    DurationInHours = eventDto.DurationInHours,
                    Price = eventDto.Price,
                    Agenda = eventDto.Agenda,
                    TypeId = eventDto.TypeId,
                    LocationId = eventDto.LocationId,
                    TypeName = eventDto.Type?.Name ?? string.Empty,
                    LocationName = location?.Name ?? string.Empty,
                    EventLecturers = eventDto.EventLecturers
                        .Select(el => new EventLecturerViewModel
                        {
                            Id = el.Id,
                            EventId = el.EventId,
                            LecturerId = el.LecturerId,
                            DateTime = el.DateTime,
                            Theme = el.Theme,
                            LecturerFullName = lecturerLookup.GetValueOrDefault(el.LecturerId, string.Empty)
                        })
                        .ToList()
                };

                return View(model);
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View();
            }
            catch (BrokenCircuitException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan. Pokušajte kasnije.");
                return View();
            }
        }

        public async Task<IActionResult> Create()
        {
            try
            {
                await PopulateDropdowns();
                return View();
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View();
            }
            catch (BrokenCircuitException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan. Pokušajte kasnije.");
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventViewModel model)
        {
            try
            {
                if (model.DurationInHours <= 0)
                {
                    ModelState.AddModelError(nameof(EventViewModel.DurationInHours), "Trajanje mora biti veće od 0.");
                }

                if (!ModelState.IsValid)
                {
                    await PopulateDropdowns(model.TypeId, model.LocationId);
                    return View(model);
                }

                var ok = await _eventsApiClient.CreateEventAsync(new EventCreateRequestDto
                {
                    Name = model.Name,
                    Agenda = model.Agenda,
                    DateTime = model.DateTime,
                    DurationInHours = model.DurationInHours,
                    Price = model.Price,
                    TypeId = model.TypeId,
                    LocationId = model.LocationId
                });

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "Greška pri kreiranju događaja.");
                    await PopulateDropdowns(model.TypeId, model.LocationId);
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                await PopulateDropdowns(model.TypeId, model.LocationId);
                return View(model);
            }
            catch (BrokenCircuitException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan. Pokušajte kasnije.");
                await PopulateDropdowns(model.TypeId, model.LocationId);
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

                var eventDto = await _eventsApiClient.GetEventByIdAsync(id.Value);
                if (eventDto == null)
                {
                    return NotFound();
                }

                var model = new EventViewModel
                {
                    Id = eventDto.Id,
                    Name = eventDto.Name,
                    DateTime = eventDto.DateTime,
                    DurationInHours = eventDto.DurationInHours,
                    Price = eventDto.Price,
                    Agenda = eventDto.Agenda,
                    TypeId = eventDto.TypeId,
                    LocationId = eventDto.LocationId
                };

                await PopulateDropdowns(model.TypeId, model.LocationId);
                return View(model);
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View();
            }
            catch (BrokenCircuitException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan. Pokušajte kasnije.");
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    return NotFound();
                }

                if (model.DurationInHours <= 0)
                {
                    ModelState.AddModelError(nameof(EventViewModel.DurationInHours), "Trajanje mora biti veće od 0.");
                }

                if (!ModelState.IsValid)
                {
                    await PopulateDropdowns(model.TypeId, model.LocationId);
                    return View(model);
                }

                var ok = await _eventsApiClient.UpdateEventAsync(id, new EventUpdateRequestDto
                {
                    Name = model.Name,
                    Agenda = model.Agenda,
                    DateTime = model.DateTime,
                    DurationInHours = model.DurationInHours,
                    Price = model.Price,
                    TypeId = model.TypeId,
                    LocationId = model.LocationId
                });

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "Greška pri izmeni događaja.");
                    await PopulateDropdowns(model.TypeId, model.LocationId);
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                await PopulateDropdowns(model.TypeId, model.LocationId);
                return View(model);
            }
            catch (BrokenCircuitException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan. Pokušajte kasnije.");
                await PopulateDropdowns(model.TypeId, model.LocationId);
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

                var eventDto = await _eventsApiClient.GetEventByIdAsync(id.Value);
                if (eventDto == null)
                {
                    return NotFound();
                }

                var location = await _referencesApiClient.GetLocationByIdAsync(eventDto.LocationId);

                return View(new EventViewModel
                {
                    Id = eventDto.Id,
                    Name = eventDto.Name,
                    DateTime = eventDto.DateTime,
                    DurationInHours = eventDto.DurationInHours,
                    Price = eventDto.Price,
                    Agenda = eventDto.Agenda,
                    TypeId = eventDto.TypeId,
                    LocationId = eventDto.LocationId,
                    TypeName = eventDto.Type?.Name ?? string.Empty,
                    LocationName = location?.Name ?? string.Empty
                });
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View();
            }
            catch (BrokenCircuitException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan. Pokušajte kasnije.");
                return View();
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _eventsApiClient.DeleteEventAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return RedirectToAction(nameof(Index));
            }
            catch (BrokenCircuitException)
            {
                ModelState.AddModelError(string.Empty, "Servis je privremeno nedostupan. Pokušajte kasnije.");
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task PopulateDropdowns(int? typeId = null, int? locationId = null)
        {
            var eventTypes = await _eventsApiClient.GetEventTypesAsync();
            var locations = await _referencesApiClient.GetLocationsAsync();

            ViewData["TypeId"] = new SelectList(eventTypes.OrderBy(t => t.Name), "Id", "Name", typeId);
            ViewData["LocationId"] = new SelectList(locations.OrderBy(l => l.Name), "Id", "Name", locationId);
        }
    }
}
