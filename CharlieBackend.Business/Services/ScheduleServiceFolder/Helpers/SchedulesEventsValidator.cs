using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CharlieBackend.Core.DTO.Schedule;
using CharlieBackend.Core.Entities;
using CharlieBackend.Data.Repositories.Impl.Interfaces;

namespace CharlieBackend.Business.Services.ScheduleServiceFolder.Helpers
{
    class SchedulesEventsValidator
    {
        private readonly IUnitOfWork _unitOfWork;

        public SchedulesEventsValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<string> ValidateCreateScheduleRequestAsync(CreateScheduleDto request)
        {
            if (request == null)
            {
                return "Request must not be null";
            }

            StringBuilder error = new StringBuilder(string.Empty);

            if (request.Pattern.Interval <= 0)
            {
                error.Append(" Interval value out of range");
            }

            if (!await _unitOfWork.StudentGroupRepository.IsEntityExistAsync(request.Context.GroupID))
            {
                error.Append(" Group does not exist");
            }

            if (request.Context.MentorID.HasValue && !await _unitOfWork.MentorRepository.IsEntityExistAsync(request.Context.MentorID.Value))
            {
                error.Append(" Mentor does not exist");
            }

            if ((await _unitOfWork.AccountRepository.GetAccountCredentialsById(request.Context.MentorID.Value)).IsActive == false)
            {
                error.Append(" Mentor is not active");
            }

            if (request.Context.ThemeID.HasValue && !await _unitOfWork.ThemeRepository.IsEntityExistAsync(request.Context.ThemeID.Value))
            {
                error.Append(" Theme does not exist");
            }

            switch (request.Pattern.Type)
            {
                case PatternType.Daily:
                    break;
                case PatternType.Weekly:
                    if (request.Pattern.DaysOfWeek == null || request.Pattern.DaysOfWeek.Count == 0)
                    {
                        error.Append(" Target days not provided");
                    }
                    break;
                case PatternType.AbsoluteMonthly:
                    if (request.Pattern.Dates == null || request.Pattern.Dates.Count == 0)
                    {
                        error.Append(" Target dates not provided");
                    }
                    break;
                case PatternType.RelativeMonthly:
                    if ((request.Pattern.DaysOfWeek == null || request.Pattern.DaysOfWeek.Count == 0)
                        || (request.Pattern.Index != null ? request.Pattern.Index <= 0 : false))
                    {
                        error.Append(" Target days not provided");
                    }
                    break;
                default:
                    error.Append(" Pattern type not supported");
                    break;
            }

            return error.Length > 0 ? error.ToString() : null;
        }

        public async Task<string> ValidateEventOccuranceId(long id)
        {
            string result = null;

            if (!await _unitOfWork.EventOccurrenceRepository.IsEntityExistAsync(id))
            {
                result = $"EventOccurance id={id} does not exist";
            }

            return result;
        }

        public async Task<string> ValidateGetEventsFilteredRequest(ScheduledEventFilterRequestDTO request)
        {
            if (request == null)
            {
                return "Request must not be null";
            }

            StringBuilder error = new StringBuilder(string.Empty);

            if (request.CourseID.HasValue && !await _unitOfWork.CourseRepository.IsEntityExistAsync(request.CourseID.Value))
            {
                error.Append(" Course does not exist");
            }

            if (request.GroupID.HasValue && !await _unitOfWork.StudentGroupRepository.IsEntityExistAsync(request.GroupID.Value))
            {
                error.Append(" Group does not exist");
            }

            if (request.MentorID.HasValue && !await _unitOfWork.MentorRepository.IsEntityExistAsync(request.MentorID.Value))
            {
                error.Append(" Mentor does not exist");
            }

            if (request.StudentAccountID.HasValue && !await _unitOfWork.StudentRepository.IsEntityExistAsync(request.StudentAccountID.Value))
            {
                error.Append(" Student does not exist");
            }

            if (request.ThemeID.HasValue && !await _unitOfWork.ThemeRepository.IsEntityExistAsync(request.ThemeID.Value))
            {
                error.Append(" Theme does not exist");
            }

            if (request.StartDate.HasValue && request.FinishDate.HasValue && (request.StartDate > request.FinishDate))
            {
                error.Append($" StartDate must be less then FinisDate");
            }

            return error.Length > 0 ? error.ToString() : null;
        }

        public async Task<string> ValidateScheduledEventId(long id)
        {
            string result = null;

            if (!await _unitOfWork.ScheduledEventRepository.IsEntityExistAsync(id))
            {
                result = $"ScheduledEvent id={id} does not exist";
            }

            return result;
        }
    }
}
