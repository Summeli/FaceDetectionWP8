using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FaceDetectionWinPhone
{
    /// <summary>
    /// Represents a single decision tree in the cascade classifier
    /// </summary>
    class Tree
    {
        public const int LEFT = 0;
        public const int RIGHT = 1;

        List<Feature> m_features;

        public Tree()
        {
            m_features = new List<Feature>();
        }

        public void addFeature(Feature f)
        {
            m_features.Add(f);
        }

        // don't really understand this code
        public float getVal(int[][] grayImage, int[][] squares, int i, int j, float scale)
        {
            Feature cur_node = m_features[0];

            while (true)
            {
                int where = cur_node.getLeftOrRight(grayImage, squares, i, j, scale);
                if (where == LEFT)
                {
                    if (cur_node.has_left_val)
                    {
                        return cur_node.left_val;
                    }
                    else
                    {
                        cur_node = m_features[cur_node.left_node -1 ];
                    }
                }
                else
                {
                    if (cur_node.has_right_val)
                    {
                        return cur_node.right_val;
                    }
                    else
                    {
                        cur_node = m_features[cur_node.right_node -1];
                    }
                }
            }
        }
    }
}
