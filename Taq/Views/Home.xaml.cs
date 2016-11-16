﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

using TaqShared;
using TaqShared.Models;
using System.Xml.Linq;
using Windows.Storage;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Background;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.Devices.Geolocation;

namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Home : Page
    {
        public App app;
        // For updating UI after TaqBackTask downloads a new XML.
        ThreadPoolTimer periodicTimer;

        public Home()
        {
            this.InitializeComponent();
            app = App.Current as App;
            initAqData();
            initPeriodicTimer();
            this.DataContext = this;
        }

        public async void initAqData()
        {
            try
            {
                await app.shared.downloadDataXml();
                await updateListView();
                app.shared.updateLiveTile();
            }
            catch (DownloadException ex)
            {
                statusTextBlock.Text = "資料庫下載失敗。請檢查網路，再嘗試手動更新。";
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "初始化失敗。請檢查網路，再嘗試手動更新。";
            }
        }

        private void initPeriodicTimer()
        {

#if DEBUG
            TimeSpan delay = TimeSpan.FromSeconds(3e3);
#else
            TimeSpan delay = TimeSpan.FromSeconds(60);
#endif
            periodicTimer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
            {
                // TODO: Work

                // Update the UI thread by using the UI core dispatcher.
                Dispatcher.RunAsync(CoreDispatcherPriority.High,
                    () =>
                    {
                        try
                        {
                            updateListView();
                        }
                        catch (Exception ex)
                        {
                            statusTextBlock.Text = "自動更新失敗。請嘗試手動更新。";
                        }
                    }
                );

            }, delay);
        }
        public async Task<int> updateListView()
        {
            await app.shared.reloadDataX();
            await app.shared.loadCurrSite();
            return 0;
        }

        public async Task<int> refreshSites()
        {
            try
            {
                statusTextBlock.Text = "Download start.";
                await app.shared.downloadDataXml();
                statusTextBlock.Text = "Download finish.";
                await updateListView();
                app.shared.updateLiveTile();
                app.shared.sendNotify();
            }
            catch (DownloadException ex)
            {
                statusTextBlock.Text = "資料庫下載失敗。請檢查網路，再嘗試手動更新。";
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "手動更新失敗。";
            }
            return 0;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.RegisterBackgroundTask();
        }

        private const string taskName = "TaqBackTask";
        private const string taskEntryPoint = "TaqBackTask.TaqBackTask";
        private async void RegisterBackgroundTask()
        {
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == taskName)
                    {
                        task.Value.Unregister(true);
                    }
                }

                BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                taskBuilder.Name = taskName;
                taskBuilder.TaskEntryPoint = taskEntryPoint;
                taskBuilder.SetTrigger(new TimeTrigger(15, false));
                var registration = taskBuilder.Register();
            }
        }

        private async void button_Click(Object sender, RoutedEventArgs e)
        {
            await refreshSites();
        }

        private async void Page_Loaded(Object sender, RoutedEventArgs e)
        {
            await updateListView();
        }
    }
}
