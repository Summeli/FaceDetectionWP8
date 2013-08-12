using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.IO;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using FaceDetectionWP8.Resources;
using FaceDetectionWinPhone;
using System.Windows.Shapes;

namespace FaceDetectionWP8
{
    public partial class MainPage : PhoneApplicationPage
    {
        const string MODEL_FILE = "haarcascade_frontalface_alt.xml";
        FaceDetectionWinPhone.Detector m_detector;
        
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            m_detector = new FaceDetectionWinPhone.Detector(System.Xml.Linq.XDocument.Load(MODEL_FILE));
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
                BitmapImage bmp = new BitmapImage();
                bmp.SetSource(e.ChosenPhoto);
                WriteableBitmap btmMap = new WriteableBitmap(bmp);
            
                List<FaceDetectionWinPhone.Rectangle> faces =
                     m_detector.getFaces(
                     btmMap, 10f, 1f, 0.05f, 1, false, false);

                //update the bitmap before drawing it.
                foreach (var r in faces)
                {
                    int x = Convert.ToInt32(r.X);
                    int y = Convert.ToInt32(r.Y);
                    int width = Convert.ToInt32(r.Width);
                    int height = Convert.ToInt32(r.Height);
                    btmMap.FillRectangle(x, y, x + height, y + width, System.Windows.Media.Colors.Red);
                }
                btmMap.Invalidate();
                facesPic.Source = btmMap;
   
            }

        }
    }
}