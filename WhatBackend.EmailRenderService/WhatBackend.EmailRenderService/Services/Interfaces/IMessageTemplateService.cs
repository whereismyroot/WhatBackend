using System.Threading.Tasks;
using CharlieBackend.Core.IntegrationEvents.Events;

namespace WhatBackend.EmailRenderService.Services.Interfaces
{
    public interface IMessageTemplateService
    {
        string GetAccountApprovedTemplate(AccountApprovedEvent message);

        string GetRegistrationSuccessTemplate(RegistrationSuccessEvent message);

        string GetCourseOpenedTemplate(string message);
    }
}
