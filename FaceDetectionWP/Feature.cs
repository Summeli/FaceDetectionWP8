using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace FaceDetectionWinPhone
{
    /// <summary>
    /// Represents a feature in an image, that is, the difference of sums of two different rectangular regions in an image.
    /// </summary>
    class Feature
    {
        public RectFeature[] rects;
        public int nb_rects;
        public float threshold;
        public float left_val;
        public float right_val;
        public Point size;
        public int left_node;
        public int right_node;
        public bool has_left_val;
        public bool has_right_val;


        public Feature(float threshold, float left_val, int left_node, bool has_left_val,
        float right_val, int right_node, bool has_right_val, Point size)
        {
            nb_rects = 0;
            rects = new RectFeature[3];
            this.threshold = threshold;
            this.left_val = left_val;
            this.left_node = left_node;
            this.has_left_val = has_left_val;
            this.right_val = right_val;
            this.right_node = right_node;
            this.has_right_val = has_right_val;
            this.size = size;
        }

        /// <summary>
        /// Given an image, says whether classifier should go left or right int the classification tree.
        /// </summary>
        /// <param name="grayImage">integral of source image</param>
        /// <param name="squares">integral of squares</param>
        /// <param name="i">x coordinate</param>
        /// <param name="j">y coordinate</param>
        /// <param name="scale">scale to scale image to (i.e. 2 would look for stuff twice as big)</param>
        /// <returns></returns>
        public int getLeftOrRight(int[][] grayImage, int[][] squares, int i, int j, float scale)
        {
            // Figure out size of the image
            int w = (int)(scale * size.X);
            int h = (int)(scale * size.Y);

            // can fold w*h into a constant
            // division?
            double inv_area = 1 / (double)(w * h);


            // Get a normalization Coefficient
            int total_x = grayImage[i + w][ j + h] + grayImage[i][ j] - grayImage[i][ j + h] - grayImage[i + w][ j];
            int total_x2 = squares[i + w][ j + h] + squares[i][ j] - squares[i][ j + h] - squares[i + w][ j];
            double moy = total_x * inv_area;
            double vnorm = total_x2 * inv_area - moy * moy;

            // sqrt?
            vnorm = (vnorm > 1) ? Math.Sqrt(vnorm) : 1;

            int rect_sum = 0;
            for (int k = 0; k < nb_rects; k++)
            {
                RectFeature r = rects[k];
                // Get coordinates of rectangles for computation and add them to sum
                // This is the feature value computation for each feature
                int rx1 = i + (int)(scale * r.x1);
                int rx2 = i + (int)(scale * (r.x1 + r.y1));
                int ry1 = j + (int)(scale * r.x2);
                int ry2 = j + (int)(scale * (r.x2 + r.y2));
                rect_sum += (int)((grayImage[rx2][ ry2] - grayImage[rx1][ ry2] - grayImage[rx2][ry1] + grayImage[rx1][ ry1]) * r.weight);
            }
            // Normalize by area
            double rect_sum2 = rect_sum * inv_area;

            // If the sum of the rectangle area is less than the threshold * a normalization factor go left otherwise go right.
            return (rect_sum2 < threshold * vnorm) ? Tree.LEFT : Tree.RIGHT;

        }

        public void add(RectFeature r)
        {
            rects[nb_rects++] = r;
        }
    }
}
