﻿using System;
using Taq;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml.Media.Imaging;

namespace TaqBackTask
{
    public sealed class BackTaskUpdateTiles : XamlRenderingBackgroundTask
    {
        private static TaqModel m = new TaqModel();

        protected async override void OnRun(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            // We assume that this method has a high probability of a successfull run
            // after a failed run with exceptions. It means the success rate of a run is almost independent of the previous runs. So, we just catch exceptions and do nothing, so that this baskgroundtask won't crash and exit.
            try
            {
                await m.loadAqXml();
            }
            catch (Exception ex)
            {
                // Ignore.
            }
            
            try
            { 
                m.convertXDoc2Dict();
                await m.loadCurrSite();
                await m.loadSubscrSiteXml();

                // Update the live tile with the feed items.
                await m.updateLiveTile();
            }
            catch (Exception ex)
            {
                // Do nothing.
            }
            finally
            {

                // Inform the system that the task is finished.
                deferral.Complete();
            }
        }

        volatile bool _cancelRequested = false;
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //
            // Indicate that the background task is canceled.
            //
            _cancelRequested = true;
        }
    }
}
