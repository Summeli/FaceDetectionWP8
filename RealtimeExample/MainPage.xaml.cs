using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Phone;
using Microsoft.Phone.Controls;
using System.Threading;
using System.Xml.Linq;

namespace RealtimeExsample
{
    public partial class MainPage : PhoneApplicationPage
    {


        static bool _faceRecoThreadAlive;              // Specifies whether face recognition thread is alive (I can't just stop the thread...)
        Thread _faceDetectionThread;                   // Thread that performs face detection

        const string MODEL_FILE = "haarcascade_frontalface_alt.xml";
        FaceDetectionWinPhone.Detector _detector;

        int _downsampleFactor = 4;
        private byte[] _pixelDataGray;
        private byte[] _pixelDataDownsampled;
        private int[] _pixelDataGrayInt;
        private WriteableBitmap _wb;

        //private int[] _debugImagePixels;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            _detector = new FaceDetectionWinPhone.Detector(XDocument.Load(MODEL_FILE));
        }


        private DateTime _lastUpdate;
        /// <summary>
        /// When we get a new camera frame, find faces and paint
        /// rectangles on canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="cameraFrameEventArgs"></param>
        private void NewCameraFrame(object sender, CameraFrameEventArgs cameraFrameEventArgs)
        {
            var w = cameraViewer.CameraWidth;
            var h = cameraViewer.CameraHeight;
            
            //create temporary buffers for the frame
            if (_pixelDataGray == null || _pixelDataGray.Length != h * w)
            {
                _pixelDataGray = new byte[w / _downsampleFactor * h / _downsampleFactor];
                _pixelDataDownsampled =
                    new byte[w / _downsampleFactor * h / _downsampleFactor];
                _pixelDataGrayInt = new int[w / _downsampleFactor * h / _downsampleFactor];
                _wb = new WriteableBitmap(w / _downsampleFactor, w / _downsampleFactor);
                //The dbgImg is the image on the upper left corner in the demo, and it's now pointing into _wb
                dbgImg.Source = _wb;
            }

            _lastUpdate = DateTime.Now;

            Utils.DownSample(cameraFrameEventArgs.ARGBData, w, h, ref _pixelDataGrayInt, _downsampleFactor);
            Utils.ARGBToGreyScale(_pixelDataGrayInt, ref _pixelDataGray);
            Utils.HistogramEqualization(ref _pixelDataGray);
            Utils.GrayToARGB(_pixelDataGray, ref _pixelDataGrayInt);

            List<FaceDetectionWinPhone.Rectangle> faces =
                _detector.getFaces(
                _pixelDataGrayInt,
                w / _downsampleFactor,
                h / _downsampleFactor,
                2f, 1.25f, 0.1f, 1, false, false);

            var elapsed = (DateTime.Now - _lastUpdate).TotalMilliseconds;
            cameraResolution.Text = w + " x " + h + " " + elapsed + " ms";

            //copy the grayscale image into the frame shown in the upper left corner
            _pixelDataGrayInt.CopyTo(_wb.Pixels, 0);
            _wb.Invalidate();

            /* Draw a red square around the face.
               The image sent to the facedetection library was downscaled by factor of 4, so in 
               here we're multiplying the x and the y coordinates with 4 to draw the square to 
               correct place on the canvas. */
            Dispatcher.BeginInvoke(delegate()
            {
                cnvsFaceRegions.Children.Clear();
                foreach (var r in faces)
                {
                    Rectangle toAdd = new Rectangle();
                    TranslateTransform loc = new TranslateTransform();
                    loc.X = r.X * _downsampleFactor / (double)w * cnvsFaceRegions.ActualWidth;
                    loc.Y = r.Y * _downsampleFactor / (double)w * cnvsFaceRegions.ActualHeight;
                    toAdd.RenderTransform = loc;
                    toAdd.Width = r.Width * _downsampleFactor;
                    toAdd.Height = r.Height * _downsampleFactor;
                    toAdd.Stroke = new SolidColorBrush(Colors.Red);
                    cnvsFaceRegions.Children.Add(toAdd);
                }
            });
        }

        #region Overrides

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            cameraViewer.SaveToCameraRoll = true;
            cameraViewer.NewCameraFrame += NewCameraFrame;
            cameraViewer.StartPumpingFrames();

            // Finally figured out how to do this
            // instructions athttp://windowsphonegeek.com/articles/encode-and-decode-images-in-wp7
            //Stream stream = App.GetResourceStream(new Uri("Sample;component/Images/testpic.jpg", UriKind.Relative)).Stream;
            //WriteableBitmap bmp = PictureDecoder.DecodeJpeg(stream);
            //_debugImagePixels = bmp.Pixels;
        }



        // When user navigates away from the page, dispose of objects and stop threads
        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            cameraViewer.StopPumpingFrames();
            base.OnNavigatedFrom(e);
        }

        #endregion

    }
}
