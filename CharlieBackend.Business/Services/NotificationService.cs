using EasyNetQ;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CharlieBackend.Core.Entities;
using CharlieBackend.Business.Services.Interfaces;
using CharlieBackend.Core.IntegrationEvents.Events;
using System;
using CharlieBackend.Core.DTO.Student;
using System.Collections.Generic;

namespace CharlieBackend.Business.Services
{
    public class NotificationService : INotificationService
    {
        private const string queueAccountApproved = "AccountApproved";
        private const string queueRegistrationSuccess = "RegistrationSuccess";
        private const string queueCourseOpened = "CourseOpened";
        private readonly IBus _bus;
        private readonly ILogger<NotificationService> _logger;
        private readonly IStudentService _studentService;

        public NotificationService(IBus bus, ILogger<NotificationService> logger, IStudentService studentService)
        {
            _bus = bus;
            _logger = logger;
            _studentService = studentService;
        }

        public async Task AccountApproved(Account account)
        {
            _logger.LogInformation($"AccountApprovedEvent has been sent for user " +
                                   $"{account.FirstName} {account.LastName}");

            await _bus.SendReceive.SendAsync(queueAccountApproved, new AccountApprovedEvent(account.Email,
                                       account.FirstName, account.LastName, account.Role));
        }

        public async Task RegistrationSuccess(Account account)
        {
            _logger.LogInformation($"RegistrationSuccessEvent has been sent for user " +
                                   $"{account.FirstName} {account.LastName}");

            await _bus.SendReceive.SendAsync(queueRegistrationSuccess, new RegistrationSuccessEvent(account.Email,
                                       account.FirstName, account.LastName));
        }

        public async Task CourseOpened(DateTime startDate, string courseName) 
        {
            var students = await _studentService.GetAllStudentsAsync();

            _logger.LogInformation($"");

            await _bus.SendReceive.SendAsync(queueCourseOpened, new CourseOpenedEvent(startDate, courseName, (List<StudentDto>)students));
        }
    }
}
