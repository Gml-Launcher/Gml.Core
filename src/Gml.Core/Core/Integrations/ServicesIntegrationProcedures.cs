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

namespace Gml.Core.Integrations
{
    public class ServicesIntegrationProcedures : IServicesIntegrationProcedures
    {
        private readonly IStorageService _storage;
        private IEnumerable<IAuthServiceInfo>? _authServices;

        public ServicesIntegrationProcedures(IStorageService storage)
        {
            _storage = storage;
        }

        public Task<AuthType> GetAuthType()
        {
            return _storage.GetAsync<AuthType>(StorageConstants.AuthType);
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
                new AuthServiceInfo("UnicoreCMS", AuthType.UnicoreCMS)
            }.AsEnumerable());
        }

        public async Task<IAuthServiceInfo?> GetActiveAuthService()
        {
            if (_authServices == null || _authServices.Count() == 0)
                _authServices = await GetAuthServices();

            return await _storage.GetAsync<AuthServiceInfo>(StorageConstants.ActiveAuthService);
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
                await _storage.SetAsync<object>(StorageConstants.AuthType, null);
                await _storage.SetAsync<object>(StorageConstants.ActiveAuthService, null);

                return;
            }

            await _storage.SetAsync(StorageConstants.AuthType, service.AuthType);
            await _storage.SetAsync(StorageConstants.ActiveAuthService, service);
        }

        public async Task<string> GetSkinServiceAsync()
        {
            return await _storage.GetAsync<string>(StorageConstants.SkinUrl)
                   ?? throw new Exception("Сервис скинов не настроен");
        }

        public async Task<string> GetCloakServiceAsync()
        {
            return await _storage.GetAsync<string>(StorageConstants.CloakUrl)
                   ?? throw new Exception("Сервис плащей не настроен");
        }

        public Task SetSkinServiceAsync(string url)
        {
            return _storage.SetAsync(StorageConstants.SkinUrl, url);
        }

        public Task SetCloakServiceAsync(string url)
        {
            return _storage.SetAsync(StorageConstants.CloakUrl, url);
        }

        public Task<string?> GetSentryService()
        {
            return _storage.GetAsync<string>(StorageConstants.SentrySdnUrl);
        }

        public Task SetSentryService(string url)
        {
            return _storage.SetAsync(StorageConstants.SentrySdnUrl, url);
        }

        public Task UpdateDiscordRpc(IDiscordRpcClient client)
        {
            return _storage.SetAsync(StorageConstants.DiscordRpcClient, client);
        }

        public async Task<IDiscordRpcClient?> GetDiscordRpc()
        {
            return await _storage
                .GetAsync<DiscordRpcClient>(StorageConstants.DiscordRpcClient)
                .ConfigureAwait(false);
        }
    }
}
