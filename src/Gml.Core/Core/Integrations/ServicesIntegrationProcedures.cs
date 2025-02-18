using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gml.Core.Constants;
using Gml.Core.Services.Storage;
using Gml.Models.Auth;
using Gml.Models.Discord;
using GmlCore.Interfaces.Auth;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Integrations;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;

namespace Gml.Core.Integrations
{
    public class ServicesIntegrationProcedures(
        IGmlSettings settings,
        IStorageService storage,
        IBugTrackerProcedures bugTracker)
        : IServicesIntegrationProcedures
    {
        public ITextureProvider TextureProvider { get; set; } = new TextureProvider(settings.TextureServiceEndpoint, bugTracker);
        public INewsListenerProvider NewsProvider { get; set; } = new NewsListenerProvider(TimeSpan.FromMinutes(1), storage);
        private IEnumerable<IAuthServiceInfo>? _authServices;


        public Task<AuthType> GetAuthType()
        {
            return storage.GetAsync<AuthType>(StorageConstants.AuthType);
        }

        public Task<IEnumerable<IAuthServiceInfo>> GetAuthServices()
        {
            return Task.FromResult(new List<IAuthServiceInfo>
            {
                new AuthServiceInfo("Undefined", AuthType.Undefined),
                new AuthServiceInfo("Any", AuthType.Any),
                new AuthServiceInfo("DataLifeEngine", AuthType.DataLifeEngine),
                new AuthServiceInfo("Azuriom", AuthType.Azuriom),
                new AuthServiceInfo("EasyCabinet", AuthType.EasyCabinet),
                new AuthServiceInfo("UnicoreCMS", AuthType.UnicoreCMS),
                new AuthServiceInfo("CustomEndpoint", AuthType.CustomEndpoint),
                // new AuthServiceInfo("NamelessMC", AuthType.NamelessMC)
            }.AsEnumerable());
        }

        public async Task<IAuthServiceInfo?> GetActiveAuthService()
        {
            if (_authServices == null || _authServices.Count() == 0)
                _authServices = await GetAuthServices();

            return await storage.GetAsync<AuthServiceInfo>(StorageConstants.ActiveAuthService);
        }

        public async Task<IAuthServiceInfo?> GetAuthService(AuthType authType)
        {
            if (_authServices == null || _authServices.Count() == 0)
                _authServices = await GetAuthServices();

            return _authServices.FirstOrDefault(c => c.AuthType == authType);
        }

        public async Task SetActiveAuthService(IAuthServiceInfo? service)
        {
            if (service == null)
            {
                await storage.SetAsync<object>(StorageConstants.AuthType, null);
                await storage.SetAsync<object>(StorageConstants.ActiveAuthService, null);

                return;
            }

            await storage.SetAsync(StorageConstants.AuthType, service.AuthType);
            await storage.SetAsync(StorageConstants.ActiveAuthService, service);
        }

        public async Task<string> GetSkinServiceAsync()
        {
            return await storage.GetAsync<string>(StorageConstants.SkinUrl)
                   ?? throw new Exception("Сервис скинов не настроен");
        }

        public async Task<string> GetCloakServiceAsync()
        {
            return await storage.GetAsync<string>(StorageConstants.CloakUrl)
                   ?? throw new Exception("Сервис плащей не настроен");
        }

        public Task SetSkinServiceAsync(string url)
        {
            return storage.SetAsync(StorageConstants.SkinUrl, url);
        }

        public Task SetCloakServiceAsync(string url)
        {
            return storage.SetAsync(StorageConstants.CloakUrl, url);
        }

        public Task<string?> GetSentryService()
        {
            return storage.GetAsync<string>(StorageConstants.SentrySdnUrl);
        }

        public Task SetSentryService(string url)
        {
            return storage.SetAsync(StorageConstants.SentrySdnUrl, url);
        }

        public Task UpdateDiscordRpc(IDiscordRpcClient client)
        {
            return storage.SetAsync(StorageConstants.DiscordRpcClient, client);
        }

        public async Task<IDiscordRpcClient?> GetDiscordRpc()
        {
            return await storage
                .GetAsync<DiscordRpcClient>(StorageConstants.DiscordRpcClient)
                .ConfigureAwait(false);
        }
    }
}
