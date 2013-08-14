using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace RealtimeExsample
{
    public class Utils
    {
        /// <summary>
        /// Downsamples the input image by downsampleFactor.
        /// downsampled data goes into downsampled param.
        /// </summary>
        public static void DownSample(byte[] input, int width, int height, ref byte[] downsampled, int downsampleFactor)
        {
            for (int y = 0, yy = 0; y < height; y += downsampleFactor, yy++)
            {
                for (int x = 0, xx = 0; x < width; x += downsampleFactor, xx++)
                {
                    downsampled[yy * width / downsampleFactor + xx] = input[y * width + x];
                }
            }
        }

        /// <summary>
        /// Downsamples the input image by downsampleFactor.
        /// downsampled data goes into downsampled param.
        /// </summary>
        public static void DownSample(int[] input, int width, int height, ref int[] downsampled, int downsampleFactor)
        {
            for (int y = 0, yy = 0; y < height; y += downsampleFactor, yy++)
            {
                for (int x = 0, xx = 0; x < width; x += downsampleFactor, xx++)
                {
                    downsampled[yy * width / downsampleFactor + xx] = input[y * width + x];
                }
            }
        }

        /// <summary>
        /// Converts a grayscale byte[] array to ARGB. Converts grayData into passed in intData.
        /// </summary>
        public static void GrayToARGB(byte[] grayData, ref int[] intData)
        {
            for (int i = 0; i < grayData.Length; i++)
            {
                byte value = grayData[i];
                intData[i] = 0xff << 24 | (value << 16) | value << 8 | value;
            }
        }


        // general guidance from http://azzlsoft.com/tag/histogram-equalization/
        /// <summary>
        /// Calculates the CDF of a grayscale image (used for histogram equalization)
        /// </summary>
        /// <param name="grayData" />
        /// <returns></returns>
        private static double[] CalculateCdf(byte[] grayData)
        {
            var histogramData = new int[256];
            for (int i = 0; i < grayData.Length; i++)
            {
                histogramData[grayData[i]]++;
            }

            var cdf = new double[256];
            cdf[0] = (double)histogramData[0] / grayData.Length;
            for (int i = 1; i < cdf.Length; i++)
            {
                cdf[i] = (double)histogramData[i] / grayData.Length + cdf[i - 1];
            }
            return cdf;
        }

        /// <summary>
        /// Perform histogram equalization on an image, modifying input grayData array
        /// </summary>
        public static void HistogramEqualization(ref byte[] grayData)
        {
            double[] cdf = CalculateCdf(grayData);
            for (int i = 0; i < grayData.Length; i++)
            {
                byte val = grayData[i];
                // equalize, not sure what the 0.5 is for
                var newVal = (byte)(255 * cdf[val] + 0.5);
                grayData[i] = newVal;
            }

        }

        /// <summary>
        /// Given RGB image copies it to byteData
        /// </summary>
        public static void ARGBToGreyScale(int[] imageData, ref byte[] byteData)
        {

            for (int i = 0; i < imageData.Length; i++)
            {
                int c = imageData[i];
                int red = (c & 0x00ff0000) >> 16;
                int green = (c & 0x0000ff00) >> 8;
                int blue = c & 0x000000ff;
                var value = (byte)((30 * red + 59 * green + 11 * blue) / 100);
                byteData[i] = value;
            }
        }

    }
}
