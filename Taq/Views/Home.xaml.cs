﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TaqShared;
using TaqShared.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    // For updating UI after TaqBackTask downloads a new XML.

    public sealed partial class Home : Page
    {
        public App app;
        ThreadPoolTimer periodicTimer;

        public Home()
        {
            app = App.Current as App;
            this.InitializeComponent();
            downloadAndReload();
            initPeriodicTimer();
        }

        public async Task<int> downloadAndReload()
        {
            try
            {
                statusTextBlock.Text = "Download start.";
                await app.shared.downloadDataXml();
                statusTextBlock.Text = "Download finish.";
            }
            catch (DownloadException ex)
            {
                statusTextBlock.Text = "資料庫下載失敗。請檢查網路，再嘗試手動更新。";
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "錯誤，請嘗試手動更新。";
            }

            try
            {
                await app.shared.reloadXd();
            }
            catch (Exception ex)
            {
                // Ignore.
            }

            await updateListView();
            app.shared.updateLiveTile();
            return 0;
        }

        private void initPeriodicTimer()
        {

#if DEBUG
            TimeSpan delay = TimeSpan.FromSeconds(3e3);
#else
            TimeSpan delay = TimeSpan.FromSeconds(60);
#endif
            periodicTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
            {
                // TODO: Work

                // Update the UI thread by using the UI core dispatcher.
                await Dispatcher.RunAsync(CoreDispatcherPriority.High,
                        async () =>
                        {
                            await ReloadXdAndUpdateList();
                        }
                    );

            }, delay);
        }

        public async Task<int> updateListView()
        {
            try
            {
                await app.shared.reloadDataX();
                await app.shared.loadCurrSite();
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "列表更新失敗，請重試手動更新。";
            }
            return 0;
        }

        private async Task<int> ReloadXdAndUpdateList()
        {
            try
            {
                await app.shared.reloadXd();
                await updateListView();
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "自動更新失敗。請嘗試手動更新。";
            }
            return 0;
        }

        private async void button_Click(Object sender, RoutedEventArgs e)
        {
            await downloadAndReload();
            app.shared.sendNotify();
        }

        private async void Page_Loaded(Object sender, RoutedEventArgs e)
        {
            await ReloadXdAndUpdateList();
        }
    }
}
