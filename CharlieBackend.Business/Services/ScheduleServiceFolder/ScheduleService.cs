using CharlieBackend.Business.Services.Interfaces;
using CharlieBackend.Core;
using CharlieBackend.Core.Entities;
using CharlieBackend.Core.DTO.Schedule;
using CharlieBackend.Data.Repositories.Impl.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CharlieBackend.Core.Models.ResultModel;
using CharlieBackend.Business.Services.ScheduleServiceFolder;
using System.Text;
using CharlieBackend.Core.DTO.Schedule.CreateScheduleDTO;

namespace CharlieBackend.Business.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IScheduledEventHandlerFactory _scheduledEventFactory;

        public ScheduleService(IUnitOfWork unitOfWork, IMapper mapper, IScheduledEventHandlerFactory scheduledEventHandlerFactory)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _scheduledEventFactory = scheduledEventHandlerFactory;
        }

        public async Task<Result<EventOccurrenceDTO>> CreateScheduleAsync(CreateScheduleDto createScheduleRequest)
        {
            string error = await ValidateCreateScheduleRequestAsync(createScheduleRequest);

            if (error != null)
            {
                return Result<EventOccurrenceDTO>.GetError(ErrorCode.ValidationError, error);
            }

            EventOccurrence result = new EventOccurrence
            {
                Pattern = createScheduleRequest.Pattern.Type,
                StudentGroupId = createScheduleRequest.Context.GroupID,
                EventStart = createScheduleRequest.Range.StartDate,
                EventFinish = createScheduleRequest.Range.FinishDate.Value,
                Storage = EventOccuranceStorageParser.GetPatternStorageValue(createScheduleRequest.Pattern)
            };

            _unitOfWork.EventOccurrenceRepository.Add(result);

            await _unitOfWork.CommitAsync();

            await AddEventsAsync(result, createScheduleRequest);

            return Result<EventOccurrenceDTO>.GetSuccess(_mapper.Map<EventOccurrenceDTO>(result));
        }

        private async Task AddEventsAsync(EventOccurrence result, CreateScheduleDto createScheduleRequest)
        {
            _unitOfWork.ScheduledEventRepository.AddRange(_scheduledEventFactory.Get(createScheduleRequest.Pattern).GetEvents(result, createScheduleRequest.Context));
            await _unitOfWork.CommitAsync();
        }

        public async Task<Result<EventOccurrenceDTO>> DeleteScheduleByIdAsync(long id, DateTime? startDate, DateTime? finishDate)
        {
            string error = await ValidateEventOccuranceId(id);

            if (error != null)
            {
                return Result<EventOccurrenceDTO>.GetError(ErrorCode.ValidationError, error);
            }

            var eventOccurrenceResult = await _unitOfWork.EventOccurrenceRepository.GetByIdAsync(id);

            var everyValue = eventOccurrenceResult
                .ScheduledEvents
                .Where(x => startDate.HasValue && x.EventFinish >= startDate.Value)
                .Where(x => finishDate.HasValue && x.EventStart <= finishDate.Value);

            var everyValueWithLesson = everyValue.Where(x => x.LessonId != null);

            var result = everyValue.Where(x => x.LessonId == null);
            if (startDate.HasValue)
            {
                if (eventOccurrenceResult.ScheduledEvents.Except(everyValue).Any() || everyValueWithLesson.Any())
                {
                    DateTime actualFinish = eventOccurrenceResult.EventFinish;

                    if (!finishDate.HasValue || finishDate.Value >= eventOccurrenceResult.EventFinish)
                    {
                        if (everyValueWithLesson.Any() && everyValueWithLesson.Last().EventFinish >= startDate.Value)
                        {
                            actualFinish = everyValueWithLesson.Last().EventFinish;
                        }
                        else
                        {
                            actualFinish = startDate.Value;
                        }

                        eventOccurrenceResult.EventFinish = actualFinish;

                        _unitOfWork.EventOccurrenceRepository.Update(eventOccurrenceResult);
                    }

                    _unitOfWork.ScheduledEventRepository.RemoveRange(result);
                }
            }
            else
            {
                await _unitOfWork.EventOccurrenceRepository.DeleteAsync(id);
            }

            await _unitOfWork.CommitAsync();

            return Result<EventOccurrenceDTO>.GetSuccess(_mapper.Map<EventOccurrenceDTO>(eventOccurrenceResult));
        }

        public async Task<Result<bool>> DeleteConcreteScheduleByIdAsync(long id)
        {
            if (id < 0)
            {
                return Result<bool>.GetError(ErrorCode.Conflict, 
                    "Can not delete scheduled event due to wrong request data");
            }

            var scheduledEvent = await _unitOfWork.ScheduledEventRepository.GetByIdAsync(id);

            if (scheduledEvent is null)
            {
                return Result<bool>.GetError(ErrorCode.ValidationError, "Scheduled event does not exist");
            }

            await _unitOfWork.ScheduledEventRepository.DeleteAsync(id);
            await _unitOfWork.CommitAsync();

            return Result<bool>.GetSuccess(true);
        }

        public async Task<Result<EventOccurrenceDTO>> GetEventOccurrenceByIdAsync(long id)
        {
            var scheduleEntity = await _unitOfWork.EventOccurrenceRepository.GetByIdAsync(id);

            return scheduleEntity == null ?
                Result<EventOccurrenceDTO>.GetError(ErrorCode.NotFound, $"Schedule with id={id} does not exist") :
                Result<EventOccurrenceDTO>.GetSuccess(_mapper.Map<EventOccurrenceDTO>(scheduleEntity));
        }

        public async Task<Result<IList<EventOccurrenceDTO>>> GetEventOccurrencesAsync()
        {
            var eventOccurences = await _unitOfWork.EventOccurrenceRepository.GetAllAsync();

            return Result<IList<EventOccurrenceDTO>>.GetSuccess(
                _mapper.Map<IList<EventOccurrenceDTO>>(eventOccurences));
        }

        public async Task<Result<IList<ScheduledEventDTO>>> GetEventsFiltered(ScheduledEventFilterRequestDTO request)
        {
            string error = await ValidateGetEventsFilteredRequest(request);

            if (error != null)
            {
                return Result<IList<ScheduledEventDTO>>.GetError(ErrorCode.ValidationError, error);
            }

            var schedulesOfGroup = await _unitOfWork.ScheduledEventRepository.GetEventsFilteredAsync(request);

            return Result<IList<ScheduledEventDTO>>.GetSuccess(
                _mapper.Map<IList<ScheduledEventDTO>>(schedulesOfGroup));
        }

        public async Task<Result<ScheduledEventDTO>> UpdateScheduledEventByID(long scheduledEventId, UpdateScheduledEventDto request)
        {
            string error = await ValidateScheduledEventId(scheduledEventId);

            error ??= await ValidateUpdateScheduleDTO(request);

            if (error != null)
            {
                return Result<ScheduledEventDTO>.GetError(ErrorCode.ValidationError, error);
            }

            ScheduledEvent item = await _unitOfWork.ScheduledEventRepository.GetByIdAsync(scheduledEventId);

            item = UpdateFields(item, request);

            _unitOfWork.ScheduledEventRepository.Update(item);

            await _unitOfWork.CommitAsync();

            return Result<ScheduledEventDTO>.GetSuccess(_mapper.Map<ScheduledEventDTO>(item));
        }

        public async Task<Result<ScheduledEventDTO>> AddSingleScheduledEvent(CreateScheduledEventDto createSingleScheduleRequest)
        {
            var singleScheduleRequest = new CreateScheduleDto()
            {
                Context = createSingleScheduleRequest.Context,
                Range = createSingleScheduleRequest.Range
            };

            string error = await ValidateCreateScheduleEventRequestAsync(singleScheduleRequest);

            if (error != null)
            {
                return Result<ScheduledEventDTO>.GetError(ErrorCode.ValidationError, error);
            }

            EventOccurrence result = null;

            _unitOfWork.EventOccurrenceRepository.Add(result);

            await _unitOfWork.CommitAsync();

            await AddEventsAsync(result, singleScheduleRequest);

            return Result<ScheduledEventDTO>.GetSuccess(_mapper.Map<ScheduledEventDTO>(result));
        }

        public async Task<Result<IList<ScheduledEventDTO>>> UpdateEventsRange(ScheduledEventFilterRequestDTO filter, UpdateScheduledEventDto request)
        {
            string error = await ValidateUpdateScheduleDTO(request);
            error ??= await ValidateGetEventsFilteredRequest(filter);

            if (error != null)
            {
                return Result<IList<ScheduledEventDTO>>.GetError(ErrorCode.ValidationError, error);
            }

            var allRelatedEvents = await _unitOfWork.ScheduledEventRepository.GetEventsFilteredAsync(filter);

            var result = allRelatedEvents.Where(x => x.LessonId is null);

            foreach (ScheduledEvent item in result)
            {
                UpdateFields(item, request);
            }

            _unitOfWork.ScheduledEventRepository.UpdateRange(result);

            await _unitOfWork.CommitAsync();

            return Result<IList<ScheduledEventDTO>>.GetSuccess(_mapper.Map<IList<ScheduledEventDTO>>(result));
        }

        public async Task<Result<EventOccurrenceDTO>> UpdateEventOccurrenceById(long eventOccurrenceId, CreateScheduleDto request)
        {
            string error = await ValidateCreateScheduleRequestAsync(request);
            error ??= await ValidateEventOccuranceId(eventOccurrenceId);

            if (error != null)
            {
                return Result<EventOccurrenceDTO>.GetError(ErrorCode.ValidationError, error);
            }

            var eventOccurrenceResult = await _unitOfWork.EventOccurrenceRepository.GetByIdAsync(eventOccurrenceId);

            eventOccurrenceResult.Pattern = request.Pattern.Type;
            eventOccurrenceResult.StudentGroupId = request.Context.GroupID;
            eventOccurrenceResult.EventStart = request.Range.StartDate;
            eventOccurrenceResult.EventFinish = request.Range.FinishDate.Value;
            eventOccurrenceResult.Storage = EventOccuranceStorageParser.GetPatternStorageValue(request.Pattern);

            _unitOfWork.ScheduledEventRepository.RemoveRange(eventOccurrenceResult.ScheduledEvents.Where(x => x.LessonId is null));
            _unitOfWork.ScheduledEventRepository.AddRange(_scheduledEventFactory.Get(request.Pattern).GetEvents(eventOccurrenceResult, request.Context));
            _unitOfWork.EventOccurrenceRepository.Update(eventOccurrenceResult);

            await _unitOfWork.CommitAsync();

            return Result<EventOccurrenceDTO>.GetSuccess(_mapper.Map<EventOccurrenceDTO>(eventOccurrenceResult));
        }

        private ScheduledEvent UpdateFields(ScheduledEvent item, UpdateScheduledEventDto request)
        {
            item.MentorId = request.MentorId ?? item.MentorId;
            item.StudentGroupId = request.StudentGroupId ?? item.StudentGroupId;
            item.ThemeId = request.ThemeId ?? item.ThemeId;
            item.EventStart = request.EventStart.HasValue ? new DateTime(item.EventStart.Year, item.EventStart.Month, item.EventStart.Day,
                request.EventStart.Value.Hour, request.EventStart.Value.Minute, request.EventStart.Value.Second) : item.EventStart;
            item.EventFinish = request.EventEnd.HasValue ? new DateTime(item.EventFinish.Year, item.EventFinish.Month, item.EventFinish.Day,
                request.EventEnd.Value.Hour, request.EventEnd.Value.Minute, request.EventEnd.Value.Second) : item.EventFinish;

            return item;
        }

        private async Task<string> ValidateCreateScheduleEventRequestAsync(CreateScheduleDto request)
        {
            if (request == null)
            {
                return "Request must not be null";
            }

            StringBuilder error = new StringBuilder(string.Empty);

            if (!await _unitOfWork.StudentGroupRepository.IsEntityExistAsync(request.Context.GroupID))
            {
                error.Append(" Group does not exist");
            }

            if (request.Context.MentorID.HasValue && !await _unitOfWork.MentorRepository.IsEntityExistAsync(request.Context.MentorID.Value))
            {
                error.Append(" Mentor does not exist");
            }

            if (request.Context.ThemeID.HasValue && !await _unitOfWork.ThemeRepository.IsEntityExistAsync(request.Context.ThemeID.Value))
            {
                error.Append(" Theme does not exist");
            }
            return error.Length > 0 ? error.ToString() : null;
        }

        private async Task<string> ValidateCreateScheduleRequestAsync(CreateScheduleDto request)
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

        private async Task<string> ValidateUpdateScheduleDTO(UpdateScheduledEventDto request)
        {
            if (request == null)
            {
                return "requestDTO body must not be null";
            }

            StringBuilder error = new StringBuilder(string.Empty);

            if (request.StudentGroupId.HasValue && !await _unitOfWork.StudentGroupRepository.IsEntityExistAsync(request.StudentGroupId.Value))
            {
                error.Append($" No such group id={request.StudentGroupId.Value}");
            }

            if (request.MentorId.HasValue && !await _unitOfWork.MentorRepository.IsEntityExistAsync(request.MentorId.Value))
            {
                error.Append($" No such mentor id={request.MentorId.Value}");
            }

            if (request.ThemeId.HasValue && !await _unitOfWork.ThemeRepository.IsEntityExistAsync(request.ThemeId.Value))
            {
                error.Append($" No such theme id={request.ThemeId.Value}");
            }

            if (request.EventEnd.HasValue && request.EventStart.HasValue && (request.EventEnd < request.EventStart))
            {
                error.Append($" StartDate must be less then FinisDate");
            }

            return error.Length > 0 ? error.ToString() : null;
        }

        private async Task<string> ValidateEventOccuranceId(long id)
        {
            string result = null;

            if (!await _unitOfWork.EventOccurrenceRepository.IsEntityExistAsync(id))
            {
                result = $"EventOccurance id={id} does not exist";
            }

            return result;
        }

        private async Task<string> ValidateScheduledEventId(long id)
        {
            string result = null;

            if (!await _unitOfWork.ScheduledEventRepository.IsEntityExistAsync(id))
            {
                result = $"ScheduledEvent id={id} does not exist";
            }

            return result;
        }

        private async Task<string> ValidateGetEventsFilteredRequest(ScheduledEventFilterRequestDTO request)
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

        public async Task<Result<ScheduledEventDTO>> GetConcreteScheduleByIdAsync(long eventId)
        {
            var foundScheduleEvent = await _unitOfWork.ScheduledEventRepository.GetByIdAsync(eventId);

            return foundScheduleEvent == null ?
                Result<ScheduledEventDTO>.GetError(ErrorCode.NotFound, $"Single schedule event with id={eventId} does not exist") :
                Result<ScheduledEventDTO>.GetSuccess(_mapper.Map<ScheduledEventDTO>(foundScheduleEvent));
        }
    }
}
