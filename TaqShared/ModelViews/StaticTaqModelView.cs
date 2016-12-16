﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace TaqShared.ModelViews
{
    public static class StaticTaqModelView
    {
        public async static Task<StorageFile> saveUi2Png(string fileName, UIElement ui)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(ui);
            IBuffer pixelBuffer = await bitmap.GetPixelsAsync();

            var saveFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            // Encode the image to the selected file on disk 
            using (var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi,
                pixelBuffer.ToArray());
                await encoder.FlushAsync();
            }
            return saveFile;
        }

        // Squared Euclidean distance.
        public static double posDist(SiteViewModel p1, SiteViewModel p2)
        {
            return Math.Pow(p1.twd97Lat - p2.twd97Lat, 2) + Math.Pow(p1.twd97Lon - p2.twd97Lon, 2);
        }
    }
}
