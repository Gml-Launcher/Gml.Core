using System;

namespace Gml.Core.User
{
    public class AuthUserHistory
    {
        internal AuthUserHistory()
        {
        }

        internal AuthUserHistory(DateTime date)
        {
            Date = date;
        }

        public DateTime Date { get; set; }

        public string Device { get; set; }

        public static AuthUserHistory Create(string device)
        {
            return new AuthUserHistory(DateTime.Now)
            {
                Device = device
            };
        }
    }
}
