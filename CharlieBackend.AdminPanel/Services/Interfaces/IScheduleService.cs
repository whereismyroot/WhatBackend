using CharlieBackend.AdminPanel.Models.Calendar;
using CharlieBackend.AdminPanel.Models.Schedules;
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
        Task<IList<SchedulesViewModel>> GetAllEventOccurrences();
    }
}
