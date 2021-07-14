using CharlieBackend.Data;
using Microsoft.EntityFrameworkCore;
using CharlieBackend.Business.Options;
using CharlieBackend.Business.Services;
using Microsoft.Extensions.Configuration;
using CharlieBackend.Data.Repositories.Impl;
using Microsoft.Extensions.DependencyInjection;
using CharlieBackend.Business.Services.Interfaces;
using CharlieBackend.Data.Repositories.Impl.Interfaces;
using CharlieBackend.Business.Services.FileServices;
using CharlieBackend.Business.Services.ScheduleServiceFolder;
using CharlieBackend.Business.Helpers;

namespace CharlieBackend.Root
{
    public class CompositionRoot
    {
        // Inject your dependencies here
        public static void InjectDependencies(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationContext>(options =>
            {
                options.UseMySql(configuration.GetConnectionString("DefaultConnection"),
                  b => b.MigrationsAssembly("CharlieBackend.Api"));
            });

            services.AddControllers()
                    .AddNewtonsoftJson(options =>
                        options.SerializerSettings.ReferenceLoopHandling
                        = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            services.Configure<AuthOptions>(configuration.GetSection("AuthOptions"));

            #region
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ILessonService, LessonService>();
            services.AddScoped<IThemeService, ThemeService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<IMentorService, MentorService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IStudentGroupService, StudentGroupService>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ILessonRepository, LessonRepository>();
            services.AddScoped<IThemeRepository, ThemeRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<IMentorRepository, MentorRepository>();
            services.AddScoped<IMentorOfCourseRepository, MentorOfCourseRepository>();
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<IStudentGroupRepository, StudentGroupRepository>();
            services.AddScoped<ISecretaryService, SecretaryService>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IScheduleService, ScheduleService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAttachmentService, AttachmentService>();
            services.AddScoped<IHomeworkService, HomeworkService>();
            services.AddScoped<IBlobService, BlobService>();
            services.AddScoped<IBaseFileService, BaseFileService>();
            services.AddScoped<IGroupXlsFileImporter, GroupXlsFileImporter>();
            services.AddScoped<IStudentXlsFileImporter, StudentXlsFileImporter>();
            services.AddScoped<IThemeXlsFileImporter, ThemeXlsFileImporter>();
            services.AddScoped<IScheduledEventHandlerFactory, ScheduledEventHandlerFactory>();
            services.AddScoped<IHomeworkStudentService, HomeworkStudentService>();
            services.AddScoped<IEventsService, EventsService>();
            services.AddScoped<IJWTGenerator, JWTGenerator>();
            #endregion
        }
    }
}
