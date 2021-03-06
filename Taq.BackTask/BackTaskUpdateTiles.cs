﻿using System;
using System.Diagnostics;
using Taq;
using Taq.Shared.Models;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml.Media.Imaging;

namespace Taq.BackTask
{
    public sealed class BackTaskUpdateTiles : XamlRenderingBackgroundTask
    {
        private static TaqModel m = new TaqJsonModel();

        protected async override void OnRun(IBackgroundTaskInstance taskInstance)
        {
            // Don't place any code (including debug code) before GetDeferral!!!
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
            // We assume that this method has a high probability of a successfull run
            // after a failed run with exceptions. It means the success rate of a run is almost independent of the previous runs. So, we just catch exceptions and do nothing, so that this baskgroundtask won't crash and exit.
            await m.loadSubscrSiteXml();
            try
            {
                await m.loadAq2Dict();
            }
            catch (Exception ex)
            {
                // Ignore.
                Debug.WriteLine(ex.Message);
            }
            
            try
            { 
                await m.loadMainSite((string)m.localSettings.Values["MainSite"]);

                // Update the live tile with the feed items.
                await m.updateLiveTile();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                // Do nothing.
            }
            finally
            {

                // Inform the system that the task is finished.
                deferral.Complete();
            }
        }

        //volatile bool _cancelRequested = false;
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //
            // Indicate that the background task is canceled.
            //
            //_cancelRequested = true;
        }
    }
}
