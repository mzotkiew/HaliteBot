using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    public class Region
    {
        double factor;
        public
                List<List<Region>> children=new List<List<Region>>(); //Tries to make it 4x4.

        public Region(int _w, int _h, Random rand)
        {
            factor = Math.Pow(rand.NextDouble(), 1.5);
            children.Clear();
            const int CHUNK_SIZE = 4;
            if (_w == 1 && _h == 1) return;
            int cw = _w / CHUNK_SIZE, ch = _h / CHUNK_SIZE;
            int difW = _w - CHUNK_SIZE * cw, difH = _h - CHUNK_SIZE * ch;
            for (int a = 0; a < CHUNK_SIZE; a++)
            {
                int tch = a < difH ? ch + 1 : ch;
                if (tch > 0)
                {
                    children.Add(new List<Region>());
                    for (int b = 0; b < CHUNK_SIZE; b++)
                    {
                        int tcw = b < difW ? cw + 1 : cw;
                        if (tcw > 0)
                        {
                            children[children.Count - 1].Add(new Region(tcw, tch, rand));
                        }
                    }
                }
            }
            const double OWN_WEIGHT = 0.75;
            for (int z = 0; z < 1; z++)
            { //1 iterations found by experiment.
                double[,] blurredFactors = new double[children.Count, children[0].Count];
                for (int a = 0; a < children.Count; a++)
                {
                    int mh = a - 1, ph = a + 1;
                    if (mh < 0) mh += children.Count;
                    if (ph == children.Count) ph = 0;
                    for (int b = 0; b < children[0].Count; b++)
                    {
                        int mw = b - 1, pw = b + 1;
                        if (mw < 0) mw += children[0].Count;
                        if (pw == children[0].Count) pw = 0;
                        blurredFactors[a, b] += children[a][b].factor * OWN_WEIGHT;
                        blurredFactors[a, b] += children[mh][b].factor * (1 - OWN_WEIGHT) / 4;
                        blurredFactors[a, b] += children[ph][b].factor * (1 - OWN_WEIGHT) / 4;
                        blurredFactors[a, b] += children[a][mw].factor * (1 - OWN_WEIGHT) / 4;
                        blurredFactors[a, b] += children[a][pw].factor * (1 - OWN_WEIGHT) / 4;
                    }
                }
                for (int a = 0; a < children.Count; a++) for (int b = 0; b < children[0].Count; b++) children[a][b].factor = blurredFactors[a, b]; //Set factors.
            }
        }

        public double[,] getFactors()
        {
            if (children.Count == 0) return new double[1, 1] { { factor } };
            double[,][,] childrenFactors = new double[children.Count, children[0].Count][,];
            for (int a = 0; a < children.Count; a++)
            {
                for (int b = 0; b < children[0].Count; b++)
                {
                    childrenFactors[a, b] = children[a][b].getFactors();
                }
            }
            int width = 0, height = 0;
            for (int a = 0; a < children.Count; a++) height += childrenFactors[a, 0].GetLength(0);
            for (int b = 0; b < children[0].Count; b++) width += childrenFactors[0, b].GetLength(1);
            double[,] factors = new double[height, width];
            int x = 0, y = 0;
            for (int my = 0; my < children.Count; my++)
            {
                for (int iy = 0; iy < childrenFactors[my, 0].GetLength(0); iy++)
                {
                    for (int mx = 0; mx < children[0].Count; mx++)
                    {
                        for (int ix = 0; ix < childrenFactors[0, mx].GetLength(1); ix++)
                        {
                            factors[y, x] = childrenFactors[my, mx][iy, ix] * factor;
                            x++;
                        }
                    }
                    y++;
                    x = 0;
                }
            }
            return factors;
        }
    }
}
