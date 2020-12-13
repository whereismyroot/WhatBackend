using EasyNetQ;
using Serilog.Context;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CharlieBackend.Core.DTO.EmailData;
using CharlieBackend.Core.IntegrationEvents.Events;
using WhatBackend.EmailRenderService.Services.Interfaces;
using System.Collections.Generic;
using CharlieBackend.Core.DTO.Student;

namespace WhatBackend.EmailRenderService.IntegrationEvents.EventHandling
{
    public class CourseOpenedHanlder
    {
        private const string queueName = "EmailSenderService";
        private readonly ILogger<CourseOpenedHanlder> _logger;
        private readonly IBus _bus;
        private readonly IMessageTemplateService _messageTemplate;

        public CourseOpenedHanlder(
                ILogger<CourseOpenedHanlder> logger,
                IBus bus,
                IMessageTemplateService messageTemplate
                )
        {
            _logger = logger;
            _bus = bus;
            _messageTemplate = messageTemplate;
        }

        public async Task HandleAsync(CourseOpenedEvent message)
        {
            using (LogContext.PushProperty("IntegrationEventContext", $"{message}"))
            {
                _logger.LogInformation($"Account has been approved: {message}");

                _logger.LogInformation("-----Publishing AccountApprovedEvent integration event----- ");

                for (int i = 0; i < message.Students.Count; i++)
                {
                    await _bus.SendReceive.SendAsync(queueName, new EmailData
                    {
                        RecipientMail = message.Students[i].Email,
                        EmailBody = _messageTemplate.GetCourseOpenedTemplate(message.CourseName 
                                + " reopening after " + message.StartDate.ToString() + ".")
                    });
                }
            }
        }
    }
}
