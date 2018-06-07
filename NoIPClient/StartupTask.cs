using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace NoIPClient
{
    public sealed class StartupTask : IBackgroundTask
    {
        internal static string SETTINGS_FILE = "NoIPClient-Configuration.json";

        BackgroundTaskDeferral deferral = null;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            SETTINGS_FILE = System.IO.Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, SETTINGS_FILE);

            // get deferral
            deferral = taskInstance.GetDeferral();

            var settingsFile = new JsonFile<NoIPApi.NoIPApiSettings>();
            if (!settingsFile.Load(SETTINGS_FILE))
            {
                settingsFile.Content = new NoIPApi.NoIPApiSettings();
                if (!settingsFile.Save(SETTINGS_FILE, Newtonsoft.Json.Formatting.Indented))
                {
                    throw new Exception($"Can't write initial settings file '{SETTINGS_FILE}'.");
                }

                deferral.Complete();
                return;
            }

            NoIPApi.UpdateManager.UpdateLoop(TimeSpan.FromMinutes(5), settingsFile.Content)
                .ContinueWith(A => deferral.Complete());
        }
        
    }
}
