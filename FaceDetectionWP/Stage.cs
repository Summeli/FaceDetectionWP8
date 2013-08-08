using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FaceDetectionWinPhone
{
    /// <summary>
    /// Represents a single stage in the set of classifiers, i.e. a single decision tree
    /// </summary>
    class Stage
    {
        public List<Tree> trees;
        public float threshold;
        public Stage(float threshold)
        {
            this.threshold = threshold;
            trees = new List<Tree>();
        }

        public void addTree(Tree t)
        {
            trees.Add(t);
        }

        public bool pass(int[][] grayImage, int[][] squares, int i, int j, float scale)
        {
            float sum = 0;
            foreach (Tree t in trees)
            {
                sum += t.getVal(grayImage, squares, i, j, scale);

                // Pretty sure that getVal will only return nonnegative numbers, not positive.
                if (sum > threshold) return true;
            }
            return sum > threshold;
        }
    }
}
