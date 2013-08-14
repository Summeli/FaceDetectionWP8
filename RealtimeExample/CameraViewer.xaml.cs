using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace RealtimeExsample
{
    public class CameraFrameEventArgs : RoutedEventArgs
    {
        public int[] ARGBData { get; set; }
    }

    /// <summary>
    /// Shows the camera view and raises events when new camera frames are ready
    /// Uses code from sample at  http://msdn.microsoft.com/en-us/library/hh202982(v=vs.92).aspx
    /// </summary>
    public partial class CameraViewer : UserControl
    {
        #region Events

        public EventHandler<CameraFrameEventArgs> NewCameraFrame { get; set; }

        public EventHandler<ContentReadyEventArgs> NewCameraCaptureImage { get; set; }

        public EventHandler<CameraOperationCompletedEventArgs> CamInitialized { get; set; }

        #endregion
       
        #region Properties

        public int CameraWidth
        {
            get { return _cameraWidth; }
            set { _cameraWidth = value; }
        }

        public int CameraHeight
        {
            get { return _cameraHeight; }
            set { _cameraHeight = value; }
        }

        public bool PhotoOnPress
        {
            get { return _photoOnPress; }
            set
            {
                _photoOnPress = value;
                CameraButtons.ShutterKeyPressed -= CameraButtonsOnShutterKeyPressed;
                if (_photoOnPress)
                {
                    CameraButtons.ShutterKeyPressed += CameraButtonsOnShutterKeyPressed;
                }
            }
        }

        public bool SaveToCameraRoll { get; set; }

        public PhotoCamera Camera
        {
            get { return _camera; }
        }

        public bool TakingPhoto
        {
            get { return _takingPhoto; }
        }

        #endregion

        private PhotoCamera _camera; // the windows phone camera that takes the photos
        private int _cameraWidth = -1;
        private int _cameraHeight = -1;
        private Thread _pumpFramesThread;
        private static ManualResetEvent _pauseFramesEvent = new ManualResetEvent(true);
        private bool _takingPhoto;
        
        private bool _photoOnPress;
        private SoundEffect _cameraShutterSound;
        private static ManualResetEvent _cameraCaptureEvent = new ManualResetEvent(true);
        private static ManualResetEvent _cameraInitializedEvent = new ManualResetEvent(false);
        private bool _pumpFrames;

        public CameraViewer()
        {
            InitializeComponent();
            Unloaded += new RoutedEventHandler(OnUnloaded);
            PhotoOnPress = true;
        }


        #region Public methods
        public void StartPumpingFrames()
        {
            _pauseFramesEvent = new ManualResetEvent(true);
            _cameraCaptureEvent = new ManualResetEvent(true);
            _cameraInitializedEvent = new ManualResetEvent(false);

            InitializeCamera();
            //if (_pumpFramesThread != null)
            //{
            //    if (_pumpFramesThread.IsAlive)
            //        _pumpFramesThread.Abort();
            //    _pumpFramesThread = null;
            //}

            _pumpFrames = true;
            if(_pumpFramesThread == null)
            {
                _pumpFramesThread = new Thread(PumpFrames);
            }
            if(!_pumpFramesThread.IsAlive)
                _pumpFramesThread.Start();
        }

        public void StopPumpingFrames()
        {
            _pumpFrames = false;
            _pumpFramesThread = null;
        }

        public void TakePhoto()
        {
            if (TakingPhoto)
                return;
            _cameraCaptureEvent.Reset();
            FrameworkDispatcher.Update();
            _cameraShutterSound.Play();
            _takingPhoto = true;
            Camera.CaptureImage();

        }

        #endregion

        private void CameraButtonsOnShutterKeyPressed(object sender, EventArgs eventArgs)
        {

            if(_photoOnPress && !TakingPhoto)
            {
                _cameraCaptureEvent.WaitOne();
                TakePhoto();
            }


        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _cameraInitializedEvent.Reset();
            CameraButtons.ShutterKeyPressed -= CameraButtonsOnShutterKeyPressed;
        }

        public void InitializeCamera()
        {
            _cameraInitializedEvent.Reset();

            // Check to see if the camera is available on the device.
            if ((PhotoCamera.IsCameraTypeSupported(CameraType.Primary) == true) ||
                (PhotoCamera.IsCameraTypeSupported(CameraType.FrontFacing) == true))
            {
                // Initialize the default camera.
                _camera = new Microsoft.Devices.PhotoCamera();

                //Event is fired when the PhotoCamera object has been initialized
                Camera.Initialized +=
                    new EventHandler<Microsoft.Devices.CameraOperationCompletedEventArgs>(CameraInitialized);
                Camera.CaptureImageAvailable += CameraOnCaptureImageAvailable;
                Camera.CaptureCompleted += CameraOnCaptureCompleted;

                //Set the VideoBrush source to the camera
                viewfinderBrush = new VideoBrush();
                viewfinderBrush.SetSource(Camera);
                videoRectangle.Fill = viewfinderBrush;



                // initialize the shutter sound
                // Audio
                Stream stream = TitleContainer.OpenStream("shutter.wav");
                _cameraShutterSound = SoundEffect.FromStream(stream);

                CameraButtons.ShutterKeyPressed -= CameraButtonsOnShutterKeyPressed;
                if (_photoOnPress)
                {
                    CameraButtons.ShutterKeyPressed += CameraButtonsOnShutterKeyPressed;
                }
            }
            else
            {
                // The camera is not supported on the device.
                MessageBox.Show(
                    "Sorry, this sample requires a phone camera and no camera is detected. This application will not show any camera output.");
            }
        }

        private void CameraOnCaptureCompleted(object sender, CameraOperationCompletedEventArgs cameraOperationCompletedEventArgs)
        {
            _cameraCaptureEvent.Set();
            _takingPhoto = false;
        }

        private void CameraOnCaptureImageAvailable(object sender, ContentReadyEventArgs contentReadyEventArgs)
        {
            if(SaveToCameraRoll)
            {
                Dispatcher.BeginInvoke(() =>
                                           {
                                               WriteableBitmap bitmap =
                                                   CreateWriteableBitmap(contentReadyEventArgs.ImageStream,
                                                                         (int) Camera.Resolution.Width,
                                                                         (int) Camera.Resolution.Height);
                                               SaveCapturedImage(bitmap);
                                           }
                    );
            }
            
            if(NewCameraCaptureImage != null)
                NewCameraCaptureImage.Invoke(this, contentReadyEventArgs);
        }

        // Helper for CameraOnCaptureImageAvailable
        private void SaveCapturedImage(WriteableBitmap imageToSave)
        {
            var stream = new MemoryStream();
            imageToSave.SaveJpeg(stream, imageToSave.PixelWidth, imageToSave.PixelHeight, 0, 100);

            //Take the stream back to its beginning because it will be read again 
            //when saving to the library
            stream.Position = 0;

            var library = new MediaLibrary();
            string fileName = string.Format("{0:yyyy-MM-dd-HH-mm-ss}.jpg", DateTime.Now);
            library.SavePictureToCameraRoll(fileName, stream);
        }


        // Creates a WriteableBitmap from an imageStream
        private WriteableBitmap CreateWriteableBitmap(Stream imageStream, int width, int height)
        {
            var bitmap = new WriteableBitmap(width, height);

            imageStream.Position = 0;
            bitmap.LoadJpeg(imageStream);
            return bitmap;
        }

        private void CameraInitialized(object sender, CameraOperationCompletedEventArgs e)
        {
            if (e.Succeeded)
            {
                try
                {
                    // available resolutions are ordered based on number of pixels in each resolution
                    CameraWidth = (int)Camera.PreviewResolution.Width;
                    CameraHeight = (int)Camera.PreviewResolution.Height;


                    if (CamInitialized != null)
                        CamInitialized.Invoke(this, e);

                    _cameraInitializedEvent.Set();
                    _pauseFramesEvent.Set();
                }
                catch (ObjectDisposedException )
                {
                    // If the camera was disposed, try initializing again

                }

            }
        }



        private void PumpFrames()
        {
            _cameraInitializedEvent.WaitOne();
            int[] pixels = new int[CameraWidth * CameraHeight];

            int numExceptions = 0;
            while(_pumpFrames)
            {
                _pauseFramesEvent.WaitOne();
                _cameraCaptureEvent.WaitOne();
                _cameraInitializedEvent.WaitOne();

                try
                {
                    
                    Camera.GetPreviewBufferArgb32(pixels);
                    
                } catch(Exception e)
                {
                    // If we get an exception try capturing again, do this up to 10 times
                    if (numExceptions >= 10)
                        throw e;
                    numExceptions++;
                    continue;
                }

                numExceptions = 0;

                _pauseFramesEvent.Reset();

                Deployment.Current.Dispatcher.BeginInvoke(
                    () =>
                        {
                            if(NewCameraFrame != null && _pumpFrames)
                            {
                                    NewCameraFrame(this, new CameraFrameEventArgs { ARGBData = pixels });
                            }
                            _pauseFramesEvent.Set();
                        }
                    );
            }
        }

        public void UpdateOrientation(PageOrientation orientation)
        {
            if (orientation == PageOrientation.PortraitDown)
            {
                viewfinderBrush.RelativeTransform =
                    new CompositeTransform { CenterX = 0.5, CenterY = 0.5, Rotation = -90 };
            }
            else if (orientation == PageOrientation.PortraitUp)
            {
                viewfinderBrush.RelativeTransform =
                    new CompositeTransform { CenterX = 0.5, CenterY = 0.5, Rotation = 90 };
            }
            else
            {
                viewfinderBrush.RelativeTransform =
                    new CompositeTransform { CenterX = 0.5, CenterY = 0.5, Rotation = 0 };
            }
        }
    }
}
        
