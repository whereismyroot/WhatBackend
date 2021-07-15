using CharlieBackend.AdminPanel.Models.Calendar;
using CharlieBackend.AdminPanel.Models.EventOccurrence;
using CharlieBackend.Core.DTO.Schedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CharlieBackend.AdminPanel.Services.Interfaces
{
    /// <summary>
    /// Interface that describes accessible methods to work with schedules.
    /// </summary>
    public interface IScheduleService
    {
        /// <summary>
        /// Method for obtaining a collection of scheduled events
        /// using given filter DTO.
        /// </summary>
        /// <param name="scheduledEventFilterDto">
        /// DTO with a set of optional filtration parameters.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns>filtered collection of scheduled events.</returns>
        Task<IList<ScheduledEventDTO>> GetEventsFiltered(
            ScheduledEventFilterRequestDTO scheduledEventFilterDto);

        /// <summary>
        /// Method for obtaining a collection of all event occurrences.
        /// </summary>
        /// <returns>a collection of all event occurrences.</returns>
        Task<IList<EventOccurrenceDTO>> GetAllEventOccurrences();

        /// <summary>
        /// Method for obtaining a specified event occurrence by id.
        /// </summary>
        /// <returns>EventOccurrenceDTO with specified id</returns>
        Task<EventOccurrenceDTO> GetEventOccurrenceById(long id);

        /// <summary>
        /// Method for adding new event occurrence.
        /// </summary>
        Task CreateScheduleAsync(CreateScheduleDto scheduleDTO);

        /// <summary>
        /// Method for updating event occurrence by id.
        /// </summary>
        Task UpdateScheduleByIdAsync(long eventOccurrenceID, CreateScheduleDto updateScheduleDto);

        /// <summary>
        /// Method for deleting event occurrence by id.
        /// </summary>
        Task DeleteScheduleByIdAsync(long eventOccurrenceID);

        /// <summary>
        /// Method for getting required lists to add new event occurrence.
        /// </summary>
        Task<EventOccurrenceEditViewModel> PrepareStudentGroupAddAsync();
    }
}
