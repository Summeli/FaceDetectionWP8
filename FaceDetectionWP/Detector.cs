using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Diagnostics;
using System.Windows;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Media.Imaging;

namespace FaceDetectionWinPhone
{

    /// <summary>
    /// Class that performs face detection (this is the one you care about!)
    /// </summary>
    public class Detector
    {


        List<Stage> m_stages;                   // List of classifiers the image needs to pass to be considered an image
        System.Windows.Point m_size;            // This is how big each of the face detection regions is
        int[,] m_canny;                         // array used for canny pruning (not actually used)
        int[,] m_grad;                          // array used for canny pruning (not actually used)

        // we can cache number of rows and columns since the image size is always the same
        int m_nrows = -1;
        int m_ncols = -1;

        /// <summary>
        /// Factory method to create face detectors
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Detector Create(String path)
        {
            try
            {
                return new Detector(XDocument.Load(path));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Construct the detector given an XML document describing the model file.
        /// This method parses the XML file and builds the feature medel
        /// </summary>
        /// <param name="document"></param>
        public Detector(XDocument document)
        {
            //Debug.WriteLine("loading document " + document);

            m_stages = new List<Stage>();

            var root = document.Root.Elements().First();
            Debug.Assert(root != null, "xml document root is not haarcascade_frontalface_alt");

            // Get the size of the classifier (size of the region to look at, i.e. 20 x 20
            string[] sizeStr = (from node in root.Descendants()
                                where node.Name.LocalName == "size"
                                select node.Value.Trim().Split(' ')).First().ToArray();
            m_size = new System.Windows.Point(Convert.ToDouble(sizeStr[0], CultureInfo.InvariantCulture), Convert.ToDouble(sizeStr[1], CultureInfo.InvariantCulture));

            var stagesRoot = root.Descendants().Where(x => x.Name.LocalName == "stages").First();
            var stages = stagesRoot.Elements();
            foreach (XElement stage in stages)
            {
                // There's an extra level for some reason so we have to do down one
                var trueStage = stage;
                float stage_threshold = (float)Convert.ToDouble(trueStage.Element("stage_threshold").Value.Trim(), CultureInfo.InvariantCulture);
                Stage st = new Stage(stage_threshold);
                var trees = trueStage.Element("trees");
                foreach (XElement tree in trees.Elements())
                {
                    // There's an extra level for some reason so we have to do down one
                    XElement trueTree = tree.Elements().First();
                    Tree t = new Tree();
                    XElement feature = trueTree.Element("feature");
                    float threshold = (float)Convert.ToDouble(trueTree.Element("threshold").Value.Trim(),CultureInfo.InvariantCulture);
                    int left_node = -1;
                    float left_val = 0;
                    bool has_left_val = false;
                    int right_node = -1;
                    float right_val = 0;
                    bool has_right_val = false;
                    XElement e = trueTree.Element("left_val");
                    if (e != null)
                    {
                        left_val = (float)Convert.ToDouble(e.Value.Trim(), CultureInfo.InvariantCulture);
                        has_left_val = true;
                    }
                    else
                    {
                        left_node = Convert.ToInt32(trueTree.Element("left_node").Value.Trim(), CultureInfo.InvariantCulture);
                        has_left_val = false;
                    }
                    e = trueTree.Element("right_val");
                    if (e != null)
                    {
                        right_val = (float)Convert.ToDouble(e.Value.Trim(), CultureInfo.InvariantCulture);
                        has_right_val = true;
                    }
                    else
                    {
                        right_node = Convert.ToInt32(trueTree.Element("right_node").Value.Trim(), CultureInfo.InvariantCulture);
                        has_right_val = false;
                    }
                    Feature f = new Feature(threshold, left_val, left_node, has_left_val, right_val, right_node, has_right_val, m_size);
                    var rects = feature.Element("rects");
                    foreach (var r in rects.Elements())
                    {
                        string rstr = r.Value.Trim();
                        RectFeature rect = RectFeature.fromString(rstr);
                        f.add(rect);
                    }

                    t.addFeature(f);
                    st.addTree(t);

                }
                m_stages.Add(st);

            }

            //Debug.WriteLine("Number of stages is " + m_stages.Count);
            //Debug.WriteLine("size str is " + m_stages.Count);
        }


        /// <summary>
        /// Returns a list of rectangles representing detected objects from Viola-jones.
        /// 
        /// The algorithm tests, from sliding windows on the image at different scales which regions should be considered as searched objects.
        /// Please see Wikipedia for a description of the algorithm.
        /// </summary>
        /// <param name="file">the image file containing the image you want to detect</param>
        /// <param name="baseScale"> The initial ratio between the size of your image and the size of the sliding window (default: 2)</param>
        /// <param name="scale_inc">How much to increment your window for every iteration (default:1.25)</param>
        /// <param name="increment">How much to shif the window at each step, in terms of the % of the window size</param>
        /// <param name="doCannyPruning"> Whether or not to do canny pruning, i.e. rejecting objects based on # of edges (doesn't actually have an effect)</param>
        /// <param name="min_neighbors"> Minimum number of overlapping face rectangles to be considered a valid face (default: 1)</param>
        /// <param name="multipleFaces"> Whether or not to detect multiple faces</param>
        public List<Rectangle> getFaces(String file, float baseScale, float scale_inc, float increment, int min_neighbors, bool doCannyPruning, bool multipleFaces)
        {
            try
            {
                WriteableBitmap image = new WriteableBitmap(new BitmapImage(new Uri(file, UriKind.Absolute)));
                var result = getFaces(image, baseScale, scale_inc, increment, min_neighbors, doCannyPruning, multipleFaces);
                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return null;
        }

        /// <summary>
        /// Returns a list of rectangles representing detected objects from Viola-jones.
        /// 
        /// The algorithm tests, from sliding windows on the image at different scales which regions should be considered as searched objects.
        /// Please see Wikipedia for a description of the algorithm.
        /// </summary>
        /// <param name="image">the image you want to find stuff in</param>
        /// <param name="baseXcale"> The initial ratio between the size of your image and the size of the sliding window (default: 2)</param>
        /// <param name="scale_inc">How much to increment your window for every iteration (default:1.25)</param>
        /// <param name="increment">How much to shif the window at each step, in terms of the % of the window size</param>
        /// <param name=param name="doCannyPruning"> Whether or not to do canny pruning, i.e. rejecting objects based on # of edges (doesn't actually have an effect)</param>
        /// <param name="min_neighbors"> Minimum number of overlapping face rectangles to be considered a valid face (default: 1)</param>
        public List<Rectangle> getFaces(WriteableBitmap image, float baseScale, float scale_inc, float increment, int min_neighbors, bool doCannyPruning, bool multipleFaces)
        {
            return getFaces(image.Pixels, image.PixelWidth, image.PixelHeight, baseScale, scale_inc, increment, min_neighbors, doCannyPruning, multipleFaces);
        }

        private int[][] InitArray(int width, int height)
        {
            int[][]result = new int[width][];
            for (int i = 0; i < width; i++)
            {
                result[i] = new int[height];
            }
            return result;
        }

        /// <summary>
        /// Returns a list of rectangles representing detected objects from Viola-jones.
        /// 
        /// The algorithm tests, from sliding windows on the image at different scales which regions should be considered as searched objects.
        /// Please see Wikipedia for a description of the algorithm.
        /// </summary>
        /// <param name="imageData">int array of the image you want to find stuff in</param>
        /// <param name="baseXcale"> The initial ratio between the size of your image and the size of the sliding window (default: 2)</param>
        /// <param name="scale_inc">How much to increment your window for every iteration (default:1.25)</param>
        /// <param name="increment">How much to shif the window at each step, in terms of the % of the window size</param>
        /// <param name=param name="doCannyPruning"> Whether or not to do canny pruning, i.e. rejecting objects based on # of edges (doesn't actually have an effect)</param>
        /// <param name="min_neighbors"> Minimum number of overlapping face rectangles to be considered a valid face (default: 1)</param>
        public List<Rectangle> getFaces(int[] imageData, int width, int height, float baseScale, float scale_inc, float increment, int min_neighbors, bool doCannyPruning, bool multipleFaces)
        {
            List<Rectangle> ret = new List<Rectangle>();
            float maxScale = (float)(Math.Min((width + 0.0f) / m_size.X, (height + 0.0f) / m_size.Y));
            // What is grayImage?
            int[][] grayImage = InitArray(width, height);
            // WHat is img?
            int[][] img = InitArray(width, height);
            // What is squares?
            int[][] squares = InitArray(width, height);

            // buil the grey image and squared image
            for (int i = 0; i < width; i++)
            {
                int col = 0;
                int col2 = 0;
                for (int j = 0; j < height; j++)
                {
                    // get hte color at the particular pixel
                    int c = imageData[j * width + i];
                    int red = (c & 0x00ff0000) >> 16;
                    int green = (c & 0x0000ff00) >> 8;
                    int blue = c & 0x000000ff;
                    int value = (30 * red + 59 * green + 11 * blue) / 100;
                    img[i][j] = value;
                    grayImage[i][j] = (i > 0 ? grayImage[i - 1][j] : 0) + col + value;
                    squares[i][j] = (i > 0 ? squares[i - 1][j] : 0) + col2 + value * value;
                    col += value;
                    col2 += value * value;
                }
            }

            // TODO: uncomment if it works
            // Do canny pruning
            //if (doCannyPruning)
            //    getIntegralCanny(img);


            // Do detection at every scale size
            for (float scale = baseScale; scale < maxScale; scale *= scale_inc)
            {
                int step = (int)(scale * m_size.X * increment);
                int size = (int)(scale * m_size.X);
                for (int i = 0; i < width - size; i += step)
                {
                    for (int j = 0; j < height - size; j += step)
                    {
                        if (doCannyPruning)
                        {
                            int edges_density = m_canny[i + size, j + size] + m_canny[i, j] - m_canny[i, j + size] - m_canny[i + size, j];
                            int d = edges_density / size / size;
                            if (d < 20 || d > 100)
                                continue;
                        }
                        bool pass = true;
                        int k = 0;

                        // Check if the rectangle passes at the location and scale we care about 
                        foreach (Stage s in m_stages)
                        {
                            if (!s.pass(grayImage, squares, i, j, scale))
                            {
                                pass = false;
                                //Debug.WriteLine("\t {0},{1} Failed at Stage {2}", i, j, k);
                                break;
                            }
                            k++;
                        }
                        if (pass)
                        {
                            ret.Add(new Rectangle(i, j, size, size));
                            //Debug.WriteLine("found face! {0}, {1}, {2}, {3}", i, j, size, size);
                            if(!multipleFaces)
                                return ret;
                        }
                    }
                }
            }

            //return ret;
            return merge(ret, min_neighbors);
        }

        public void getIntegralCanny(int[,] grayImage)
        {
            m_nrows = grayImage.GetLength(0);
            m_ncols = grayImage.GetLength(1); ;
            // possible bug location
            m_canny = new int[m_nrows, m_ncols];

            for (int i = 0; i < m_nrows; i++)
            {
                for (int j = 0; j < m_ncols; j++)
                {
                    m_canny[i, j] = 0;
                }
            }
            for (int i = 2; i < m_nrows - 2; i++)
            {
                for (int j = 2; j < m_ncols - 2; j++)
                {
                    int sum = 0;
                    sum += 2 * grayImage[i - 2, j - 2];
                    sum += 4 * grayImage[i - 2, j - 1];
                    sum += 5 * grayImage[i - 2, j + 0];
                    sum += 4 * grayImage[i - 2, j + 1];
                    sum += 2 * grayImage[i - 2, j + 2];
                    sum += 4 * grayImage[i - 1, j - 2];
                    sum += 9 * grayImage[i - 1, j - 1];
                    sum += 12 * grayImage[i - 1, j + 0];
                    sum += 9 * grayImage[i - 1, j + 1];
                    sum += 4 * grayImage[i - 1, j + 2];
                    sum += 5 * grayImage[i + 0, j - 2];
                    sum += 12 * grayImage[i + 0, j - 1];
                    sum += 15 * grayImage[i + 0, j + 0];
                    sum += 12 * grayImage[i + 0, j + 1];
                    sum += 5 * grayImage[i + 0, j + 2];
                    sum += 4 * grayImage[i + 1, j - 2];
                    sum += 9 * grayImage[i + 1, j - 1];
                    sum += 12 * grayImage[i + 1, j + 0];
                    sum += 9 * grayImage[i + 1, j + 1];
                    sum += 4 * grayImage[i + 1, j + 2];
                    sum += 2 * grayImage[i + 2, j - 2];
                    sum += 4 * grayImage[i + 2, j - 1];
                    sum += 5 * grayImage[i + 2, j + 0];
                    sum += 4 * grayImage[i + 2, j + 1];
                    sum += 2 * grayImage[i + 2, j + 2];

                    m_canny[i, j] = sum / 159;
                    //System.out.println(canny[i][j]);
                }
            }
            if (m_grad == null)
            {
                m_grad = new int[m_nrows, m_ncols];
            }
            for (int i = 1; i < m_nrows - 1; i++)
                for (int j = 1; j < m_ncols - 1; j++)
                {
                    int grad_x = -m_canny[i - 1, j - 1] + m_canny[i + 1, j - 1] - 2 * m_canny[i - 1, j] + 2 * m_canny[i + 1, j] - m_canny[i - 1, j + 1] + m_canny[i + 1, j + 1];
                    int grad_y = m_canny[i - 1, j - 1] + 2 * m_canny[i, j - 1] + m_canny[i + 1, j - 1] - m_canny[i - 1, j + 1] - 2 * m_canny[i, j + 1] - m_canny[i + 1, j + 1];
                    m_grad[i, j] = Math.Abs(grad_x) + Math.Abs(grad_y);
                    //System.out.println(grad[i][j]);
                }
            //JFrame f = new JFrame();
            //f.setContentPane(new DessinChiffre(grad));
            //f.setVisible(true);
            for (int i = 0; i < m_nrows; i++)
            {
                int col = 0;
                for (int j = 0; j < m_ncols; j++)
                {
                    int value = m_grad[i, j];
                    m_canny[i, j] = (i > 0 ? m_canny[i - 1, j] : 0) + col + value;
                    col += value;
                }
            }
        }
        /// <summary>
        /// Merges multiple rectangles representing the same face into one face. Does so by finding all rectangles with overlapping regions and 
        /// taking the average of all corners of these rectangles
        /// </summary>
        /// <param name="rects">Input rectangles</param>
        /// <param name="min_neighbors">Minimum number of neighbors for a face to be considered valid?</param>
        /// <returns></returns>
        public List<Rectangle> merge(List<Rectangle> rects, int min_neighbors)
        {
            //Debug.WriteLine("merging, num rects is {0}, min_neibhgors is {1}", rects.Count, min_neighbors);
            List<Rectangle> retour = new List<Rectangle>();
            int[] ret = new int[rects.Count];
            int nb_classes = 0;
            for (int i = 0; i < rects.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < i; j++)
                {
                    if (equals(rects[j], rects[i]))
                    {
                        found = true;
                        ret[i] = ret[j];
                    }
                }
                if (!found)
                {
                    ret[i] = nb_classes;
                    nb_classes++;
                }
            }

            // merge all of the rectangles together by averaging. and take the average of the merged rectangles.
            int[] neighbors = new int[nb_classes];
            Rectangle[] rect = new Rectangle[nb_classes];
            for (int i = 0; i < nb_classes; i++)
            {
                neighbors[i] = 0;
                rect[i] = new Rectangle(0, 0, 0, 0);
            }
            for (int i = 0; i < rects.Count; i++)
            {
                neighbors[ret[i]]++;
                rect[ret[i]].X += rects[i].X;
                rect[ret[i]].Y += rects[i].Y;
                rect[ret[i]].Height += rects[i].Height;
                rect[ret[i]].Width += rects[i].Width;
            }
            for (int i = 0; i < nb_classes; i++)
            {
                int n = neighbors[i];
                if (n >= min_neighbors)
                {
                    Rectangle r = new Rectangle(0, 0, 0, 0);
                    r.X = (rect[i].X * 2 + n) / (2 * n);
                    r.Y = (rect[i].Y * 2 + n) / (2 * n);
                    r.Width = (rect[i].Width * 2 + n) / (2 * n);
                    r.Height = (rect[i].Height * 2 + n) / (2 * n);
                    retour.Add(r);
                }
            }
            return retour;
        }

        /// <summary>
        /// Tests if two rectangles actually represent the same face
        /// </summary>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <returns></returns>
        public bool equals(Rectangle r1, Rectangle r2)
        {
            int distance = (int)(r1.Width * 0.2);

            if (r2.X <= r1.X + distance &&
                   r2.X >= r1.X - distance &&
                   r2.Y <= r1.Y + distance &&
                   r2.Y >= r1.Y - distance &&
                   r2.Width <= (int)(r1.Width * 1.2) &&
                   (int)(r2.Width * 1.2) >= r1.Width) return true;
            // r1 is within r2
            if (r1.X >= r2.X && r1.X + r1.Width <= r2.X + r2.Width && r1.Y >= r2.Y && r1.Y + r1.Height <= r2.Y + r2.Height) return true;
            return false;
        }


    }



}
