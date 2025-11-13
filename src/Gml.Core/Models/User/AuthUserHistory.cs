using System;

namespace Gml.Models.User;

public class AuthUserHistory
{
    public AuthUserHistory()
    {
    }

    internal AuthUserHistory(DateTime date)
    {
        Date = date;
    }

    public DateTime Date { get; set; }
    public string Device { get; set; }
    public string? Address { get; set; }
    public string Protocol { get; set; }
    public string? Hwid { get; set; }

    public static AuthUserHistory Create(string device, string protocol, string? hwid, string? address)
    {
        return new AuthUserHistory(DateTime.Now)
        {
            Device = device,
            Protocol = protocol,
            Address = address,
            Hwid = hwid
        };
    }
}
