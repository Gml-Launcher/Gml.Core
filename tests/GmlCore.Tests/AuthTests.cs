using Gml;
using Gml.Core.Launcher;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace GmlCore.Tests;

public class AuthTests
{
    private const string LauncherName = "GamerVIILauncher";
    private const string SecurityKey = "gfweagertghuysergfbsuyerbgiuyserg";

    private IGmlManager _gmlManager;

    [OneTimeSetUp]
    public async Task SetupOnce()
    {
        _gmlManager = new GmlManager(new GmlSettings(LauncherName, SecurityKey, httpClient: new HttpClient())
        {
            TextureServiceEndpoint = "http://gml-web-skins:8085"
        });
    }

    [Test]
    public async Task CheckEndpointUpdate()
    {
        var service = await _gmlManager.Integrations.GetAuthService(AuthType.Any);

        if (service == null)
            throw new Exception("No active auth service");

        service.Endpoint = "http://localhost:8085/api/v1/auth/login";

        await _gmlManager.Integrations.SetActiveAuthService(service);

        var result = await _gmlManager.Integrations.GetActiveAuthService() ?? throw new Exception();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(service.Name, Is.EqualTo(result.Name));
            Assert.That(service.Endpoint, Is.EqualTo(result.Endpoint));
        });
    }

    [Test]
    public async Task CheckAuthTypesUpdate()
    {
        foreach (AuthType authType in Enum.GetValues(typeof(AuthType)))
        {
            if (authType == AuthType.Undefined) continue;

            var service = await _gmlManager.Integrations.GetAuthService(authType);

            if (service == null) continue;

            service.Endpoint = $"http://localhost:8085/api/v1/{authType.ToString().ToLower()}/login";

            await _gmlManager.Integrations.SetActiveAuthService(service);

            var result = await _gmlManager.Integrations.GetActiveAuthService();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(service.Name, Is.EqualTo(result.Name));
                Assert.That(service.Endpoint, Is.EqualTo(result.Endpoint));
                Assert.That(service.AuthType, Is.EqualTo(authType));
            });
        }
    }
}
