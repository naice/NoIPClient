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
        BackgroundTaskDeferral deferral = null;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //
            deferral = taskInstance.GetDeferral();

            NoIPApi.UpdateManager.UpdateLoop(TimeSpan.FromMinutes(5), new NoIPApi.NoIPApiSettings())
                .ContinueWith(A => deferral.Complete());

            //NoIPApi.UpdateManager.Update(new NoIPApi.NoIPApiSettings())
            //    .ContinueWith(A => {
            //        deferral.Complete();
            //    });

        }
        
    }
}
