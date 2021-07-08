using AutoMapper;
using CharlieBackend.Business.Services;
using CharlieBackend.Core.DTO.Schedule;
using CharlieBackend.Core.Entities;
using CharlieBackend.Core.Mapping;
using CharlieBackend.Core.Models.ResultModel;
using CharlieBackend.Data.Repositories.Impl.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using CharlieBackend.Business.Services.ScheduleServiceFolder;
using FluentAssertions;
using CharlieBackend.Business.Services.Interfaces;
using System.Linq;

namespace CharlieBackend.Api.UnitTest
{
    public class ScheduleServiceTests : TestBase
    {
        private readonly IMapper _mapper;
        private readonly IScheduledEventHandlerFactory _scheduledEventFactory;
        private readonly Mock<IScheduledEventRepository> _scheduleRepositoryMock;
        private readonly Mock<IThemeRepository> _themeRepositoryMock;
        private readonly Mock<IMentorRepository> _mentorRepositoryMock;
        private readonly Mock<IAccountRepository> _accountRepositoryMock;
        private readonly Mock<IStudentGroupRepository> _studentGroupRepositoryMock;
        private readonly long existentGroupId = 3;
        private readonly long existentMentorId = 1;
        private readonly long existentThemeId = 5;
        private readonly long newGroupId = 2;
        private readonly long newThemeId = 3;
        private readonly long existingId = 1;
        private readonly long nonExistentId = -1;
        private readonly long nonExistentEventOccuranceId = 0;
        private readonly long existentEventOccuranceId = 1;
        private readonly long id = 0;
        private Mock<IEventOccurrenceRepository> _eventOccuranceRepositoryMock;
        private CreateScheduleDto validScheduleDTO;
        private EventOccurrence validEventOccurrence;
        private readonly DateTime startDate = DateTime.Parse("Jan 1, 2021");
        private readonly DateTime finishDate = DateTime.Parse("Feb 1, 2021");

        public ScheduleServiceTests()
        {
            _mapper = GetMapper(new ModelMappingProfile());
            _eventOccuranceRepositoryMock = new Mock<IEventOccurrenceRepository>();
            _scheduledEventFactory = new ScheduledEventHandlerFactory();
            _scheduleRepositoryMock = new Mock<IScheduledEventRepository>();
            _themeRepositoryMock = new Mock<IThemeRepository>();
            _mentorRepositoryMock = new Mock<IMentorRepository>();
            _accountRepositoryMock = new Mock<IAccountRepository>();
            _studentGroupRepositoryMock = new Mock<IStudentGroupRepository>();
            _unitOfWorkMock.Setup(x => x.ScheduledEventRepository).Returns(_scheduleRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.ThemeRepository).Returns(_themeRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.MentorRepository).Returns(_mentorRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.AccountRepository).Returns(_accountRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.StudentGroupRepository).Returns(_studentGroupRepositoryMock.Object);
            _unitOfWorkMock.Setup(x => x.EventOccurrenceRepository).Returns(_eventOccuranceRepositoryMock.Object);
            validScheduleDTO = new CreateScheduleDto
            {
                Pattern = new PatternForCreateScheduleDTO
                {
                    Type = PatternType.Daily,
                    Interval = 1,
                    DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday },
                },

                Range = new OccurenceRange
                {
                    StartDate = startDate,
                    FinishDate = finishDate
                },

                Context = new ContextForCreateScheduleDTO
                {
                    GroupID = existentGroupId,
                    MentorID = existentMentorId,
                    ThemeID = existentThemeId
                }
            };

            validEventOccurrence = new EventOccurrence
            {
                Pattern = validScheduleDTO.Pattern.Type,
                StudentGroupId = validScheduleDTO.Context.GroupID,
                EventStart = validScheduleDTO.Range.StartDate,
                EventFinish = validScheduleDTO.Range.FinishDate.Value,
                Storage = EventOccuranceStorageParser.GetPatternStorageValue(validScheduleDTO.Pattern)
            };
        }

        private ScheduledEventFilterRequestDTO Get_ScheduledEventFilterRequestDTO(
            long? courseId = null,
            long? mentorId = null,
            long? groupId = null,
            long? themeId = null,
            long? studentAccountId = null,
            long? EventOccurenceId = null,
            DateTime? startDate = null,
            DateTime? finishDate = null)
        {
            return new ScheduledEventFilterRequestDTO
            {
                CourseID = courseId,
                MentorID = mentorId,
                GroupID = groupId,
                ThemeID = themeId,
                StudentAccountID = studentAccountId,
                EventOccurrenceID = EventOccurenceId,
                StartDate = startDate,
                FinishDate = finishDate
            };
        }

        private void Initialize(CreateScheduleDto createScheduleDto)
        {
            _scheduleRepositoryMock.Setup(x => x.AddRange(new List<ScheduledEvent>()
            {
                new ScheduledEvent
                {
                    ThemeId = existentThemeId,
                    StudentGroupId = existentGroupId,
                    MentorId = existentMentorId
                }
            }
            ));

            var eventOccuranceRepositoryMock = new Mock<IEventOccurrenceRepository>();

            if (createScheduleDto != null)
            {
                eventOccuranceRepositoryMock.Setup(x => x.Add(
                    new EventOccurrence
                    {
                        StudentGroupId = existentGroupId,
                        EventStart = createScheduleDto.Range.StartDate,
                        Pattern = createScheduleDto.Pattern.Type
                    }
                    ));
            }
        }

        [Fact]
        public async Task CreateScheduleAsync_ValidScheduleRequest_ShouldReturnEventOccurrenceDTO()
        {
            //Arrange
            _themeRepositoryMock.Setup(x => x.IsEntityExistAsync(existentThemeId)).ReturnsAsync(true);

            _mentorRepositoryMock.Setup(x => x.IsEntityExistAsync(existentMentorId)).ReturnsAsync(true);

            _accountRepositoryMock.Setup(x => x.GetAccountCredentialsById(existentMentorId)).ReturnsAsync(new Account { IsActive = true });

            _studentGroupRepositoryMock.Setup(x => x.IsEntityExistAsync(existentGroupId)).ReturnsAsync(true);

            Initialize(validScheduleDTO);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.CreateScheduleAsync(validScheduleDTO);

            //Assert
            result.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(_mapper.Map<EventOccurrenceDTO>(validEventOccurrence));
        }

        [Fact]
        public async Task CreateScheduleAsync_PatternIntervalLessThanNull_ShouldReturnValidationError()
        {
            //Arrange
            var createScheduleDto = new CreateScheduleDto
            {
                Pattern = new PatternForCreateScheduleDTO
                {
                    Type = PatternType.Daily,
                    Interval = -1
                },

                Range = new OccurenceRange { },

                Context = new ContextForCreateScheduleDTO
                {
                    GroupID = existentGroupId,
                    MentorID = existentMentorId
                }
            };

            _studentGroupRepositoryMock.Setup(x => x.IsEntityExistAsync(existentGroupId)).ReturnsAsync(true);

            _accountRepositoryMock.Setup(x => x.GetAccountCredentialsById(existentMentorId)).ReturnsAsync(new Account { IsActive = true });

            Initialize(createScheduleDto);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.CreateScheduleAsync(createScheduleDto);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task CreateScheduleAsync_NotValidGroupId_ShouldReturnValidationError()
        {
            //Arrange
            var createScheduleDto = new CreateScheduleDto
            {
                Pattern = new PatternForCreateScheduleDTO
                {
                    Type = PatternType.Daily,
                    Interval = 1
                },

                Range = new OccurenceRange { },

                Context = new ContextForCreateScheduleDTO
                {
                    GroupID = nonExistentId,
                    MentorID = existentMentorId
                }
            };

            _studentGroupRepositoryMock.Setup(x => x.IsEntityExistAsync(nonExistentId)).ReturnsAsync(false);

            _mentorRepositoryMock.Setup(x => x.IsEntityExistAsync(existentMentorId)).ReturnsAsync(true);

            _accountRepositoryMock.Setup(x => x.GetAccountCredentialsById(existentMentorId)).ReturnsAsync(new Account { IsActive = true });

            Initialize(createScheduleDto);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.CreateScheduleAsync(createScheduleDto);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task CreateScheduleAsync_NotValidMentorId_ShouldReturnValidationError()
        {
            //Arrange
            var createScheduleDto = new CreateScheduleDto
            {
                Pattern = new PatternForCreateScheduleDTO
                {
                    Type = PatternType.Daily,
                    Interval = 1
                },

                Range = new OccurenceRange { },

                Context = new ContextForCreateScheduleDTO
                {
                    GroupID = existentGroupId,
                    MentorID = nonExistentId
                }
            };

            _studentGroupRepositoryMock.Setup(x => x.IsEntityExistAsync(existentGroupId)).ReturnsAsync(true);

            _mentorRepositoryMock.Setup(x => x.IsEntityExistAsync(nonExistentId)).ReturnsAsync(false);

            _accountRepositoryMock.Setup(x => x.GetAccountCredentialsById(nonExistentId)).ReturnsAsync(new Account { IsActive = true });

            Initialize(createScheduleDto);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.CreateScheduleAsync(createScheduleDto);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task CreateScheduleAsync_NotValidThemeId_ShouldReturnValidationError()
        {
            //Arrange
            var createScheduleDto = new CreateScheduleDto
            {
                Pattern = new PatternForCreateScheduleDTO
                {
                    Type = PatternType.Daily,
                    Interval = 1
                },

                Range = new OccurenceRange { },

                Context = new ContextForCreateScheduleDTO
                {
                    GroupID = existentGroupId,
                    MentorID = existentMentorId,
                    ThemeID = nonExistentId
                }
            };

            _studentGroupRepositoryMock.Setup(x => x.IsEntityExistAsync(existentGroupId)).ReturnsAsync(true);

            _mentorRepositoryMock.Setup(x => x.IsEntityExistAsync(existentMentorId)).ReturnsAsync(true);

            _accountRepositoryMock.Setup(x => x.GetAccountCredentialsById(existentMentorId)).ReturnsAsync(new Account { IsActive = true });

            _themeRepositoryMock.Setup(x => x.IsEntityExistAsync(nonExistentId)).ReturnsAsync(false);

            Initialize(createScheduleDto);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.CreateScheduleAsync(createScheduleDto);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task CreateScheduleAsync_NotValidDaysOfWeekInWeekly_ShouldReturnValidationError()
        {
            //Arrange
            var createScheduleDto = new CreateScheduleDto
            {
                Pattern = new PatternForCreateScheduleDTO
                {
                    Type = PatternType.Weekly,
                    Interval = 1
                },

                Range = new OccurenceRange { },

                Context = new ContextForCreateScheduleDTO
                {
                    GroupID = existentGroupId,
                    MentorID = existentMentorId,
                    ThemeID = existentThemeId
                }
            };

            _studentGroupRepositoryMock.Setup(x => x.IsEntityExistAsync(existentGroupId)).ReturnsAsync(true);

            _mentorRepositoryMock.Setup(x => x.IsEntityExistAsync(existentMentorId)).ReturnsAsync(true);

            _accountRepositoryMock.Setup(x => x.GetAccountCredentialsById(existentMentorId)).ReturnsAsync(new Account { IsActive = true });

            _themeRepositoryMock.Setup(x => x.IsEntityExistAsync(existentThemeId)).ReturnsAsync(true);

            Initialize(createScheduleDto);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.CreateScheduleAsync(createScheduleDto);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task CreateScheduleAsync_NotValidDatesOfWeekInAbsoluteMonthly_ShouldReturnValidationError()
        {
            //Arrange
            var createScheduleDto = new CreateScheduleDto
            {
                Pattern = new PatternForCreateScheduleDTO
                {
                    Type = PatternType.AbsoluteMonthly,
                    Interval = 1
                },

                Range = new OccurenceRange { },

                Context = new ContextForCreateScheduleDTO
                {
                    GroupID = existentGroupId,
                    MentorID = existentMentorId,
                    ThemeID = existentThemeId
                }
            };

            _studentGroupRepositoryMock.Setup(x => x.IsEntityExistAsync(existentGroupId)).ReturnsAsync(true);

            _mentorRepositoryMock.Setup(x => x.IsEntityExistAsync(existentMentorId)).ReturnsAsync(true);

            _accountRepositoryMock.Setup(x => x.GetAccountCredentialsById(existentMentorId)).ReturnsAsync(new Account { IsActive = true });

            _themeRepositoryMock.Setup(x => x.IsEntityExistAsync(existentThemeId)).ReturnsAsync(true);

            Initialize(createScheduleDto);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.CreateScheduleAsync(createScheduleDto);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task CreateScheduleAsync_NotValidDaysOfWeekInRelativeMonthly_ShouldReturnValidationError()
        {
            //Arrange
            var createScheduleDto = new CreateScheduleDto
            {
                Pattern = new PatternForCreateScheduleDTO
                {
                    Type = PatternType.AbsoluteMonthly,
                    Interval = 1
                },

                Range = new OccurenceRange { },

                Context = new ContextForCreateScheduleDTO
                {
                    GroupID = existentGroupId,
                    MentorID = existentMentorId,
                    ThemeID = existentThemeId
                }
            };

            _studentGroupRepositoryMock.Setup(x => x.IsEntityExistAsync(existentGroupId)).ReturnsAsync(true);

            _mentorRepositoryMock.Setup(x => x.IsEntityExistAsync(existentMentorId)).ReturnsAsync(true);

            _accountRepositoryMock.Setup(x => x.GetAccountCredentialsById(existentMentorId)).ReturnsAsync(new Account { IsActive = true });

            _themeRepositoryMock.Setup(x => x.IsEntityExistAsync(existentThemeId)).ReturnsAsync(true);

            Initialize(createScheduleDto);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.CreateScheduleAsync(createScheduleDto);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task CreateScheduleAsync_NullRequest_ShouldReturnValidationError()
        {
            //Arrange
            CreateScheduleDto createScheduleDto = null;

            Initialize(createScheduleDto);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.CreateScheduleAsync(createScheduleDto);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }
        
        [Fact]
        public async Task GetEventOccurrenceByIdAsync_ExistingId_ShouldReturnExpectedEventOccurrenceDTO()
        {
            //Arrange
            Initialize(validScheduleDTO);

            _eventOccuranceRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(validEventOccurrence);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.GetEventOccurrenceByIdAsync(id);

            //Assert
            result.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(_mapper.Map<EventOccurrenceDTO>(validEventOccurrence));
        }

        [Fact]
        public async Task GetEventOccurrenceByIdAsync_NonExistingId_ShouldReturnNotFound()
        {
            //Arrange
            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.GetEventOccurrenceByIdAsync(nonExistentId);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.NotFound);
        }

        [Fact]
        public async Task GetEventsFiltered_ValidScheduledEventFilterRequest_ShouldReturnFilteredListOfScheduleEventDTO()
        {
            //Arrange
            var validRequest = new ScheduledEventFilterRequestDTO
            {
                MentorID = existentMentorId,
                GroupID = existentGroupId
            };

            var expectedFilteredList = new List<ScheduledEventDTO>()
            {

            new ScheduledEventDTO
            {
                StudentGroupId = existentGroupId,
                ThemeId = existentThemeId,
                MentorId = existentMentorId,
                EventStart = startDate,
                EventFinish = finishDate,
                EventOccuranceId = nonExistentEventOccuranceId,
                Id = id
            }
            };

            _scheduleRepositoryMock.Setup(x => x.GetEventsFilteredAsync(validRequest)).ReturnsAsync(new List<ScheduledEvent>()
            {
            new ScheduledEvent
            {
                StudentGroupId = existentGroupId,
                ThemeId = existentThemeId,
                MentorId = existentMentorId,
                EventStart = startDate,
                EventFinish = finishDate
            } });

            _mentorRepositoryMock.Setup(x => x.IsEntityExistAsync(existentMentorId)).ReturnsAsync(true);

            _studentGroupRepositoryMock.Setup(x => x.IsEntityExistAsync(existentGroupId)).ReturnsAsync(true);

            Initialize(validScheduleDTO);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.GetEventsFiltered(validRequest);

            //Assert
            result.Should().NotBeNull();
            result.Data.First().Should().BeEquivalentTo(expectedFilteredList.First());
        }

        [Fact]
        public async Task GetEventsFiltered_StartDateBiggerThanFinishDate_ShouldReturnValidationError()
        {
            // Arrange
            var finishDate = DateTime.Now;
            var invalidStartDate = finishDate.AddDays(1);

            var scheduledEventFilterDTO = Get_ScheduledEventFilterRequestDTO(
                startDate: (DateTime?)invalidStartDate,
                finishDate: (DateTime?)finishDate);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            // Act
            var result = await scheduleService.GetEventsFiltered(scheduledEventFilterDTO);

            // Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task GetEventsFiltered_NonValidScheduledEventFilterRequest_ShouldReturnValidationError()
        {
            //Arrange
            var nonValidRequest = new ScheduledEventFilterRequestDTO
            { 
                CourseID = nonExistentId,
                MentorID = nonExistentId,
                GroupID = nonExistentId
            };

            var courseRepositoryMock = new Mock<ICourseRepository>();
            courseRepositoryMock.Setup(x => x.IsEntityExistAsync(nonExistentId)).ReturnsAsync(false);

            _themeRepositoryMock.Setup(x => x.IsEntityExistAsync(nonExistentId)).ReturnsAsync(false);

            _mentorRepositoryMock.Setup(x => x.IsEntityExistAsync(nonExistentId)).ReturnsAsync(false);

            _studentGroupRepositoryMock.Setup(x => x.IsEntityExistAsync(nonExistentId)).ReturnsAsync(false);

            Initialize(validScheduleDTO);

            _unitOfWorkMock.Setup(x => x.CourseRepository).Returns(courseRepositoryMock.Object);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.GetEventsFiltered(nonValidRequest);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task DeleteScheduleByIdAsync_NonExistingId_ShouldReturnValidationError()
        {
            //Arrange
            _eventOccuranceRepositoryMock.Setup(x => x.IsEntityExistAsync(nonExistentId)).ReturnsAsync(false);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.DeleteScheduleByIdAsync(nonExistentId, startDate, finishDate);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task DeleteScheduleByIdAsync_ExistingId_ShouldReturn()
        {
            //Arrange
            validEventOccurrence.ScheduledEvents = new List<ScheduledEvent>()
                {
                    new ScheduledEvent
                    {
                        StudentGroupId = existentGroupId,
                        ThemeId = existentThemeId,
                        MentorId = existentMentorId,
                        EventStart = startDate,
                        EventFinish = finishDate,
                        EventOccurrenceId = existentEventOccuranceId
                    }
                };

            var expected = new EventOccurrenceDTO
            {
                StudentGroupId = existentGroupId,
                EventStart = startDate,
                EventFinish = finishDate,
                Events = new List<ScheduledEventDTO>()
                {
                    new ScheduledEventDTO()
                {
                        StudentGroupId = existentGroupId,
                        ThemeId = existentThemeId,
                        MentorId = existentMentorId,
                        EventStart = startDate,
                        EventFinish = finishDate,
                        EventOccuranceId = existentEventOccuranceId
                } },
                Id = id,
                Storage = EventOccuranceStorageParser.GetPatternStorageValue(validScheduleDTO.Pattern)
            };

            _eventOccuranceRepositoryMock.Setup(x => x.IsEntityExistAsync(existingId)).ReturnsAsync(true);
            _eventOccuranceRepositoryMock.Setup(x => x.GetByIdAsync(existingId)).ReturnsAsync(validEventOccurrence);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.DeleteScheduleByIdAsync(existingId, startDate, finishDate);

            //Assert
            result.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task UpdateEventOccurrenceById_ExistingIdValidScheduleDTO_ShouldReturnExpectedEventOccurrenceDTO()
        {
            //Arrange
            validEventOccurrence.ScheduledEvents = new List<ScheduledEvent>()
                {
                    new ScheduledEvent
                    {
                        StudentGroupId = newGroupId,
                        ThemeId = newThemeId,
                        MentorId = existentMentorId,
                        EventStart = startDate,
                        EventFinish = finishDate,
                        EventOccurrenceId = existingId
                    }
                };

            _eventOccuranceRepositoryMock.Setup(x => x.IsEntityExistAsync(1)).ReturnsAsync(true);
            _eventOccuranceRepositoryMock.Setup(x => x.GetByIdAsync(existingId)).ReturnsAsync(validEventOccurrence);

            _scheduleRepositoryMock.Setup(x => x.IsEntityExistAsync(existingId)).ReturnsAsync(true);

            _themeRepositoryMock.Setup(x => x.IsEntityExistAsync(existentThemeId)).ReturnsAsync(true);
            
            _mentorRepositoryMock.Setup(x => x.IsEntityExistAsync(existentMentorId)).ReturnsAsync(true);

            _accountRepositoryMock.Setup(x => x.GetAccountCredentialsById(existentMentorId)).ReturnsAsync(new Account { IsActive = true });

            _studentGroupRepositoryMock.Setup(x => x.IsEntityExistAsync(existentGroupId)).ReturnsAsync(true);

            Initialize(validScheduleDTO);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.UpdateEventOccurrenceById(existingId, validScheduleDTO);

            //Assert
            result.Should().NotBeNull();
            result.Data.Should().BeEquivalentTo(_mapper.Map<EventOccurrenceDTO>(validEventOccurrence));
        }

        [Fact]
        public async Task UpdateEventOccurrenceById_NonExistingIdValidRequestScheduleDTO_ShouldReturnValidationError()
        {
            //Arrange
            _eventOccuranceRepositoryMock.Setup(x => x.IsEntityExistAsync(nonExistentId)).ReturnsAsync(false);

            _themeRepositoryMock.Setup(x => x.IsEntityExistAsync(existentThemeId)).ReturnsAsync(true);

            _mentorRepositoryMock.Setup(x => x.IsEntityExistAsync(existentMentorId)).ReturnsAsync(true);

            _accountRepositoryMock.Setup(x => x.GetAccountCredentialsById(existentMentorId)).ReturnsAsync(new Account { IsActive = true });

            _studentGroupRepositoryMock.Setup(x => x.IsEntityExistAsync(existentGroupId)).ReturnsAsync(true);

            Initialize(validScheduleDTO);

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.UpdateEventOccurrenceById(nonExistentId, validScheduleDTO);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }

        [Fact]
        public async Task UpdateEventOccurrenceById_ExistingIdNullRequestScheduleDTO_ShouldReturnValidationError()
        {
            //Arrange
            CreateScheduleDto request = null;

            var scheduleService = new ScheduleService(_unitOfWorkMock.Object, _mapper, _scheduledEventFactory);

            //Act
            var result = await scheduleService.UpdateEventOccurrenceById(existingId, request);

            //Assert
            result.Should().NotBeNull();
            result.Error.Code.Should().BeEquivalentTo(ErrorCode.ValidationError);
        }
    }
}
