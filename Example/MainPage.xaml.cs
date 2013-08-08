using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using FaceDetectionWP8.Resources;
using FaceDetectionWinPhone;

namespace FaceDetectionWP8
{
    public partial class MainPage : PhoneApplicationPage
    {

        static bool _faceRecoThreadAlive;              // Specifies whether face recognition thread is alive (I can't just stop the thread...)

        const string MODEL_FILE = "models/haarcascade_frontalface_alt.xml";
        FaceDetectionWinPhone.Detector _detector;

        int _downsampleFactor = 4;
        private byte[] _pixelDataGray;
        private byte[] _pixelDataDownsampled;
        private int[] _pixelDataGrayInt;
        private WriteableBitmap _wb;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        private void btnPhotoChoose_Clicked(object sender, RoutedEventArgs e)
        {
            PhotoChooserTask photo = new PhotoChooserTask();
            photo.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);
            photo.ShowCamera = true;
            photo.Show();
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                //Code to display the photo on the page in an image control named myImage.
                BitmapImage bmp = new BitmapImage();
                bmp.SetSource(e.ChosenPhoto);
                facesPic.Source = bmp;
            }

        }
    }
}