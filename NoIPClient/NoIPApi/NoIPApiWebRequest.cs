using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace NoIPClient.NoIPApi
{
    internal class HostResult
    {
        public string HostName { get; set; }
        public bool IsOkay { get; set; }
        public bool IsNoChange { get; set; }
        public bool IsBadHostName { get; set; }


        public static HostResult Create(string hostname, string line)
        {
            HostResult result = null;
            if (!string.IsNullOrEmpty(line))
            {
                var spl = line.Trim().Split(' ');
                if (spl.Length == 2)
                {
                    bool isOkay = spl[0] == "good";
                    bool isNoChange = spl[0] == "nochg";
                    isOkay = isNoChange ? true : isOkay;

                    result = new HostResult() { HostName = hostname, IsNoChange = isNoChange, IsOkay = isOkay };
                }
                else if (spl.Length == 1)
                {
                    if (spl[0] != "nohost") throw new InvalidOperationException("unkown response type, update api!");
                    result = new HostResult() { HostName = hostname, IsBadHostName = true };
                }
            }

            return result;
        }
    }
    internal enum UpdateResultState { Success, BadAuth, BadAgent, Abuse, NoDonator, Error }
    internal class UpdateResult
    {
        List<HostResult> _hosts = new List<HostResult>();

        public ReadOnlyCollection<HostResult> Hosts { get { return _hosts.AsReadOnly(); } }
        public UpdateResultState State { get; internal set; } = UpdateResultState.Error;
        public bool IsOkay => State == UpdateResultState.Success;
        public Exception Exception { get; set; }

        public static UpdateResult Create(string[] hosts, string response)
        {
            UpdateResult result = new UpdateResult();

            if (!string.IsNullOrEmpty(response))
            {
                result.State = TryGetState(response);
                if (result.State == UpdateResultState.Success)
                {
                    var lines = response.Trim().Split('\n');
                    if (lines.Length == hosts.Length)
                    {
                        for (int i = 0; i < lines.Length; i++)
                        {
                            result._hosts.Add(HostResult.Create(hosts[i], lines[i]));
                        }
                    }
                    else
                    {
                        result.State = UpdateResultState.Error;
                    }
                }
            }

            return result;
        }

        private static readonly Dictionary<string, UpdateResultState> _stateMap = new Dictionary<string, UpdateResultState>()
        {
            {"badauth", UpdateResultState.Abuse},
            {"badagent", UpdateResultState.Abuse},
            {"!donator", UpdateResultState.Abuse},
            {"abuse", UpdateResultState.Abuse},
            {"911", UpdateResultState.Abuse},
        };
        private static UpdateResultState TryGetState(string s)
        {
            UpdateResultState state = UpdateResultState.Success;

            s = s.Trim();
            foreach (var key in _stateMap.Keys)
            {
                if (s.StartsWith(key))
                {
                    state = _stateMap[key]; break;
                }
            }

            return state;
        }
    }
    
    internal static class NoIPApiWebRequest
    {
        private static readonly string UpdateUrlSecure = "http://dynupdate.no-ip.com/nic/update?hostname={0}&myip={1}";

        private static string CreateUrlHostnamesString(string [] hosts)
        {
            return string.Join(",", hosts);
        }

        public static async Task<UpdateResult> Update(string username, string password, string[] hosts, string ipAddress)
        {
            UpdateResult result = new UpdateResult();
            try
            {
                string hostStr = CreateUrlHostnamesString(hosts);

                if (!string.IsNullOrEmpty(hostStr))
                {
                    string updateUrl = string.Format(NoIPApiWebRequest.UpdateUrlSecure, hostStr, ipAddress);

                    try
                    {
                        using (Stream requestAndGetResponse = await NoIPApiWebRequest.CreateRequestAndGetResponse(updateUrl, username, password))
                        {
                            if (requestAndGetResponse != null)
                            {
                                using (StreamReader streamReader = new StreamReader(requestAndGetResponse, Encoding.UTF8))
                                {
                                    string responseStr = streamReader.ReadToEnd().TrimEnd();
                                    result = UpdateResult.Create(hosts, responseStr);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.HResult == -2147024809)
                        {
                            // server did not provide content but thats a success anyway.
                            result.State = UpdateResultState.Success;
                        }
                        else
                        {
                            result.Exception = ex;
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                result.Exception = ex;
            }
            return result;
        }

        public static async Task<IPAddress> DetectIp()
        {
            return await DigMyIP(string.Format("ipcast1.dynupdate.noip.com"), 8253);
        }

        private static string _userAgent;
        private static string UserAgent
        {
            get
            {
                if (string.IsNullOrEmpty(_userAgent))
                {
                    Package package = Package.Current;
                    PackageId packageId = package.Id;
                    PackageVersion version = packageId.Version;

                    var versionString = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
                    _userAgent = string.Format("UWP IoT NoIPClient by Jens Marchewka/{0} jens.marchewka@gmail.com", versionString);
                }

                return _userAgent;
            }
        }
        private static async Task<Stream> CreateRequestAndGetResponse(string url, string username, string password)
        {
            HttpWebRequest httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
            httpWebRequest.Headers[HttpRequestHeader.UserAgent] = UserAgent;
            httpWebRequest.Headers[HttpRequestHeader.CacheControl] = "no-cache";
            httpWebRequest.Headers[HttpRequestHeader.Pragma] = "no-cache";
            httpWebRequest.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
            //httpWebRequest.Credentials = new NetworkCredential(username, password);

            using (HttpWebResponse httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse)
            {
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                    return httpWebResponse.GetResponseStream();
            }
            return (Stream)null;
        }

        public static async Task<IPAddress> DigMyIP(string ipAddress, int port)
        {
            byte[] buffer1 = new byte[20]
            {
                (byte) 166,
                (byte) 124,
                (byte) 1,
                (byte) 0,
                (byte) 0,
                (byte) 1,
                (byte) 0,
                (byte) 0,
                (byte) 0,
                (byte) 0,
                (byte) 0,
                (byte) 0,
                (byte) 2,
                (byte) 105,
                (byte) 112,
                (byte) 0,
                (byte) 0,
                (byte) 1,
                (byte) 0,
                (byte) 1
            };
            IPAddress ipAddress1 = (IPAddress)null;
            byte[] buffer2 = new byte[512];
            Socket socket = (Socket)null;
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 10000);
                foreach (IPAddress address1 in (await Dns.GetHostEntryAsync(ipAddress)).AddressList)
                {
                    socket.SendTo(buffer1, (EndPoint)new IPEndPoint(address1, port));
                    int count = socket.Receive(buffer2);
                    byte[] numArray = new byte[count];
                    Buffer.BlockCopy((Array)buffer2, 0, (Array)numArray, 0, count);
                    if ((int)buffer1[0] == (int)numArray[0] && (int)buffer1[1] == (int)numArray[1] && (int)BitConverter.ToInt16(numArray, 6) == 256)
                    {
                        byte[] address2 = new byte[4];
                        Buffer.BlockCopy((Array)numArray, numArray.Length - 4, (Array)address2, 0, address2.Length);
                        ipAddress1 = new IPAddress(address2);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (socket != null)
                    socket.Dispose();
            }
            return ipAddress1;
        }
    }
}
