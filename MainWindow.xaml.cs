using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ShutterStream
{
    public sealed partial class MainWindow : Window
    {
        private List<string> imageFiles;
        private int currentIndex = 0;
        private DispatcherTimer timer;

        public MainWindow()
        {
            this.InitializeComponent();
            LoadImages();
            SetupTimer();
        }

        private async void LoadImages()
        {
            string folderPath = @"E:\Slideshows\USA";
            string[] allowedExtensions = { ".jpg", ".png", ".tiff" };

            imageFiles = Directory.GetFiles(folderPath)
                .Where(file => allowedExtensions.Contains(Path.GetExtension(file).ToLower()))
                .ToList();

            if (imageFiles.Any())
            {
                await DisplayImage(imageFiles[currentIndex]);
            }
        }

        private void SetupTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private async void Timer_Tick(object sender, object e)
        {
            currentIndex = (currentIndex + 1) % imageFiles.Count;
            await DisplayImage(imageFiles[currentIndex]);
        }

        private async Task DisplayImage(string imagePath)
        {
            using (IRandomAccessStream stream = await FileRandomAccessStream.OpenAsync(imagePath, FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                SoftwareBitmap resizedBitmap = await ResizeImage(softwareBitmap, 1024, 1024);

                SoftwareBitmap convertedBitmap = SoftwareBitmap.Convert(resizedBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                SoftwareBitmapSource source = new SoftwareBitmapSource();
                await source.SetBitmapAsync(convertedBitmap);

                ImageControl.Source = source;
            }
        }

        private async Task<SoftwareBitmap> ResizeImage(SoftwareBitmap softwareBitmap, uint targetWidth, uint targetHeight)
        {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);

                float scale = Math.Min((float)targetWidth / softwareBitmap.PixelWidth, (float)targetHeight / softwareBitmap.PixelHeight);
                uint scaledWidth = (uint)(softwareBitmap.PixelWidth * scale);
                uint scaledHeight = (uint)(softwareBitmap.PixelHeight * scale);

                encoder.BitmapTransform.ScaledWidth = scaledWidth;
                encoder.BitmapTransform.ScaledHeight = scaledHeight;
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;

                await encoder.FlushAsync();

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                return await decoder.GetSoftwareBitmapAsync();
            }
        }
    }
}