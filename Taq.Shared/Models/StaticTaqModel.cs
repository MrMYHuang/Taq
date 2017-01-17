﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace Taq.Shared.Models
{
    public static class StaticTaqModel
    {
        // XML AQ names to ordinary AQ names.
        // The order of keys is meaningful!
        // The display order of AQ items in Home.xaml follows this order of keys.
        public static Dictionary<string, string> fieldNames = new Dictionary<string, string>
        {
            { "SiteName", "觀測站" },
            { "County", "縣市"},

            { "PublishTime", "發佈時間"},
            { "Pollutant", "污染指標物"},

            { "Status", "狀態"},
            { "AQI", "空氣品質指標"},

            { "PM2.5", "即時PM2.5"},
            { "PM2.5_AVG", "平均PM2.5"},

            { "PM10", "即時PM10"},
            { "PM10_AVG", "平均PM10"},

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

        public static Dictionary<string, string> shortStatusDict = new Dictionary<string, string>
        {
            { "對敏感族群不良", "敏感"},
            { "對所有族群不良", "所有"},
            { "非常不良", "非常"},
        };

        public static string getShortStatus(string statusStr)
        {
            if (statusStr.Length > 2)
            {
                return StaticTaqModel.shortStatusDict[statusStr];
            }
            return statusStr;
        }

        // Limit of color of texts on the corresponding AQ background color.
        // <= aqTextColorLimit: black
        // > aqTextColorLimit: white
        public static int aqTextColorLimit = 2;

        // AQ level limits and corresponding colors lists.
        // Notice: a color list has one more element than a limit list!
        public static List<double> defaultLimits = new List<double> { 0 };
        public static List<string> defaultColors = new List<string> { "#a6ce39", "#a6ce39" };

        public static List<string> dirtyColors = new List<string> { "#C0C0C0", "#C0C0C0" };

        public static List<double> aqiLimits = new List<double> { 50, 100, 150, 200, 300, 400, 500 };
        // From https://www3.epa.gov/airnow/aqi_brochure_02_14.pdf
        public static List<string> aqiBgColors = new List<string> { "#a6ce39", "#fff200", "#f7901e", "#ed1d24", "#a2064a", "#891a1c", "#891a1c", "#891a1c" };

        public static List<double> pm2_5Limits = new List<double> { 15.4, 35.4, 54.4, 150.4, 250.4, 350.4, 500.4 };
        public static List<double> pm10Limits = new List<double> { 54, 125, 254, 354, 424, 504, 604 };
        public static List<double> o3Limits = new List<double> { 60, 125, 164, 204, 404, 504, 604 };
        // 201, 202, ...
        public static List<double> o3_8hrLimits = new List<double> { 54, 70, 85, 105, 200, 201, 202 };
        public static List<double> coLimits = new List<double> { 4.4, 9.4, 12.4, 15.4, 30.4, 40.4, 50.4 };
        public static List<double> so2Limits = new List<double> { 35, 75, 185, 304, 604, 804, 1004 };
        public static List<double> no2Limits = new List<double> { 53, 100, 360, 649, 1249, 1649, 2049 };

        public static int getAqLevel(string aqName, double aqVal)
        {
            var aqLevel = aqLimits[aqName].FindIndex(x => aqVal <= x);
            if (aqLevel == -1)
            {
                aqLevel = aqLimits[aqName].Count;
            }
            return aqLevel;
        }

        // Combine color lists into Dictinoary.
        public static Dictionary<string, List<string>> aqColors = new Dictionary<string, List<string>>
        {
            { "PublishTime", defaultColors},
            { "SiteName", defaultColors },
            { "County", defaultColors},
            { "Pollutant", dirtyColors},
            { "AQI", aqiBgColors},
            { "Status", aqiBgColors},
            { "ShortStatus", aqiBgColors},
            { "PM2.5", aqiBgColors},
            { "PM2.5_AVG", aqiBgColors},
            { "PM10", aqiBgColors},
            { "PM10_AVG", aqiBgColors},
            { "O3", aqiBgColors},
            { "O3_8hr", aqiBgColors},
            { "CO", aqiBgColors},
            { "CO_8hr", aqiBgColors},
            { "SO2", aqiBgColors},
            { "NO2", aqiBgColors},
            { "NOx", dirtyColors},
            { "NO", dirtyColors},
            { "WindSpeed", defaultColors},
            { "WindDirec", defaultColors},
        };

        // Combine limit lists into Dictinoary.
        public static Dictionary<string, List<double>> aqLimits = new Dictionary<string, List<double>>
        {
            { "PublishTime", defaultLimits},
            { "SiteName", defaultLimits },
            { "County", defaultLimits},
            { "Pollutant", defaultLimits},
            { "AQI", aqiLimits},
            { "Status", aqiLimits},
            { "ShortStatus", aqiLimits},
            { "PM2.5", pm2_5Limits},
            { "PM2.5_AVG", pm2_5Limits},
            { "PM10", pm10Limits},
            { "PM10_AVG", pm10Limits},
            { "O3", o3Limits},
            { "O3_8hr", o3_8hrLimits},
            { "CO", coLimits},
            { "CO_8hr", coLimits},
            { "SO2", so2Limits},
            { "NO2", no2Limits},
            { "NOx", defaultLimits},
            { "NO", defaultLimits},
            { "WindSpeed", defaultLimits},
            { "WindDirec", defaultLimits},
        };

        // Squared Euclidean distance.
        public static double posDist(GpsPoint p1, GpsPoint p2)
        {
            return Math.Pow(p1.twd97Lat - p2.twd97Lat, 2) + Math.Pow(p1.twd97Lon - p2.twd97Lon, 2);
        }

        public static async Task<int> downloadAndBackup(Uri source, string dstFile, int timeout = 10000)
        {
            // Download may fail, so we create a temp StorageFile.
            var dlFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("Temp" + dstFile, CreationCollisionOption.ReplaceExisting);

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(source, dlFile);

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            cts.CancelAfter(timeout);
            try
            {
                // Pass the token to the task that listens for cancellation.
                await download.StartAsync().AsTask(token);
            }
            catch (Exception ex)
            {
                // timeout is reached, downloadOperation is cancled
                throw new Exception("錯誤，下載逾時！");
            }
            finally
            {
                // Releases all resources of cts
                cts.Dispose();
            }

            // file is downloaded in time
            StorageFile dstSf;
            try
            {
                dstSf = await ApplicationData.Current.LocalFolder.GetFileAsync(dstFile);
                // Backup old file.
                var oldAqDbSf = await ApplicationData.Current.LocalFolder.CreateFileAsync("Old" + dstFile, CreationCollisionOption.ReplaceExisting);
                await dstSf.CopyAndReplaceAsync(oldAqDbSf);
            }
            catch (Exception ex)
            {
                dstSf = await ApplicationData.Current.LocalFolder.CreateFileAsync(dstFile, CreationCollisionOption.ReplaceExisting);
            }
            // Copy download file to dstFile.
            await dlFile.CopyAndReplaceAsync(dstSf);
            return 0;
        }
        
        // Return true if the file modified date is older for time renewTime.
        public static async Task<bool> checkFileOutOfDate(string file, TimeSpan renewTime)
        {
            try
            {
                var fsf = await ApplicationData.Current.LocalFolder.GetFileAsync(file);
                var fbp = await fsf.GetBasicPropertiesAsync();
                var fdm = fbp.DateModified;
                var now = DateTimeOffset.Now;
                return ((now - fdm) > renewTime);
            }
            // If file not exist.
            catch (Exception ex)
            {
                return true;
            }
        }
    }
}