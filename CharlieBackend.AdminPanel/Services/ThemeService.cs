using CharlieBackend.AdminPanel.Models.Theme;
using CharlieBackend.AdminPanel.Services.Interfaces;
using CharlieBackend.AdminPanel.Utils.Interfaces;
using CharlieBackend.Core.DTO.Theme;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharlieBackend.AdminPanel.Services
{
    public class ThemeService : IThemeService
    {
        private readonly IApiUtil _apiUtil;
        private readonly ThemesApiEndpoints _themesApiEndpoints;

        public ThemeService(IApiUtil apiUtil, IOptions<ApplicationSettings> options)
        {
            _apiUtil = apiUtil;
            _themesApiEndpoints = options.Value.Urls.ApiEndpoints.Themes;
        }

        public async Task DeleteTheme(long id)
        {
            var deleteThemeEndpoint = string
                .Format(_themesApiEndpoints.DeleteThemeEndpoint, id);

            await
                _apiUtil.DeleteAsync<ThemeViewModel>(deleteThemeEndpoint);
        }

        public async Task UpdateTheme(long id, UpdateThemeDto UpdateDto)
        {
            var updateThemeEndpoint = string
                .Format(_themesApiEndpoints.UpdateThemeEndpoint, id);

            await
                _apiUtil.PutAsync<UpdateThemeDto>(updateThemeEndpoint, UpdateDto);
        }

        public async Task<IList<ThemeViewModel>> GetAllThemesAsync()
        {
            var getAllThemesEndpoint = _themesApiEndpoints.GetAllThemesEndpoint;

            return await
                _apiUtil.GetAsync<IList<ThemeViewModel>>(getAllThemesEndpoint);
        }

        public async Task AddThemeAsync(CreateThemeDto themeDto)
        {
            var addThemeEndpoint = _themesApiEndpoints.AddThemeEndpoint;

            await
                _apiUtil.CreateAsync<CreateThemeDto>(addThemeEndpoint, themeDto);
        }
    }
}
