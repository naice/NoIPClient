using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoIPClient.NoIPApi
{
    internal class NoIPApiSettings
    {
        public string UserName { get; set; } = "jmarchewka";
        public string Password { get; set; } = "jensm1985";
        public string[] UpdateHosts { get; set; } = new string[] { "jensm.noip.me", "jensm.hopto.org" };
    }
}
