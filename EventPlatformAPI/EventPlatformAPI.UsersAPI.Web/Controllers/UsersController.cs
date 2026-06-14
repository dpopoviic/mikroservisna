using EventPlatformAPI.UsersAPI.Application.Commands;
using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Application.Queries;
using EventPlatformAPI.UsersAPI.Application.ReadModels;
using EventPlatformAPI.UsersAPI.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventPlatformAPI.UsersAPI.Web.Controllers
{
    public class UsersController(IQueryDispatcher queryHandler, ICommandDispatcher commandDispatcher) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetUsersQuery();

            var result = await queryHandler.Dispatch<GetUsersQuery, List<UserRequest>>(query, cancellationToken);

            return Ok(result);
        }
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetUserByIdQuery(id);
            var result = await queryHandler.Dispatch<GetUserByIdQuery, UserRequest?>(query, cancellationToken);

            return result is null ? NotFound() : Ok(result);
        }

        [HttpGet("{id:guid}/history")]
        public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetUserHistoryQuery(id);

            var result = await queryHandler.Dispatch<GetUserHistoryQuery, List<EventHistoryRequest>>(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}/registrations")]
        public async Task<IActionResult> GetUserRegistrations(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetUserRegistrationsQuery(id);

            var result = await queryHandler.Dispatch<GetUserRegistrationsQuery, List<RegistrationRequest>>(query, cancellationToken);   

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserViewModel request,
        CancellationToken cancellationToken)
        {
            var userId = Guid.NewGuid();

            try
            {
                await commandDispatcher.Dispatch(new
                    CreateUserCommand(
                    userId,
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    Guid.NewGuid()),
                    cancellationToken);

            }
            catch (Exception ex)
            {

                return BadRequest(new { error = ex.Message });
            }

            return CreatedAtAction(nameof(GetById), new { id = userId }, new { id = userId });
        }


        [HttpPut("{id:guid}/email")]
        public async Task<IActionResult> UpdateEmail(
        Guid id,
        [FromBody] UpdateUserEmailViewModel request,
        CancellationToken cancellationToken)
        {
            try
            {
                await commandDispatcher.Dispatch(
                        new UpdateUserEmailCommand(id, request.NewEmail, Guid.NewGuid()),
                        cancellationToken);

            }
            catch (Exception ex)
            {

                return BadRequest(new { error = ex.Message });
            }

            return Ok();
        }

        [HttpPut("{id:guid}/activate")]
        public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await commandDispatcher.Dispatch(
                new ActivateUserCommand(id, Guid.NewGuid()),
                cancellationToken);
            }
            catch (Exception ex)
            {

                return BadRequest(new { error = ex.Message });
            }

            return Ok();
        }

        [HttpPut("{id:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await commandDispatcher.Dispatch(
                new DeactivateUserCommand(id, Guid.NewGuid()),
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
