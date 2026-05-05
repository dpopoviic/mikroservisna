using EventPlatformAPI.DTO;
using EventPlatformAPI.Web.Services;
using EventPlatformAPI.Web.ViewModels.EventLecturers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Polly.Timeout;

namespace EventPlatformAPI.Web.Controllers
{
    public class EventLecturersController : Controller
    {
        private readonly IEventsApiClient _eventsApiClient;
        private readonly IReferencesApiClient _referencesApiClient;

        public EventLecturersController(IEventsApiClient eventsApiClient, IReferencesApiClient referencesApiClient)
        {
            _eventsApiClient = eventsApiClient;
            _referencesApiClient = referencesApiClient;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var slots = await _eventsApiClient.GetEventLecturersAsync();
                var events = await _eventsApiClient.GetEventsAsync();
                var lecturers = await _referencesApiClient.GetLecturersAsync();

                var eventLookup = events.ToDictionary(x => x.Id, x => x.Name);
                var lecturerLookup = lecturers.ToDictionary(x => x.Id, x => $"{x.FirstName} {x.LastName}");

                var models = slots
                    .OrderBy(e => e.DateTime)
                    .Select(e => new EventLecturerViewModel
                    {
                        Id = e.Id,
                        EventId = e.EventId,
                        LecturerId = e.LecturerId,
                        DateTime = e.DateTime,
                        Theme = e.Theme,
                        EventName = eventLookup.GetValueOrDefault(e.EventId, string.Empty),
                        LecturerFullName = lecturerLookup.GetValueOrDefault(e.LecturerId, string.Empty)
                    })
                    .ToList();

                return View(models);
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View(new List<EventLecturerViewModel>());
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

                var slot = await _eventsApiClient.GetEventLecturerByIdAsync(id.Value);
                if (slot == null)
                {
                    return NotFound();
                }

                var eventDto = await _eventsApiClient.GetEventByIdAsync(slot.EventId);
                var lecturerDto = await _referencesApiClient.GetLecturerByIdAsync(slot.LecturerId);

                return View(new EventLecturerViewModel
                {
                    Id = slot.Id,
                    EventId = slot.EventId,
                    LecturerId = slot.LecturerId,
                    DateTime = slot.DateTime,
                    Theme = slot.Theme,
                    EventName = eventDto?.Name ?? string.Empty,
                    LecturerFullName = lecturerDto == null ? string.Empty : $"{lecturerDto.FirstName} {lecturerDto.LastName}"
                });
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
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
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventLecturerViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await PopulateDropdowns(model.EventId, model.LecturerId);
                    return View(model);
                }

                var ok = await _eventsApiClient.CreateEventLecturerAsync(new EventLecturerCreateRequestDto
                {
                    EventId = model.EventId,
                    LecturerId = model.LecturerId,
                    DateTime = model.DateTime,
                    Theme = model.Theme
                });
            // pitanje: da li ovde ponovo proveriti da li izabrani predavac postoji u tabeli predavaca?
                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "Veza za isti događaj, predavača i termin već postoji ili je unos neispravan.");
                    await PopulateDropdowns(model.EventId, model.LecturerId);
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                await PopulateDropdowns(model.EventId, model.LecturerId);
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

                var slot = await _eventsApiClient.GetEventLecturerByIdAsync(id.Value);
                if (slot == null)
                {
                    return NotFound();
                }

                var model = new EventLecturerViewModel
                {
                    Id = slot.Id,
                    EventId = slot.EventId,
                    LecturerId = slot.LecturerId,
                    DateTime = slot.DateTime,
                    Theme = slot.Theme
                };

                await PopulateDropdowns(model.EventId, model.LecturerId);
                return View(model);
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventLecturerViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    return NotFound();
                }

                if (!ModelState.IsValid)
                {
                    await PopulateDropdowns(model.EventId, model.LecturerId);
                    return View(model);
                }

                var ok = await _eventsApiClient.UpdateEventLecturerAsync(id, new EventLecturerUpdateRequestDto
                {
                    EventId = model.EventId,
                    LecturerId = model.LecturerId,
                    DateTime = model.DateTime,
                    Theme = model.Theme
                });

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "Greška pri izmeni termina predavača.");
                    await PopulateDropdowns(model.EventId, model.LecturerId);
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                await PopulateDropdowns(model.EventId, model.LecturerId);
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

                var slot = await _eventsApiClient.GetEventLecturerByIdAsync(id.Value);
                if (slot == null)
                {
                    return NotFound();
                }

                var eventDto = await _eventsApiClient.GetEventByIdAsync(slot.EventId);
                var lecturerDto = await _referencesApiClient.GetLecturerByIdAsync(slot.LecturerId);

                return View(new EventLecturerViewModel
                {
                    Id = slot.Id,
                    EventId = slot.EventId,
                    LecturerId = slot.LecturerId,
                    DateTime = slot.DateTime,
                    Theme = slot.Theme,
                    EventName = eventDto?.Name ?? string.Empty,
                    LecturerFullName = lecturerDto == null ? string.Empty : $"{lecturerDto.FirstName} {lecturerDto.LastName}"
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
                await _eventsApiClient.DeleteEventLecturerAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (TimeoutRejectedException)
            {
                ModelState.AddModelError(string.Empty, "Zahtev je istekao. Servis ne odgovara.");
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task PopulateDropdowns(int? eventId = null, int? lecturerId = null)
        {
            var events = await _eventsApiClient.GetEventsAsync();
            var lecturers = await _referencesApiClient.GetLecturersAsync();

            ViewData["EventId"] = new SelectList(events.OrderBy(e => e.Name), "Id", "Name", eventId);
            ViewData["LecturerId"] = new SelectList(
                lecturers
                    .OrderBy(l => l.LastName)
                    .ThenBy(l => l.FirstName)
                    .Select(l => new { l.Id, FullName = l.FirstName + " " + l.LastName }),
                "Id",
                "FullName",
                lecturerId);
        }
    }
}
