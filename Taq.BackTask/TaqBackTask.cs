﻿//#define LOG_STEP
using System;
using Taq;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using System.IO;
using Windows.UI.Xaml.Media.Imaging;
using Taq.Shared.Models;
using Windows.ApplicationModel.Resources;
using System.Diagnostics;
using System.Threading;

namespace Taq.BackTask
{
    public sealed class TaqBackTask : XamlRenderingBackgroundTask
    {
        BackgroundTaskDeferral deferral;

        protected async override void OnRun(IBackgroundTaskInstance taskInstance)
        {
            // Don't place any code (including debug code) before GetDeferral!!!
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            deferral = taskInstance.GetDeferral();
            Mutex tbm = new Mutex(false, "TaqBackMutex");
            try
            {
                if (tbm.WaitOne(1))
                {
                    try
                    {
                        // tbtLog is used to not only log infos, but also act as an execution token. Multiple Taq.BackTasks might be triggered at the same time, but only one of them has the token to execute the full Run.

                        var tbtLog = await ApplicationData.Current.LocalFolder.CreateFileAsync(taskInstance.Task.Name + "Log.txt", CreationCollisionOption.ReplaceExisting);
                        var s = await tbtLog.OpenStreamForWriteAsync();
                        var sw = new StreamWriter(s);
                        sw.WriteLine("Background task start time: " + DateTime.Now.ToString());

                        TaqModel m = new TaqJsonModel();

                        taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);

                        // We assume that this following codes have a high probability of a successfull run
                        // after a failed run with exceptions. It means the success rate of a run is almost independent of the previous runs. So, we just catch exceptions and do nothing, so that this baskgroundtask won't crash and exit.
                        await m.loadSubscrSiteXml();
                        try
                        {
                            sw.WriteLine("Download start time: " + DateTime.Now.ToString());
                            // Download the feed.
                            var res = await m.downloadAqData();
                        }
                        catch (Exception ex)
                        {
                            sw.WriteLine("Download fail time: " + DateTime.Now.ToString() + "\n" + ex.Message);
                            Debug.WriteLine(ex.Message);
                        }

                        try
                        {
                            sw.WriteLine("loadAq2Dict time: " + DateTime.Now.ToString());
                            await m.loadAq2Dict();
                        }
                        catch (Exception ex)
                        {
                            sw.WriteLine("loadAq2Dict fail time: " + DateTime.Now.ToString() + ex.Message);
                            // Ignore.
                        }

                        sw.WriteLine("Many calls start time: " + DateTime.Now.ToString());
                        try
                        {
                            if ((bool)m.localSettings.Values["AutoPos"] && (bool)m.localSettings.Values["BgMainSiteAutoPos"])
                            {
                                await m.findNearestSite();
                                await m.loadMainSite(m.nearestSite);
                            }
                            else
                            {
                                await m.loadMainSite((string)m.localSettings.Values["MainSite"]);
                            }

                            // Update the live tile with the feed items.
                            await m.updateLiveTile();

                            // Send notifications.
                            m.sendSubscrSitesNotifications();

                            // Tell Taq foreground app that data has been updated.
                            m.localSettings.Values["Taq.BackTaskUpdated"] = true;
                            sw.WriteLine("Many calls end time: " + DateTime.Now.ToString());
                        }
                        catch (Exception ex)
                        {
                            // Do nothing.
                            Debug.WriteLine(ex.Message);
                        }
                        finally
                        {
                            sw.WriteLine("Background task end time: " + DateTime.Now.ToString());
                            sw.Flush();
                            s.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    finally
                    {
                        tbm.ReleaseMutex();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
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
#if LOG_STEP
            var tbtLogC = await ApplicationData.Current.LocalFolder.CreateFileAsync("TbtCancelled.txt", CreationCollisionOption.ReplaceExisting);
            using (var s = await tbtLogC.OpenStreamForWriteAsync())
            {
                var sw = new StreamWriter(s);
                var ct = DateTime.Now;
                sw.WriteLine("Background task cancellation time: " + ct.ToString());
                sw.Flush();
            }
#endif
            //
            // Indicate that the background task is canceled.
            //
            //_cancelRequested = true;
            deferral.Complete();
        }
    }
}
