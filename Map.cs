using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    public class Map
    {
        public Site[,] contents;
        public int map_width, map_height; //Number of rows and columns, NOT maximum index.

        Map()
        {
            map_width = 0;
            map_height = 0;
            contents = new Site[map_height, map_width];
        }
        public Map(Map otherMap)
        {
            map_width = otherMap.map_width;
            map_height = otherMap.map_height;
            contents = new Site[otherMap.contents.GetLength(0), otherMap.contents.GetLength(1)];
            for (int i = 0; i < contents.GetLength(0); i++)
                for (int j = 0; j < contents.GetLength(1); j++)
                {
                    contents[i, j] = new Site();
                    contents[i, j].owner = otherMap.contents[i, j].owner;
                    contents[i, j].production = otherMap.contents[i, j].production;
                    contents[i, j].strength = otherMap.contents[i, j].strength;
                    contents[i, j].id = otherMap.contents[i, j].id;
                    contents[i, j].gain = otherMap.contents[i, j].gain;
                }
        }

        public Map(string[] production, string[] state, int width, int height)
        {
            map_width = width;
            map_height = height;
            contents = new Site[width,height];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    contents[i,j] = new Site();
                    contents[i,j].production = int.Parse(production[j*width+ i]);
                    contents[i,j].strength = int.Parse(state[state.Length - width * height + j * width + i]);
                }
                
            int bound = -1;
            int owner = 0;
            int index = 0;
            for (int j = 0; j < height; j++)
                for (int i = 0; i < width; i++)
                {
                    int now = j * width + i;
                    if (now > bound)
                    {
                        bound += int.Parse(state[index]);
                        owner = int.Parse(state[index + 1]);
                        index += 2;
                    }
                    contents[i,j].owner = owner;
                }

            updateGain();
        }

        public Map(int width, int height, int numberOfPlayers, int seed)
        {
            //Pseudorandom number generator.
            Random rand = new Random(seed);

            //Decides whether to put more players along the horizontal or the vertical.
            bool preferHorizontal = rand.Next(2) == 0 ? true : false;

            int dw, dh;
            //Find number closest to square that makes the match symmetric.
            if (preferHorizontal)
            {
                dh = (int)Math.Sqrt(numberOfPlayers);
                while (numberOfPlayers % dh != 0) dh--;
                dw = numberOfPlayers / dh;
            }
            else
            {
                dw = (int)Math.Sqrt(numberOfPlayers);
                while (numberOfPlayers % dw != 0) dw--;
                dh = numberOfPlayers / dw;
            }

            //Figure out chunk width and height accordingly.
            //Matches width and height as closely as it can, but is not guaranteed to match exactly.
            //It is guaranteed to be smaller if not the same size, however.
            int cw = width / dw;
            int ch = height / dh;

            //Ensure that we'll be able to move the tesselation by a uniform amount.
            if (preferHorizontal) while (ch % numberOfPlayers != 0) ch--;
            else while (cw % numberOfPlayers != 0) cw--;

            map_width = cw * dw;
            map_height = ch * dh;

            Region prodRegion = new Region(cw, ch, rand);
            double[,] prodChunk = prodRegion.getFactors();

            Region strengthRegion = new Region(cw, ch, rand);
            double[,] strengthChunk = strengthRegion.getFactors();



            //We'll first tesselate the map; we'll apply our various translations and transformations later.
            SiteD[,] tesselation = new SiteD[map_height, map_width];
            for (int i = 0; i < map_height; i++)
                for (int j = 0; j < map_width; j++)
                    tesselation[i, j] = new SiteD();
            for (int a = 0; a < dh; a++)
            {
                for (int b = 0; b < dw; b++)
                {
                    for (int c = 0; c < ch; c++)
                    {
                        for (int d = 0; d < cw; d++)
                        {
                            tesselation[a * ch + c, b * cw + d].production = prodChunk[c, d];
                            tesselation[a * ch + c, b * cw + d].strength = strengthChunk[c, d];
                        }
                    }
                    tesselation[a * ch + ch / 2, b * cw + cw / 2].owner = a * dw + b + 1; //Set owners.
                }
            }

            //We'll now apply the reflections to the map.
            bool reflectVertical = dh % 2 == 0, reflectHorizontal = dw % 2 == 0; //Am I going to reflect in the horizontal vertical directions at all?
            SiteD[,] reflections = new SiteD[map_height, map_width];
            for (int a = 0; a < dh; a++)
            {
                for (int b = 0; b < dw; b++)
                {
                    bool vRef = reflectVertical && a % 2 != 0, hRef = reflectHorizontal && b % 2 != 0; //Do I reflect this chunk at all?
                    for (int c = 0; c < ch; c++)
                    {
                        for (int d = 0; d < cw; d++)
                        {
                            reflections[a * ch + c, b * cw + d] = tesselation[a * ch + (vRef ? ch - c - 1 : c), b * cw + (hRef ? cw - d - 1 : d)];
                        }
                    }
                }
            }

            //Next, let's apply our shifts to create the shifts map.
            SiteD[,] shifts = new SiteD[map_height, map_width];
            if (preferHorizontal)
            {
                int shift = rand.Next(dw) * (map_height / dw); //A vertical shift.
                for (int a = 0; a < dh; a++)
                {
                    for (int b = 0; b < dw; b++)
                    {
                        for (int c = 0; c < ch; c++)
                        {
                            for (int d = 0; d < cw; d++)
                            {
                                shifts[a * ch + c, b * cw + d] = reflections[(a * ch + b * shift + c) % map_height, b * cw + d];
                            }
                        }
                    }
                }
            }
            else
            {
                int shift = rand.Next(dh) * (map_width / dh); //A horizontal shift.
                for (int a = 0; a < dh; a++)
                {
                    for (int b = 0; b < dw; b++)
                    {
                        for (int c = 0; c < ch; c++)
                        {
                            for (int d = 0; d < cw; d++)
                            {
                                shifts[a * ch + c, b * cw + d] = reflections[a * ch + c, (b * cw + a * shift + d) % map_width];
                            }
                        }
                    }
                }
            }

            //Apply a final blur to create the blur map. This will fix the edges where our transformations have created jumps or gaps.
            const double OWN_WEIGHT = 0.66667;
            SiteD[,] blur = shifts;
            for (int z = 0; z <= 2 * Math.Sqrt(map_width * map_height) / 10; z++)
            {
                SiteD[,] newBlur = blur;
                for (int a = 0; a < map_height; a++)
                {
                    int mh = a - 1, ph = a + 1;
                    if (mh < 0) mh += map_height;
                    if (ph == map_height) ph = 0;
                    for (int b = 0; b < map_width; b++)
                    {
                        int mw = b - 1, pw = b + 1;
                        if (mw < 0) mw += map_width;
                        if (pw == map_width) pw = 0;
                        newBlur[a, b].production *= OWN_WEIGHT;
                        newBlur[a, b].production += blur[mh, b].production * (1 - OWN_WEIGHT) / 4;
                        newBlur[a, b].production += blur[ph, b].production * (1 - OWN_WEIGHT) / 4;
                        newBlur[a, b].production += blur[a, mw].production * (1 - OWN_WEIGHT) / 4;
                        newBlur[a, b].production += blur[a, pw].production * (1 - OWN_WEIGHT) / 4;
                        newBlur[a, b].strength *= OWN_WEIGHT;
                        newBlur[a, b].strength += blur[mh, b].strength * (1 - OWN_WEIGHT) / 4;
                        newBlur[a, b].strength += blur[ph, b].strength * (1 - OWN_WEIGHT) / 4;
                        newBlur[a, b].strength += blur[a, mw].strength * (1 - OWN_WEIGHT) / 4;
                        newBlur[a, b].strength += blur[a, pw].strength * (1 - OWN_WEIGHT) / 4;
                    }
                }
                blur = newBlur;
            }

            //Let's now normalize the map values.
            double maxProd = 0, maxStr = 0;
            SiteD[,] normalized = blur;
            for (int a = 0; a < normalized.GetLength(0); a++) for (int b = 0; b < normalized.GetLength(1); b++)
                {
                    if (normalized[a, b].production > maxProd) maxProd = normalized[a, b].production;
                    if (normalized[a, b].strength > maxStr) maxStr = normalized[a, b].strength;
                }
            for (int a = 0; a < normalized.GetLength(0); a++) for (int b = 0; b < normalized.GetLength(1); b++)
                {
                    normalized[a, b].production /= maxProd;
                    normalized[a, b].strength /= maxStr;
                }

            //Finally, fill in the contents vector.
            int TOP_PROD = rand.Next(10) + 6;
            int TOP_STR = rand.Next(106) + 150;
            contents = new Site[map_height, map_width];
            for (int i = 0; i < map_height; i++)
                for (int j = 0; j < map_width; j++)
                    contents[i, j] = new Site();
            for (int a = 0; a < map_height; a++) for (int b = 0; b < map_width; b++)
                {
                    contents[a, b].owner = normalized[a, b].owner;
                    contents[a, b].strength = (int)Math.Round(normalized[a, b].strength * TOP_STR);
                    contents[a, b].production = (int)Math.Round(normalized[a, b].production * TOP_PROD);
                    if (contents[a, b].owner != 0 && contents[a, b].production == 0) contents[a, b].production = 1;
                }

            int tt = map_width;
            map_width = map_height;
            map_height = tt;

            updateGain();
        }

        void updateGain()
        {
            for (int i = 0; i < map_width; i++)
                for (int j = 0; j < map_height; j++)
                    contents[i, j].gain = Math.Min(255, Math.Max(1, (double)contents[i, j].strength / (double)contents[i, j].production));
        }

        public bool inBounds(Location l)
        {
            return l.x < map_width && l.y < map_height;
        }

        public Location getLocation(Location l, int direction)
        {
            Location res = new Location(l.x, l.y);
            if (direction != 'x')
            {
                if (direction == 0)
                {
                    if (l.y == 0) res.y = map_height - 1;
                    else res.y--;
                }
                else if (direction == 1)
                {
                    if (l.x == map_width - 1) res.x = 0;
                    else res.x++;
                }
                else if (direction == 2)
                {
                    if (l.y == map_height - 1) res.y = 0;
                    else res.y++;
                }
                else if (direction == 3)
                {
                    if (l.x == 0) res.x = map_width - 1;
                    else res.x--;
                }
            }
            return res;
        }
        public Site getSite(Location l, char direction = 'x')
        {
            l = getLocation(l, direction);
            return contents[l.x, l.y];
        }

        public int[,] enemyX;
        public int[,] enemyY;
        public int[,] newX;
        public int[,] newY;
        public int myId;
        public int enemiesCount;

        public void initEnemyXY(int myId, Location me, int[,] newX, int[,] newY)
        {
            this.myId = myId;
            this.newX = newX;
            this.newY = newY;
            int n = map_width;
            int m = map_height;
            enemiesCount = 0;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (contents[i, j].owner > 0)
                        enemiesCount++;

            enemyX = new int[enemiesCount + 1, n];
            enemyY = new int[enemiesCount + 1, m];

            int[] cx = new int[4] { 1, -1, 1, -1 };
            int[] cy = new int[4] { 1, 1, -1, -1 };

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (contents[i, j].owner > 0 && contents[i, j].owner != myId)
                    {
                        int bestOption = 0;
                        int bestScore = int.MaxValue;
                        for (int k = 0; k < 4; k++)
                        {
                            int tempScore = 0;
                            for (int x = -5; x <= 5; x++)
                                for (int y = -5; y <= 5; y++)
                                {
                                    int myProduction = contents[(me.x + x + n) % n, (me.y + y + m) % m].production;
                                    int hisProduction = contents[(i + x * cx[k] + n) % n, (j + y * cy[k] + m) % m].production;
                                    if (myProduction > hisProduction)
                                        tempScore += myProduction - hisProduction;
                                    else
                                        tempScore += hisProduction - myProduction;
                                }
                            if (tempScore < bestScore)
                            {
                                bestScore = tempScore;
                                bestOption = k;
                            }
                        }

                        for (int x = 0; x < n; x++)
                            enemyX[contents[i, j].owner, x] = ((x - me.x) * cx[bestOption] + i + n) % n;

                        for (int y = 0; y < m; y++)
                            enemyY[contents[i, j].owner, y] = ((y - me.y) * cy[bestOption] + j + m) % m;
                    }
        }
    }
}
