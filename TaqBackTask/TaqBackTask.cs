﻿using System;
using Taq;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using System.IO;
using Windows.UI.Xaml.Media.Imaging;

namespace TaqBackTask
{
    public sealed class TaqBackTask : XamlRenderingBackgroundTask
    {
        private static TaqModel m = new TaqModel();

        protected async override void OnRun(IBackgroundTaskInstance taskInstance)
        {
#if DEBUG
            var tbtLog = await ApplicationData.Current.LocalFolder.CreateFileAsync("TbtLog.txt", CreationCollisionOption.OpenIfExists);
            using (var s = await tbtLog.OpenStreamForWriteAsync())
            {
                s.Seek(0, SeekOrigin.End);
                var sw = new StreamWriter(s);
                var ct = DateTime.Now;
                sw.WriteLine(ct.ToString());
                sw.Flush();
            }
#endif
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            // We assume that this method has a high probability of a successfull run
            // after a failed run with exceptions. It means the success rate of a run is almost independent of the previous runs. So, we just catch exceptions and do nothing, so that this baskgroundtask won't crash and exit.
            try
            {

                // Download the feed.
                var res = await m.downloadDataXml();
            }
            catch (Exception ex)
            {
                // Ignore.
            }
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
                if((bool)m.localSettings.Values["AutoPos"] && (bool)m.localSettings.Values["BgMainSiteAutoPos"])
                {
                    await m.findNearestSite();
                    m.localSettings.Values["subscrSite"] = m.nearestSite;
                }
                await m.loadCurrSite();
                await m.loadSubscrSiteXml();

                // Update the live tile with the feed items.
                await m.updateLiveTile();

                // Send notifications.
                m.sendSubscrSitesNotifications();
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
