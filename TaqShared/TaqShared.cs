﻿using Microsoft.Toolkit.Uwp.Notifications;
using NotificationsExtensions.TileContent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TaqShared.Models;
using Windows.Devices.Geolocation;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Notifications;

namespace TaqShared
{
    public class DownloadException : Exception
    {

    }
    public class OldXmlException : Exception
    {

    }

    public class Shared : INotifyPropertyChanged
    {
        public static double[] pm2_5_concens = new double[] { 11, 23, 35, 41, 47, 53, 58, 64, 70 };
        public static string[] pm2_5_colors = new string[] { "#9cff9c", "#31ff00", "#31cf00", "#ffff00", "#ffcf00", "#ff9a00", "#ff6464", "#ff0000", "#990000", "#ce30ff" };

        public static List<int> aqi_limits = new List<int> { 50, 100, 150, 200, 300, 400, 500 };
        public static List<string> aqiBgColors = new List<string> { "#00ff00", "#ffff00", "#ff7e00", "#ff0000", "#800080", "#633300", "#633300" };

        private Windows.Storage.ApplicationDataContainer localSettings;
        public const string dataXmlFile = "taqi.xml";
        public Uri source = new Uri("http://YourTaqServerIp/taq/" + dataXmlFile);
        public string currDataXmlFile = "currData.xml";
        public XDocument xd = new XDocument();
        public XDocument siteGeoXd = new XDocument();
        public ObservableCollection<Site> sites = new ObservableCollection<Site>();
        public Site currSite = new Site { siteName = "N/A", Pm2_5 = "0" };
        public ObservableCollection<string[]> currSiteViews = new ObservableCollection<string[]>();
        public Dictionary<string, string> currSiteDict;

        public Site oldSite = new Site { siteName = "N/A", Pm2_5 = "0" };
        public Dictionary<string, string> oldSiteDict;

        // The order of keys is meaningful.
        // The display order of AQ items in Home.xaml follows this order of keys.
        public Dictionary<string, string> fieldNames = new Dictionary<string, string>
        {
            { "PublishTime", "發佈時間"},
            { "SiteName", "觀測站" },
            { "County", "縣市"},
            { "AQI", "空氣品質指標"},
            { "Status", "狀態"},
            { "Pollutant", "污染指標物"},
            { "PM2.5", "PM 2.5"},
            { "PM2.5_AVG", "PM2.5_AVG"},
            { "PM10", "PM 10"},
            { "PM10_AVG", "PM10_AVG"},
            { "O3", "O3"},
            { "O3_8hr", "O3_8hr"},
            { "CO", "CO"},
            { "CO_8hr", "CO_8hr"},
            { "SO2", "SO2"},
            { "NO2", "NO2"},
            { "NOx", "NOx"},
            { "NO", "NO"},
            { "WindSpeed", "風速"},
            { "WindDirec", "風向"},
        };

        public Shared()
        {
            localSettings =
       Windows.Storage.ApplicationData.Current.LocalSettings;
        }

        public async Task<int> downloadDataXml()
        {
            // Download may fail, so we create a temp StorageFile.
            var dlFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("Temp" + dataXmlFile, CreationCollisionOption.ReplaceExisting);

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(source, dlFile);

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            cts.CancelAfter(5000);
            try
            {
                // Pass the token to the task that listens for cancellation.
                await download.StartAsync().AsTask(token);
                // file is downloaded in time
                // Copy download file to dataXmlFile.
                var dataXml = await ApplicationData.Current.LocalFolder.CreateFileAsync(dataXmlFile, CreationCollisionOption.ReplaceExisting);
                await dlFile.CopyAndReplaceAsync(dataXml);
            }
            catch (Exception ex)
            {
                // timeout is reached, downloadOperation is cancled
                throw new DownloadException();
            }
            finally
            {
                // Releases all resources of cts
                cts.Dispose();
            }

            return 0;
        }

        public int loadSiteGeoXd()
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

            if (dataXml.IsAvailable)
            {
                try
                {
                    using (var s = await dataXml.OpenStreamForReadAsync())
                    {
                        // Reload to xd.
                        xd = XDocument.Load(s);
                    }
                }
                catch (Exception ex)
                {
                    xd = XDocument.Load("Assets/" + dataXmlFile);
                    throw new OldXmlException();
                }
            }
            else
            {
                xd = XDocument.Load("Assets/" + dataXmlFile);
                throw new OldXmlException();
            }

            return 0;
        }

        public async Task<int> reloadDataX()
        {
            var dataX = from data in xd.Descendants("Data")
                        select data;
            var geoDataX = from data in siteGeoXd.Descendants("Data")
                           select data;

            // Removing all items before updating, because the new download data XML file
            // could have a different number of Data elements from the old one.
            sites.Clear();

            foreach (var d in dataX.OrderBy(x => x.Element("County").Value))
            {
                var siteName = d.Descendants("SiteName").First().Value;
                var geoD = from gd in geoDataX
                           where gd.Descendants("SiteName").First().Value == siteName
                           select gd;
                sites.Add(new Site
                {
                    siteName = siteName,
                    County = d.Descendants("County").First().Value,
                    Aqi = d.Descendants("AQI").First().Value,
                    Pm2_5 = d.Descendants("PM2.5").First().Value,
                    twd97Lat = double.Parse(geoD.Descendants("TWD97Lat").First().Value),
                    twd97Lon = double.Parse(geoD.Descendants("TWD97Lon").First().Value),
                });
            }

            reloadSubscrSiteId();
            return 0;
        }

        public async Task<int> loadCurrSite()
        {
            try
            {
                // Load the old site.
                XDocument loadOldXd = new XDocument();
                var loadOldXml = await ApplicationData.Current.LocalFolder.GetFileAsync("OldSite.xml");
                using (var s = await loadOldXml.OpenStreamForReadAsync())
                {
                    loadOldXd = XDocument.Load(s);
                }
                var oldSiteX = loadOldXd.Descendants("Data").First();
                oldSiteDict = oldSiteX.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);
                oldSite = new Site
                {
                    siteName = oldSiteX.Descendants("SiteName").First().Value,
                    Aqi = oldSiteX.Descendants("AQI").First().Value,
                    Pm2_5 = oldSiteX.Descendants("PM2.5").First().Value,
                };
            }
            catch (Exception ex)
            {
                // Ignore.
            }

            try
            {
                // Get new site from the setting.
                var newSiteName = (string)localSettings.Values["subscrSite"];
                var currSiteX = from d in xd.Descendants("Data")
                                where d.Descendants("SiteName").First().Value == newSiteName
                                select d;

                currSiteDict = currSiteX.Elements().ToDictionary(x => x.Name.LocalName, x => x.Value);
                currSite = new Site
                {
                    siteName = currSiteDict["SiteName"],
                    Aqi = currSiteDict["AQI"],
                    Pm2_5 = currSiteDict["PM2.5"]
                };

                // Save the current site as old site.
                XDocument saveOldXd = new XDocument();
                saveOldXd.Add(currSiteX);
                var saveOldXml = await ApplicationData.Current.LocalFolder.CreateFileAsync("OldSite.xml", CreationCollisionOption.ReplaceExisting);
                using (var s = await saveOldXml.OpenStreamForWriteAsync())
                {
                    saveOldXd.Save(s);
                }

                Site2Coll();
            }
            catch (Exception ex)
            {

            }
            return 0;
        }

        public void Site2Coll()
        {
            // Don't remove all elements by new.
            // Otherwise, data bindings would be problematic.
            currSiteViews.Clear();
            foreach (var k in fieldNames.Keys)
            {
                currSiteViews.Add(new string[]
                {
                    "#31cf00", // default border background color
                    fieldNames[k] + "\n" + currSiteDict[k],
                    "Black", // default text color
                });
            }
            var aqiLevel = aqi_limits.FindIndex(x => currSite.aqi_int <= x);
            currSiteViews[3][0] = aqiBgColors[aqiLevel];
            currSiteViews[3][2] = (aqiLevel <= 3) ? "Black" : "White";
        }

        private int subscrSiteId;
        public int SubscrSiteId
        {
            get
            {
                return subscrSiteId;
            }
            set
            {
                subscrSiteId = value;
                NotifyPropertyChanged();
            }
        }

        public void reloadSubscrSiteId()
        {
            var subscrSiteName = (string)localSettings.Values["subscrSite"];
            var subscrSiteElem = from s in sites
                                 where s.siteName == subscrSiteName
                                 select s;
            SubscrSiteId = sites.IndexOf(subscrSiteElem.First());
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
            var timeStr = currSiteDict["PublishTime"].Substring(11, 5);
            var wideContent = TileContentFactory.CreateTileWide310x150BlockAndText01();
            wideContent.TextBlock.Text = currSiteDict["AQI"];
            wideContent.TextSubBlock.Text = "空氣品質";
            wideContent.TextBody1.Text = "觀測站：" + currSite.siteName;
            wideContent.TextBody2.Text = "發佈時間：" + timeStr;
            wideContent.TextBody3.Text = fieldNames["Status"] + "：" + currSiteDict["Status"];
            wideContent.TextBody4.Text = "PM 2.5：" + currSite.Pm2_5;
            //wideContent.Image.Src = "ms-appx:///Assets/Wide310x150Logo.scale-200.png";

            // create the square template and attach it to the wide template 
            ITileSquare150x150Block squareContent = TileContentFactory.CreateTileSquare150x150Block();
            squareContent.TextBlock.Text = currSite.Pm2_5.ToString();
            squareContent.TextSubBlock.Text = "PM 2.5\n" + currSite.siteName + "/" + timeStr;
            wideContent.Square150x150Content = squareContent;

            // Create a new tile notification.
            updater.Update(new TileNotification(wideContent.GetXml()));
        }

        public void sendNotifications()
        {
            int aqi_LimitId = (int)localSettings.Values["Aqi_LimitId"];
            if (oldSiteDict["AQI"] != currSiteDict["AQI"] && aqi_limits.FindLastIndex(x => currSite.aqi_int <= x) > aqi_LimitId)
            {
                sendNotification("AQI: " + currSiteDict["AQI"], "AQI");
            }

            int pm2_5_LimitId = (int)localSettings.Values["Pm2_5_LimitId"];
            if (oldSiteDict["PM2.5"] != currSiteDict["PM2.5"] && pm2_5ConcensToId(currSite.pm2_5_int) > pm2_5_LimitId)
            {
                sendNotification("PM 2.5濃度: " + currSiteDict["PM2.5"], "PM2.5");
            }

#if DEBUG
            sendNotification("AQI: " + currSiteDict["AQI"], "AQI");
            sendNotification("PM 2.5濃度: " + currSiteDict["PM2.5"], "PM2.5");
#endif
        }

        public void sendNotification(string title, string tag)
        {
            var content = "觀測站: " + currSite.siteName;
            // Now we can construct the final toast content
            ToastContent toastContent = new ToastContent()
            {
                Visual = getNotifyVisual(title, content)
            };

            // And create the toast notification
            var toast = new ToastNotification(toastContent.GetXml());
            toast.ExpirationTime = DateTime.Now.AddHours(1);
            toast.Tag = tag;
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

        public int pm2_5ConcensToId(int concens)
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
