using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CharlieBackend.Business.Services.Interfaces;
using CharlieBackend.Core.DTO.Schedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CharlieBackend.Core;
using CharlieBackend.Core.Models.ResultModel;
using Swashbuckle.AspNetCore.Annotations;
using CharlieBackend.Core.Entities;
using CharlieBackend.Core.DTO.Schedule.CreateScheduleDTO;

namespace CharlieBackend.Api.Controllers
{
    /// <summary>
    /// Controller to manage schedules data
    /// </summary>
    [Route("api/schedules")]
    [ApiController]
    public class SchedulesController : ControllerBase
    {


        private readonly IScheduleService _scheduleService;


        /// <summary>
        /// Schedules cocontroller constructor
        /// </summary>
        /// <param name="scheduleService"></param>
        public SchedulesController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        /// <summary>
        /// Add new schedule
        /// </summary>
        /// <remarks>
        /// Creates new EventOccurance instance and related ScheduledEvents
        /// Information on input format could be found here: https://docs.microsoft.com/en-us/graph/outlook-schedule-recurring-events
        /// </remarks>
        /// <response code="200">Successful add of schedule</response>
        /// <response code="HTTP: 400, API: 0">Can not create schedule due to wrong request data</response>
        /// <response code="HTTP: 404, API: 3">Can not create schedule due to missing request data</response>
        [SwaggerResponse(200, type: typeof(EventOccurrenceDTO))]
        [Authorize(Roles = "Secretary, Admin")]
        [HttpPost]
        public async Task<ActionResult<EventOccurrenceDTO>> PostSchedule([FromBody] CreateScheduleDto scheduleDTO)
        {
            var resSchedule = await _scheduleService.CreateScheduleAsync(scheduleDTO);

            return resSchedule.ToActionResult();
        }

        /// <summary>
        /// Get event occurrence by id
        /// </summary>
        /// <response code="200">Successful add of schedule</response>        
        /// <response code="HTTP: 404, API: 3">No such event occurence</response>
        [SwaggerResponse(200, type: typeof(EventOccurrenceDTO))]
        [Authorize(Roles = "Secretary, Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<EventOccurrenceDTO>> GetEventOccuranceByID(long id)
        {
            var resSchedule = await _scheduleService.GetEventOccurrenceByIdAsync(id);

            return resSchedule.ToActionResult();
        }

        /// <summary>
        /// Get concrete single schedule event by id
        /// </summary>
        /// <response code = "200" > Successful getting of schedule</response>
        /// <response code="HTTP: 404, API: 3">Error, given schedule event not found</response>
        [SwaggerResponse(200, type: typeof(ScheduledEventDTO))]
        [Authorize(Roles = "Secretary, Admin")]
        [HttpGet("events/{scheduledEventID}")]
        public async Task<ActionResult<ScheduledEventDTO>> GetConcreteScheduleByID(long scheduledEventID)
        {
            var foundScheduleEvent = await _scheduleService.GetConcreteScheduleByIdAsync(scheduledEventID);

            return foundScheduleEvent.ToActionResult();
        }

        /// <summary>
        /// Returns the list of events depending on the filtering rules set
        /// </summary>
        /// <response code="200">Successful return event list</response>
        [SwaggerResponse(200, type: typeof(IList<ScheduledEventDTO>))]
        [Authorize(Roles = "Secretary, Admin, Mentor, Student")]
        [HttpPost("events")]
        public async Task<ActionResult<IList<ScheduledEventDTO>>> GetEventsFiltered(ScheduledEventFilterRequestDTO request)
        {
            var foundSchedules = await _scheduleService.GetEventsFiltered(request);

            return foundSchedules.ToActionResult();
        }

        /// <summary>
        /// Updates range of events
        /// </summary>
        /// <remarks>
        /// Event date could not changed via this method.
        /// Event start and Finish could only be used to change event start and finish time.
        /// </remarks>
        /// <response code="200">Successful update of schedule</response>
        /// <response code="HTTP: 404, API: 3">Error, update data is missing</response>
        /// <response code="HTTP: 400, API: 0">Error, update data is wrong</response>
        [SwaggerResponse(200, type: typeof(EventOccurrence))]
        [Authorize(Roles = "Secretary, Admin")]
        [HttpPut("events/updateRange")]
        public async Task<ActionResult<IList<ScheduledEventDTO>>> UpdateEventRange([FromBody] EventUpdateRangeDTO request)
        {
            var foundSchedules = await _scheduleService.UpdateEventsRange(request.Filter, request.Request);

            return foundSchedules.ToActionResult();
        }

        /// <summary>
        /// Updates a single event
        /// </summary>
        /// <response code="200">Successful update of schedule</response>
        /// <response code="HTTP: 404, API: 3">Error, update data is missing</response>
        /// <response code="HTTP: 400, API: 0">Error, update data is wrong</response>
        [SwaggerResponse(200, type: typeof(EventOccurrenceDTO))]
        [Authorize(Roles = "Secretary, Admin")]
        [HttpPut("events/{scheduledEventID}")]
        public async Task<ActionResult<ScheduledEventDTO>> UpdateEventById(long scheduledEventID, [FromBody] UpdateScheduledEventDto request)
        {
            var foundSchedules = await _scheduleService.UpdateScheduledEventByID(scheduledEventID, request);

            return foundSchedules.ToActionResult();
        }

        /// <summary>
        /// Add new single schedule event
        /// </summary>
        /// <response code="200">Successful add of single schedule event</response>
        /// <response code="HTTP: 400, API: 0">Can not create single schedule event due to wrong request data</response>
        /// <response code="HTTP: 404, API: 3">Can not create single schedule event due to missing request data</response>
        [SwaggerResponse(200, type: typeof(EventOccurrenceDTO))]
        [Authorize(Roles = "Secretary, Admin")]
        [HttpPost ("event1")]
        public async Task<ActionResult<ScheduledEventDTO>> AddSingleEvent([FromBody] CreateScheduledEventDto request)
        {
            var foundSchedules = await _scheduleService.AddSingleScheduledEvent(request);

            return foundSchedules.ToActionResult();
        }

        /// <summary>
        /// Updates a single EventOccurrence
        /// </summary>
        /// <remarks>
        /// Old instance of event occurrence is replaced with the new one (id is the same). 
        /// Scheduled events are recreated accordingly. 
        /// Any events with lessons attached are not removed
        /// </remarks>
        /// <response code="200">Successful update of schedule</response>
        /// <response code="HTTP: 404, API: 3">Error, update data is missing</response>
        /// <response code="HTTP: 400, API: 0">Error, update data is wrong</response>
        [SwaggerResponse(200, type: typeof(EventOccurrence))]
        [Authorize(Roles = "Secretary, Admin")]
        [HttpPut("eventOccurrences/{eventOccurrenceID}")]
        public async Task<ActionResult<EventOccurrenceDTO>> UpdateEventOccurrenceById(long eventOccurrenceID, [FromBody] CreateScheduleDto updateOccurrenceRequest)
        {
            var foundSchedules = await _scheduleService.UpdateEventOccurrenceById(eventOccurrenceID, updateOccurrenceRequest);

            return foundSchedules.ToActionResult();
        }

        /// <summary>
        /// Gets all event occurrences
        /// </summary>
        /// <response code="200">Successfully returned a collection of event occurrences.</response>
        [SwaggerResponse(200, type: typeof(IList<EventOccurrenceDTO>))]
        [Authorize(Roles = "Secretary, Admin")]
        [HttpGet("event-occurrences")]
        public async Task<ActionResult<IList<EventOccurrenceDTO>>> GetAllEventOccurrences()
        {
            var eventOccurrences = await _scheduleService.GetEventOccurrencesAsync();

            return eventOccurrences.ToActionResult();
        }

        /// <summary>
        /// Deletes exact schedule
        /// </summary>
        /// <remarks>
        /// Removes scheduled events related to specified EventOccurrence, updates start and finish date accordingly
        /// If no events are left, event occurrence is deleted completely
        /// Start and finish dates input is optional. Leave blanc to remove all related events
        /// Events with lessons attached are not deleted
        /// </remarks>
        /// <response code = "200" > Successful delete of schedule</response>
        /// <response code="HTTP: 404, API: 3">Error, given schedule not found</response>
        [Authorize(Roles = "Secretary, Admin")]
        [HttpDelete("{eventOccurrenceID}")]
        public async Task<ActionResult<EventOccurrenceDTO>> DeleteSchedule(long eventOccurrenceID, DateTime? startDate, DateTime? finishDate)
        {
            var foundSchedules = await _scheduleService.DeleteScheduleByIdAsync(eventOccurrenceID, startDate, finishDate);

            return foundSchedules.ToActionResult();
        }

        /// <summary>
        /// Deletes concrete scheduled event by id
        /// </summary>
        /// <remarks>
        /// Removes one concrete scheduled event related to specified EventOccurrence
        /// Returns true if deleting was done, false in case scheduled event doesn't exist and error after the wrong request
        /// </remarks>
        /// <response code = "200" > Successful delete of schedule</response>
        /// /// <response code="HTTP: 400, API: 0">Scheduled event does not exist</response>
        /// <response code="HTTP: 409, API: 5">Can not delete scheduled event due to wrong request data</response>
        [Authorize(Roles = "Secretary, Admin")]
        [HttpDelete("events/{scheduledEventID}")]
        public async Task<ActionResult> DeleteConcreteSchedule(long scheduledEventID)
        {
            var result = await _scheduleService.DeleteConcreteScheduleByIdAsync(scheduledEventID);

            return result.ToActionResult();
        }
    }
}
