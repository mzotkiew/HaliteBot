using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    public class Solution
    {
        Map map;
        public readonly int[][,] moves;
        public readonly int[] productionInTurn;
        public readonly int[] strengthInTurn;
        public readonly int[,] whenVisited;
        public readonly int whenMove;
        public readonly double distanceMultiplier;
        public readonly int turnCount;
        public readonly int targetCount;
        public readonly int regionPercent;

        public Solution(int[][,] moves, Map map, int whenMove, double distanceMultiplier, int turnCount, int targetCount, int regionPercent)
        {
            this.map = map;
            this.moves = moves;
            int n = map.map_width;
            int m = map.map_height;
            this.whenMove = whenMove;
            this.distanceMultiplier = distanceMultiplier;
            this.turnCount = turnCount;
            this.targetCount = targetCount;
            this.regionPercent = regionPercent;

            whenVisited = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    whenVisited[i, j] = int.MaxValue;
            productionInTurn = new int[moves.Length];
            strengthInTurn = new int[moves.Length];
            for (int t = 0; t < productionInTurn.Length-1; t++)
            {
                if (t > 0)
                    strengthInTurn[t] += strengthInTurn[t - 1];
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                    {
                        if (moves[t][i, j] >= 0 && whenVisited[i, j] == int.MaxValue)
                        {
                            whenVisited[i, j] = t;
                            if (t > 0)
                            {
                            //    strengthInTurn[t - 1] -= map.contents[i, j].strength;
                                strengthInTurn[t] -= map.contents[i, j].strength;
                            }
                            else
                                strengthInTurn[t] += map.contents[i, j].strength;
                        }

                        if (whenVisited[i, j] != int.MaxValue)
                        {
                            productionInTurn[t] += map.contents[i, j].production;
                            if (moves[t][i, j] == 4 || moves[t][i, j] == -1)
                                strengthInTurn[t+1] += map.contents[i, j].production;
                        }
                    }
            }
        }

        public int compare(Solution hisSolution, out int whenConflict)
        {
            whenConflict = 240;
            if (productionInTurn.Length < 10)
                return -10000;
            if (hisSolution.productionInTurn.Length < 10)
                return -10000;

            int n = map.map_width;
            int m = map.map_height;
            int enemiesCount = map.enemyX.GetLength(0);
            for (int ii = 0; ii < n; ii++)
                for (int jj = 0; jj < m; jj++)
                    if (whenVisited[ii, jj] < int.MaxValue)
                        for (int k = 0; k < 4; k++)
                        {
                            int i = map.newX[k, ii];
                            int j = map.newY[k, jj];

                            for (int e = 1; e < enemiesCount; e++)
                                if (e != map.myId)
                                {
                                    int conflictWhen = whenVisited[i, j];
                                    if (conflictWhen < hisSolution.whenVisited[map.enemyX[e, i], map.enemyY[e, j]])
                                        conflictWhen = hisSolution.whenVisited[map.enemyX[e, i], map.enemyY[e, j]];
                                    if (conflictWhen < whenConflict)
                                        whenConflict = conflictWhen;
                                }
                        }
            int time = whenConflict;
            if (time > productionInTurn.Length-2)
                time = productionInTurn.Length - 2;

            if (time > hisSolution.productionInTurn.Length-2)
                time = hisSolution.productionInTurn.Length - 2;

            int productionPercent = 1000 * (productionInTurn[time] - hisSolution.productionInTurn[time]) / Math.Max(productionInTurn[time] + 1, hisSolution.productionInTurn[time] + 1);
            int strengthPercent = 1000 * (strengthInTurn[time] - hisSolution.strengthInTurn[time]) / Math.Max(strengthInTurn[time] + 1, hisSolution.strengthInTurn[time] + 1);

            return productionPercent + strengthPercent;
        }
    }
}
