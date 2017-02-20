using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    class Strategy
    {
        int n, m;
        Map map;
        bool[,] myFields;
        int[,] strength;
        int[,] moves;
        int[,,] attackMoves;
        List<Location> locations;

        public Strategy(Map map, bool[,] myImportant, bool[,] myFields)
        {
            locations = new List<Location>();
            this.map = map;
            n = map.map_width;
            m = map.map_height;
            moves = new int[n, m];
            strength = new int[n, m];
            this.myFields = new bool[n, m];
            attackMoves = new int[n, m, 5];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    this.myFields[i, j] = myFields[i, j];
                    moves[i, j] = -1;
                    if (myFields[i,j])
                    {
                        strength[i, j] = map.contents[i, j].strength+map.contents[i,j].production;
                        if (myImportant[i, j])
                        {
                            moves[i, j] = 4;
                            locations.Add(new Location(i, j));
                        }
                    }
                }
        }

        public static bool[,] findMyImportant(Map map, bool[,] myFields, bool[,] hisFields)
        {
            int n = map.map_width;
            int m = map.map_height;
            bool[,] res = new bool[n, m];
            Queue<Location>[] queue = new Queue<Location>[3];
            for (int i = 0; i < 3; i++)
                queue[i] = new Queue<Location>();
            int[,] distance = new int[n, m];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    distance[i, j] = int.MaxValue;
                    if (map.contents[i, j].owner == 0)
                    {
                        if (map.contents[i, j].strength == 0)
                        {
                            distance[i, j] = 2;
                            queue[2].Enqueue(new Location(i, j));
                        }
                        else
                            distance[i, j] = 0;
                    }
                    if (hisFields[i, j] && map.contents[i, j].strength > 0)
                    {
                        distance[i, j] = 0;
                        queue[0].Enqueue(new Location(i, j));
                    }
                }

            for (int l = 0; l < 3; l++)
                while (queue[l].Count > 0)
                {
                    Location now = queue[l].Dequeue();
                    if (distance[now.x, now.y] < l)
                        continue;
                    for (int k = 0; k < 4; k++)
                    {
                        Location newLoc = new Location(map.newX[k, now.x], map.newY[k, now.y]);
                        if (distance[newLoc.x, newLoc.y] <= l + 1)
                            continue;
                        if (myFields[newLoc.x, newLoc.y] && map.contents[newLoc.x, newLoc.y].strength>0)
                            res[newLoc.x, newLoc.y] = true;
                        distance[newLoc.x, newLoc.y] = l + 1;
                        if (l < 2)
                            queue[l + 1].Enqueue(newLoc);
                    }
                }
            return res;
        }

        public static bool[,] findMyFields(Map map, int id)
        {
            int n = map.map_width;
            int m = map.map_height;
            bool[,] res = new bool[n, m];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (map.contents[i, j].owner == id)
                        res[i, j] = true;

            return res;
        }

        public static bool[,] findHisFields(Map map, int id)
        {
            int n = map.map_width;
            int m = map.map_height;
            bool[,] res = new bool[n, m];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (map.contents[i, j].owner != id && map.contents[i, j].owner>0)
                        res[i, j] = true;

            return res;
        }

        public static Strategy selectBest(Strategy[] myStrategies, Strategy[] hisStrategies)
        {
            // TODO

            return myStrategies[0];
        }

        int howMuchIHave(Strategy enemy, int x, int y)
        {
            int lost = enemy.strength[x, y];
            if (strength[x, y] > 255)
                lost += strength[x, y] - 255;
            if (map.contents[x, y].owner == 0)
                lost += map.contents[x, y].strength;

            for (int k = 0; k < 4; k++)
                lost += enemy.strength[map.newX[k, x], map.newY[k, y]];
            if (lost < strength[x, y] || (lost==0 && myFields[x,y]))
                return strength[x, y] -lost + map.contents[x, y].production*2;
            return 0;
        }

        int countScoreComponent(Strategy enemy, int x, int y)
        {
            int res = 0;
            for (int i = x -2; i <= x + 2; i++)
                for (int j = y -2; j <= y + 2; j++)
                {
                    int ii = (i + n) % n;
                    int jj = (j + m) % m;
                    res += howMuchIHave(enemy, ii, jj);
                    res -= enemy.howMuchIHave(this, ii, jj);
                }
            return res;
        }

        int checkMove(Strategy enemy, int x, int y, int k)
        {
            int res = -countScoreComponent(enemy, x, y);

            if (moves[x, y] != 4)
                strength[map.newX[moves[x, y], x], map.newY[moves[x, y], y]] -= map.contents[x, y].strength;
            else
                strength[x, y] -= map.contents[x, y].strength + map.contents[x, y].production;

            if (k != 4)
                strength[map.newX[k, x], map.newY[k, y]] += map.contents[x, y].strength;
            else
                strength[x, y] += map.contents[x, y].strength + map.contents[x, y].production;

            res -= attackMoves[x, y, moves[x, y]] * map.contents[x, y].strength / 10;
            res += attackMoves[x, y, k] * map.contents[x, y].strength / 10;

            res += countScoreComponent(enemy, x, y);

            if (k != 4)
                strength[map.newX[k, x], map.newY[k, y]] -= map.contents[x, y].strength;
            else
                strength[x, y] -= map.contents[x, y].strength + map.contents[x, y].production;

            if (moves[x, y] != 4)
                strength[map.newX[moves[x, y], x], map.newY[moves[x, y], y]] += map.contents[x, y].strength;
            else
                strength[x, y] += map.contents[x, y].strength + map.contents[x, y].production;

            return res;
        }

        void makeMove(int x, int y, int k)
        {
            if (moves[x, y] != 4)
                strength[map.newX[moves[x, y], x], map.newY[moves[x, y], y]] -= map.contents[x, y].strength;
            else
                strength[x, y] -= map.contents[x, y].strength+map.contents[x,y].production;

            if (k != 4)
                strength[map.newX[k, x], map.newY[k, y]] += map.contents[x, y].strength;
            else
                strength[x, y] += map.contents[x, y].strength + map.contents[x, y].production;
            moves[x, y] = k;
        }

        public void attack(double prob, Random rand, int dirVariance)
        {
            Queue<Location> queue = new Queue<Location>();
            int[,] distance = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    if (myFields[i, j])
                    {
                        distance[i, j] = 0;
                        queue.Enqueue(new Location(i, j));
                    }
                    else
                        distance[i, j] = int.MaxValue;
                }

            while (queue.Count > 0)
            {
                Location now = queue.Dequeue();
                for (int k = 0; k < 4; k++)
                {
                    Location newLoc = new Location(map.newX[k, now.x], map.newY[k, now.y]);
                    if (myFields[newLoc.x, newLoc.y] || (map.contents[newLoc.x, newLoc.y].owner == 0 && map.contents[newLoc.x, newLoc.y].strength > 0) || distance[newLoc.x, newLoc.y] <= distance[now.x, now.y] + 1)
                        continue;
                    distance[newLoc.x, newLoc.y] = distance[now.x, now.y] + 1;
                    if (distance[newLoc.x, newLoc.y] < 3)
                        queue.Enqueue(newLoc);
                }
            }

            foreach (Location loc in locations)
                if (rand.NextDouble() < prob+(double)map.contents[loc.x,loc.y].strength/255.0)
                {
                    int[] dirScore = new int[5];
                    for (int k = 0; k < 5; k++)
                    {
                        if (k == 4 || distance[map.newX[k, loc.x], map.newY[k, loc.y]] < int.MaxValue)
                        {
                            for (int i = -1; i <= 1; i++)
                                for (int j = -1; j <= 1; j++)
                                {
                                    int ii = (loc.x + i + n) % n;
                                    int jj = (loc.y + j + m) % m;
                                    if (k < 4)
                                    {
                                        ii = (map.newX[k, loc.x] + i + n) % n;
                                        jj = (map.newY[k, loc.y] + j + m) % m;
                                    }
                                    if (distance[ii, jj] < int.MaxValue)
                                        dirScore[k] += distance[ii, jj];
                                }
                            if (k<4 && !myFields[map.newX[k, loc.x], map.newY[k, loc.y]])
                                dirScore[k] += 1;
                        }
                        else
                            dirScore[k] = -10000;
                    }



                    int bestDir = 0;
                    int bestScore = 0;
                    int bestActualScore = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        int tempScore = dirScore[k] + rand.Next(dirVariance);
                        if (tempScore > bestScore)
                        {
                            bestScore = tempScore;
                            bestDir = k;
                            bestActualScore = dirScore[k];
                        }
                    }
                    makeMove(loc.x, loc.y, bestDir);
                    for (int k = 0; k < 4; k++)
                    {
                        if (dirScore[k] < dirScore[4])
                            attackMoves[loc.x, loc.y, k] = -1;
                        if (dirScore[k] >=bestActualScore)
                            attackMoves[loc.x, loc.y, k] = 1;
                    }

                }
        }

        public void tryUpgrade(Strategy[] enemies, Random rand, double temperature, bool[,] forbidden)
        {
            if (locations.Count == 0)
                return;
            int who = rand.Next(locations.Count);
            int x = locations[who].x;
            int y = locations[who].y;
            int dir =  rand.Next(5);
            while (dir == moves[x, y])
                 dir=rand.Next(5);

            if (dir<4 && forbidden[map.newX[dir, x], map.newY[dir, y]])
                return;

            int score = 0;
            foreach (Strategy enemy in enemies)
                score += checkMove(enemy, x, y, dir);

            if (score >= 0 || rand.NextDouble() < Math.Pow(Math.E, (double)score / temperature))
            {
                makeMove(x, y, dir);
      //          Console.WriteLine(score);
            }
/*
            lastDir++;
            if(lastDir==5)
            {
                lastDir = 0;
                lastLoc++;
                if (lastLoc == locations.Count)
                    lastLoc = 0;
            }*/
        }

        public int[,] getMoves()
        {
            return moves;
        }
    }
}
