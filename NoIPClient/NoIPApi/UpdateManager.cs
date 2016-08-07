using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NoIPClient.NoIPApi
{
    internal static class UpdateManager
    {
        public static async Task<UpdateResult> Update(NoIPApiSettings settings)
        {
            var ip = await NoIPApiWebRequest.DetectIp();

            if (ip != null)
            {
                return await NoIPApiWebRequest.Update(settings.UserName, settings.Password, settings.UpdateHosts, ip.ToString());
            }

            return null;
        }

        public static async Task UpdateLoop(TimeSpan delay, NoIPApiSettings settings)
        {
            var currentDelay = delay;
            var ip = IPAddress.None;

            while (true)
            {
                var newIP = await NoIPApiWebRequest.DetectIp();
                if (newIP != null && newIP != ip)
                {
                    ip = newIP;
                    var result = await NoIPApiWebRequest.Update(settings.UserName, settings.Password, settings.UpdateHosts, ip.ToString());

                    if (result.Exception != null) throw result.Exception;

                    if (!result.IsOkay)
                    {
                        currentDelay = TimeSpan.FromMinutes(30);
                        bool fatal = false;
                        switch (result.State)
                        {
                            case UpdateResultState.BadAuth:
                            case UpdateResultState.BadAgent:
                            case UpdateResultState.Abuse:
                            case UpdateResultState.NoDonator:
                                fatal = true;
                                break;
                            default:
                                break;
                        }

                        if (fatal) break; // end loop
                    }
                    else
                    {
                        currentDelay = delay;
                    }
                }

                await Task.Delay(currentDelay);
            }
        }
    }
}
