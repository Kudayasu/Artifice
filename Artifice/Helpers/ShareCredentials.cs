using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace Artifice
{
    public static class Helpers
    {
        // Class to hold share credentials
        public class ShareCredentials
        {
            public string HostnameOrIpAddress { get; }
            public string Path { get; }
            public string Username { get; }
            public string Password { get; }

            public ShareCredentials(string hostnameOrIpAddress, string path, string username, string password)
            {
                HostnameOrIpAddress = hostnameOrIpAddress;
                Path = path;
                Username = username;
                Password = password;
            }
        }

        public class WDPCredentials
        {
            public string WdpUsername { get; set; }
            public string WdpPassword { get; set; }

            public WDPCredentials(string username, string password)
            {
                WdpUsername = username;
                WdpPassword = password;
            }
        }
    }
}
