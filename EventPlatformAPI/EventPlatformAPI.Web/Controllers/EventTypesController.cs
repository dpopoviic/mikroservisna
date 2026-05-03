using EventPlatformAPI.DTO;
using EventPlatformAPI.Web.Services;
using EventPlatformAPI.Web.ViewModels.EventTypes;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatformAPI.Web.Controllers
{
    public class EventTypesController : Controller
    {
        private readonly IEventsApiClient _eventsApiClient;

        public EventTypesController(IEventsApiClient eventsApiClient)
        {
            _eventsApiClient = eventsApiClient;
        }

        public async Task<IActionResult> Index()
        {
            var models = (await _eventsApiClient.GetEventTypesAsync())
                .OrderBy(t => t.Name)
                .Select(t => new EventTypeViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description
                })
                .ToList();

            return View(models);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventType = await _eventsApiClient.GetEventTypeByIdAsync(id.Value);
            if (eventType == null)
            {
                return NotFound();
            }

            return View(new EventTypeViewModel
            {
                Id = eventType.Id,
                Name = eventType.Name,
                Description = eventType.Description
            });
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

            var ok = await _eventsApiClient.CreateEventTypeAsync(new EventTypeDto
            {
                Name = model.Name,
                Description = model.Description
            });

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "Greška pri ?uvanju tipa doga?aja.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventType = await _eventsApiClient.GetEventTypeByIdAsync(id.Value);
            if (eventType == null)
            {
                return NotFound();
            }

            return View(new EventTypeViewModel
            {
                Id = eventType.Id,
                Name = eventType.Name,
                Description = eventType.Description
            });
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

            var ok = await _eventsApiClient.UpdateEventTypeAsync(id, new EventTypeDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description
            });

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "Greška pri izmeni tipa doga?aja.");
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

            var eventType = await _eventsApiClient.GetEventTypeByIdAsync(id.Value);
            if (eventType == null)
            {
                return NotFound();
            }

            return View(new EventTypeViewModel
            {
                Id = eventType.Id,
                Name = eventType.Name,
                Description = eventType.Description
            });
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _eventsApiClient.DeleteEventTypeAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
