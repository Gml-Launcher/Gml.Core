using System;

namespace Gml.Core.User
{
    public class AuthUserHistory
    {
        public DateTime Date { get; set; }

        public string Device { get; set; }

        internal AuthUserHistory()
        {

        }

        internal AuthUserHistory(DateTime date)
        {
            Date = date;
        }

        public static AuthUserHistory Create(string device)
        {
            return new AuthUserHistory(DateTime.Now)
            {
                Device = device
            };
        }
    }
}
