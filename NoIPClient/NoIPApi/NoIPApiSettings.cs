using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoIPClient.NoIPApi
{
    internal class NoIPApiSettings
    {
        public string UserName { get; set; } = "the user name";
        public string Password { get; set; } = "the password";
        public string[] UpdateHosts { get; set; } = new string[] { "alist.ofyour.hosts", "asmuch.asyou.want" };
    }
}
