using EventPlatformAPI.UsersAPI.Application.Commands;
using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.Queries;
using EventPlatformAPI.UsersAPI.Application.Requests;
using EventPlatformAPI.UsersAPI.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatformAPI.UsersAPI.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventRegistrationsController(IQueryDispatcher queryHandler, ICommandDispatcher commandDispatcher) : Controller
    {

        [HttpGet("{eventId:int}/registrations")]
        public async Task<IActionResult> GetRegistrationsForEvent(
            int eventId,
            CancellationToken cancellationToken)
        {
            var result = await queryHandler.Dispatch<GetRegistrationsForEventQuery, List<RegistrationRequest>>(
                new GetRegistrationsForEventQuery(eventId), cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id:guid}/registrations")]
        public async Task<IActionResult> CreateRegistration(
       Guid id,
       [FromBody] CreateRegistrationViewModel request,
       CancellationToken cancellationToken)
        {
            var registrationId = Guid.NewGuid();
            try 
            { 
                await commandDispatcher.Dispatch(
                    new CreateRegistrationCommand(id, request.EventId, Guid.NewGuid()),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }

            return NoContent();
        }

        [HttpPut("{id:guid}/registrations/{eventId:int}/confirm")]
        public async Task<IActionResult> ConfirmRegistration(
            Guid id,
            int eventId,
            CancellationToken cancellationToken)
        {
            try 
            { 
                await commandDispatcher.Dispatch(
                    new ConfirmRegistrationCommand(id, eventId, Guid.NewGuid()),
                    cancellationToken); 
            }
              catch (Exception ex)
              {
                return BadRequest(new { error = ex.Message });
              }

            return NoContent();
        }

        [HttpPut("{id:guid}/registrations/{eventId:int}/cancel")]
        public async Task<IActionResult> CancelRegistration(
            Guid id,
            int eventId,
            CancellationToken cancellationToken)
        {
            try { 
            await commandDispatcher.Dispatch(
                new CancelRegistrationCommand(id, eventId, Guid.NewGuid()),
                cancellationToken);
            }
            catch (Exception ex)
            {

                return BadRequest(new { error = ex.Message });
            }

            return Ok();
        }
    }
}
