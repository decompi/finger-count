using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
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
        private double _zoomFactor = 2.5;

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

        private void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _zoomFactor = e.NewValue;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            using (Mat frame = _capture.QueryFrame())
            {
                if (frame != null)
                {
                    Mat hsvFrame = new Mat();
                    CvInvoke.CvtColor(frame, hsvFrame, ColorConversion.Bgr2Hsv);

                    var lowerSkin = new ScalarArray(new MCvScalar(0, 20, 70));
                    var upperSkin = new ScalarArray(new MCvScalar(20, 255, 255));

                    Mat skinMask = new Mat();
                    CvInvoke.InRange(hsvFrame, lowerSkin, upperSkin, skinMask);

                    if (skinMask == null || skinMask.IsEmpty)
                    {
                        Console.WriteLine("Skin mask is not initialized properly.");
                        return;
                    }

                    Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));

                    if (kernel == null || kernel.IsEmpty)
                    {
                        Console.WriteLine("Kernel is not initialized properly.");
                        return;
                    }

                    CvInvoke.GaussianBlur(skinMask, skinMask, new System.Drawing.Size(5, 5), 0);

                    CvInvoke.Erode(skinMask, skinMask, kernel, new System.Drawing.Point(-1, -1), 2, BorderType.Constant, new MCvScalar(0));
                    CvInvoke.Dilate(skinMask, skinMask, kernel, new System.Drawing.Point(-1, -1), 2, BorderType.Constant, new MCvScalar(0));

                    CvInvoke.MorphologyEx(skinMask, skinMask, MorphOp.Open, kernel, new System.Drawing.Point(-1, -1), 2, BorderType.Constant, new MCvScalar(0));

                    int zoomWidth = (int)(skinMask.Width / _zoomFactor);
                    int zoomHeight = (int)(skinMask.Height / _zoomFactor);
                    int zoomX = (skinMask.Width - zoomWidth) / 2;
                    int zoomY = (skinMask.Height - zoomHeight) / 2;
                    var zoomedRegion = new Rectangle(zoomX, zoomY, zoomWidth, zoomHeight);

                    using (Mat zoomedSkinMask = new Mat(skinMask, zoomedRegion))
                    {
                        Mat resizedSkinMask = new Mat();
                        CvInvoke.Resize(zoomedSkinMask, resizedSkinMask, new System.Drawing.Size(skinMask.Width, skinMask.Height), 0, 0, Inter.Linear);
                        CvInvoke.Imshow("Skin Mask", resizedSkinMask);
                    }

                    using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                    {
                        CvInvoke.FindContours(skinMask, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                        Console.WriteLine($"Contours found: {contours.Size}");

                        for (int i = 0; i < contours.Size; i++)
                        {
                            using (VectorOfPoint contour = contours[i])
                            {
                                double contourArea = CvInvoke.ContourArea(contour);
                                if (contourArea > 5000)
                                {
                                    Console.WriteLine($"Contour {i} area: {contourArea}");

                                    VectorOfPoint approxContour = new VectorOfPoint();
                                    CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.02, true);

                                    VectorOfInt hull = new VectorOfInt();
                                    CvInvoke.ConvexHull(approxContour, hull, false);
                                    VectorOfPoint hullPoints = new VectorOfPoint();
                                    CvInvoke.ConvexHull(approxContour, hullPoints, false);

                                    CvInvoke.Polylines(frame, hullPoints.ToArray(), true, new MCvScalar(0, 0, 255), 2);

                                    CvInvoke.DrawContours(frame, contours, i, new MCvScalar(255, 0, 0), 2);
                                }
                            }
                        }
                    }

                    int frameWidth = (int)(frame.Width / _zoomFactor);
                    int frameHeight = (int)(frame.Height / _zoomFactor);
                    int frameX = (frame.Width - frameWidth) / 2;
                    int frameY = (frame.Height - frameHeight) / 2;
                    var zoomedFrameRegion = new Rectangle(frameX, frameY, frameWidth, frameHeight);

                    using (Mat zoomedFrame = new Mat(frame, zoomedFrameRegion))
                    {
                        Mat resizedFrame = new Mat();
                        CvInvoke.Resize(zoomedFrame, resizedFrame, new System.Drawing.Size(frame.Width, frame.Height), 0, 0, Inter.Linear);
                        webcamImage.Source = ConvertMatToBitmapImage(resizedFrame);
                    }
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