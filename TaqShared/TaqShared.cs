﻿using Microsoft.Toolkit.Uwp.Notifications;
using NotificationsExtensions.TileContent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TaqShared.Models;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Notifications;

namespace TaqShared
{
    public class DownloadException : Exception
    {

    }

    public class Shared
    {
        public static double[] pm2_5_concens = new double[] { 11, 23, 35, 41, 47, 53, 58, 64, 70 };
        public static string[] pm2_5_colors = new string[] { "#9cff9c", "#31ff00", "#31cf00", "#ffff00", "#ffcf00", "#ff9a00", "#ff6464", "#ff0000", "#990000", "#ce30ff" };
        private Windows.Storage.ApplicationDataContainer localSettings;
        public Uri source = new Uri("http://YourTaqServerIp/taq/taq.xml");
        public string dataXmlFile = "taq.xml";
        public string currDataXmlFile = "currData.xml";
        //public XDocument currXd = new XDocument();
        public XDocument xd = new XDocument();
        public XDocument siteGeoXd = new XDocument();
        public Site oldSite = new Site { siteName = "N/A", Pm2_5 = "0" };
        public Site currSite = new Site { siteName = "N/A", Pm2_5 = "0" };

        public Shared()
        {
            localSettings =
       Windows.Storage.ApplicationData.Current.LocalSettings;
        }

        public async Task<int> downloadDataXml()
        {
            //try
            //{
            var dstFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(dataXmlFile, CreationCollisionOption.ReplaceExisting);

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(source, dstFile);

            var task = Task.Run(async () => await download.StartAsync().AsTask());
            if (task.Wait(TimeSpan.FromSeconds(2)))
            {
                // file is downloaded in time
            }
            else
            {
                // timeout is reached - how to cancel downloadOperation ?????
                download.AttachAsync().Cancel();
                throw new DownloadException();
            }


            // Forcly close stream!?
            using (var s = await dstFile.OpenStreamForWriteAsync())
            {

            }

            await reloadXd();
            //}
            /*
            catch (Exception ex)
            {
                //LogException("Download Error", ex);
            }
            */
            return 0;
        }

        public async Task<int> loadSiteGeoXd()
        {
            /*
            var dataXml = await ApplicationData.Current.LocalFolder.GetFileAsync("Assets/SiteGeo.xml");
            using (var s = await dataXml.OpenStreamForReadAsync())
            {
                // Reload to xd.
                siteGeoXd = XDocument.Load(s);
            }*/
            //http://opendata.epa.gov.tw/ws/Data/AQXSite/?format=xml
            siteGeoXd = XDocument.Load("Assets/SiteGeo.xml");

            return 0;
        }

        // Reload air quality XML files.
        public async Task<int> reloadXd()
        {
            var dataXml = await ApplicationData.Current.LocalFolder.GetFileAsync(dataXmlFile);
            using (var s = await dataXml.OpenStreamForReadAsync())
            {
                // Reload to xd.
                xd = XDocument.Load(s);
            }

            return 0;
        }

        public async Task<int> loadCurrSite()
        {
            try
            {
                // Save old site.
                oldSite = currSite;
                
                // Get new site from the setting.
                var newSiteName = (string)localSettings.Values["subscrSite"];
                var newSite = from d in xd.Descendants("Data")
                                where d.Descendants("SiteName").First().Value == newSiteName
                              select d;
                currSite = new Site { siteName = newSite.Descendants("SiteName").First().Value, Pm2_5 = newSite.Descendants("PM2.5").First().Value };
            }
            catch (Exception ex)
            {

            }
            return 0;
        }

        public void updateLiveTile()
        {
            // create the instance of Tile Updater, which enables you to change the appearance of the calling app's tile
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            // enables the tile to queue up to five notifications 
            updater.EnableNotificationQueue(true);
            updater.Clear();

            // get the XML content of one of the predefined tile templates, so that, you can customize it
            // create the wide template
            ITileWide310x150PeekImageAndText01 wideContent = TileContentFactory.CreateTileWide310x150PeekImageAndText01();
            wideContent.TextBodyWrap.Text = "觀測站：" + currSite.siteName + "\nPM 2.5：" + currSite.Pm2_5 + "\n";
            wideContent.Image.Src = "ms-appx:///Assets/Wide310x150Logo.scale-200.png";

            // create the square template and attach it to the wide template 
            ITileSquare150x150Block squareContent = TileContentFactory.CreateTileSquare150x150Block();
            squareContent.TextBlock.Text = currSite.Pm2_5.ToString();
            squareContent.TextSubBlock.Text = "PM 2.5\n觀測站：" + currSite.siteName;
            wideContent.Square150x150Content = squareContent;

            // Create a new tile notification.
            updater.Update(new TileNotification(wideContent.GetXml()));
        }

        public void sendNotify()
        {
            int pm2_5_WarnIdx = (int) localSettings.Values["Pm2_5_ConcensIdx"];
            //var currMin = DateTime.Now.Minute;
            if (!(oldSite.Pm2_5 != currSite.Pm2_5 && pm2_5ConcensToIdx(int.Parse(currSite.Pm2_5)) > pm2_5_WarnIdx))
            {
                return;
            }
            var title = "PM 2.5濃度: " + currSite.Pm2_5;
            var content = "觀測站: " + currSite.siteName;
            // Now we can construct the final toast content
            ToastContent toastContent = new ToastContent()
            {
                Visual = getNotifyVisual(title, content)
            };

            // And create the toast notification
            var toast = new ToastNotification(toastContent.GetXml());
            toast.ExpirationTime = DateTime.Now.AddHours(1);
            toast.Tag = "1";
            toast.Group = "wallPosts";
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        string logo = "Assets/StoreLogo.png";
        private ToastVisual getNotifyVisual(string title, string content)
        {
            // Construct the visuals of the toast
            return new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = title
                        },

                        new AdaptiveText()
                        {
                            Text = content
                        }
                    },
                    AppLogoOverride = new ToastGenericAppLogo()
                    {
                        Source = logo,
                        HintCrop = ToastGenericAppLogoCrop.Circle
                    }
                }
            };
        }

        public int pm2_5ConcensToIdx(int concens)
        {
            var i = 0;
            for (; i < pm2_5_concens.Length; i++)
            {
                if (concens <= pm2_5_concens[i])
                {
                    break;
                }
            }
            return i + 1;
        }
    }
}
