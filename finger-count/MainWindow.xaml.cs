using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


namespace finger_count
{
    public partial class MainWindow : Window
    {
        private VideoCapture _capture;
        private DispatcherTimer _timer;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _capture = new VideoCapture(0);
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            using (Mat frame = _capture.QueryFrame())
            {
                if (frame != null)
                {
                    webcamImage.Source = ConvertMatToBitmapImage(frame);
                }
            }
        }

        private BitmapImage ConvertMatToBitmapImage(Mat mat)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Bitmap bitmap = mat.ToBitmap();
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
                memoryStream.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _timer.Stop();
            _capture.Dispose();
        }
    }
}