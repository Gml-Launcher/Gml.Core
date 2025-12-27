namespace Gml.Domains.Integrations;

public class AuthCustomResponse
{
    public string Login { get; set; } = null!;

    public string UserUuid { get; set; } = null!;
    public bool? IsSlim { get; set; }
    public string? Message { get; set; }
}
