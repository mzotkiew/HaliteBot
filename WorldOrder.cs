using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    class WorldOrder
    {
        int playerCount;
        int activePlayers;
        bool[,] atWar;
        int[] strengths;
        int[] productions;
        int[] territories;
        bool[] enemiesToAttack;
        Map map;
        bool[,,] enemyCanReach;

        bool[] ifCanAttackEnemy(Map map)
        {
            bool[] res = new bool[strengths.Length];
            res[0] = true;
       
            if(activePlayers==2)
            {
                for (int i = 0; i < strengths.Length; i++)
                    res[i] = true;
                return res;
            }

            int strengthAgainstMe = 0;
            int productionAgainstMe = 0;
            for (int i = 1; i < strengths.Length; i++)
                if (atWar[i, map.myId])
                {
                    strengthAgainstMe += strengths[i];
                    productionAgainstMe += productions[i];
                    res[i] = true;
                }

            for (int i = 1; i < strengths.Length; i++)
                if (i != map.myId)
                {
                    int strengthAgainstHim = 0;
                    int productionAgainstHim = 0;
                    for (int j = 1; j < strengths.Length; j++)
                        if (atWar[i, j])
                        {
                            strengthAgainstHim += strengths[j];
                            productionAgainstHim += productions[j];
                        }

                    if ((strengthAgainstHim + strengths[i]+strengthAgainstMe + productionAgainstHim + productions[i] + productionAgainstMe) * 3 < strengths[map.myId] + productions[map.myId])
                        res[i] = true;
                }
            return res;
        }

        bool[] enemiesMoveStartWarWith(int x, int y)
        {
            bool[] res = new bool[playerCount];

            if (map.contents[x, y].owner != 0 || map.contents[x, y].strength == 0)
                return res;

            for (int k = 0; k < 4; k++)
            {
                Location newLoc = new Location(map.newX[k, x], map.newY[k, y]);
                for (int p = 1; p < playerCount; p++)
                    if (enemyCanReach[p, newLoc.x, newLoc.y])
                        res[p] = true;
                int[] sumStrength = new int[playerCount];
                for (int kk = 0; kk < 4; kk++)
                {
                    Location newLoc2 = new Location(map.newX[kk, newLoc.x], map.newY[kk, newLoc.y]);
                    sumStrength[map.contents[newLoc2.x, newLoc2.y].owner] += map.contents[newLoc2.x, newLoc2.y].strength;
                }
                for (int i = 0; i < playerCount; i++)
                    if (sumStrength[i] > map.contents[newLoc.x, newLoc.y].strength)
                        res[i] = true;
            }
            res[map.myId] = false;
            res[0] = false;
            return res;
        }

        int[,] distanceToEnemy(int enemyId)
        {
            int n = map.map_width;
            int m = map.map_height;
            int[,] res = new int[n, m];
            Queue<Location> queue = new Queue<Location>();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    if (map.contents[i,j].owner == enemyId)
                    {
                        res[i, j] = 0;
                        queue.Enqueue(new Location(i, j));
                    }
                    else
                        res[i, j] = int.MaxValue;
                }

            while (queue.Count > 0)
            {
                Location now = queue.Dequeue();
                for (int k = 0; k < 4; k++)
                {
                    Location newLoc = new Location(map.newX[k, now.x], map.newY[k, now.y]);
                    if (map.contents[newLoc.x, newLoc.y].owner == 0 && map.contents[newLoc.x, newLoc.y].strength > 0)
                        continue;
                    if (res[newLoc.x, newLoc.y] <= res[now.x, now.y] + 1)
                        continue;
                    res[newLoc.x, newLoc.y] = res[now.x, now.y] + 1;
                    queue.Enqueue(newLoc);
                }
            }
            return res;
        }

        bool[] findNearEnemies()
        {
            int n = map.map_width;
            int m = map.map_height;
            bool[] res = new bool[playerCount];
            for(int i=0;i<n;i++)
                for(int j=0;j<m;j++)
                    if(map.contents[i,j].owner==0)
                        for(int k=0;k<4;k++)
                            if(map.contents[map.newX[k,i],map.newY[k,j]].owner==map.myId)
                            {
                                for (int kk = 0; kk < 4; kk++)
                                    res[map.contents[map.newX[kk, i], map.newY[kk, j]].owner] = true;
                                break;
                            }
            return res;
        }

        int bestEnemyToAttack()
        {
            bool anyWar = false;
            for (int i = 0; i < playerCount; i++)
                for (int j = i + 1; j < playerCount; j++)
                    if (atWar[i, j])
                        anyWar = true;

            bool[] nearEnemies = findNearEnemies();

            bool meAtWar = false;
            for (int p = 1; p < playerCount; p++)
                if (p != map.myId && atWar[map.myId, p])
                    meAtWar = true;
            if (meAtWar)
                for (int p = 1; p < playerCount; p++)
                    if (!atWar[map.myId, p])
                        nearEnemies[p] = false;

            bool amIStrongest = true;
            for (int i = 1; i < playerCount; i++)
                if (territories[i] > territories[map.myId])
                    amIStrongest = false;
            if (!anyWar && amIStrongest)
                return -1;

            bool amIWorst = true;
            for (int i = 1; i < playerCount; i++)
                if (territories[i]>0 && territories[i] < territories[map.myId])
                    amIWorst = false;

            int lowestStrengthId = -1;
            int lowestStrength = strengths[map.myId]/2;
            if (amIWorst)
                lowestStrength = int.MaxValue;
            for (int i = 1; i < playerCount; i++)
                if (nearEnemies[i] && i != map.myId && lowestStrength > strengths[i] && 5 * productions[i] > productions[map.myId])
                {
                    lowestStrength = strengths[i];
                    lowestStrengthId = i;
                }
            return lowestStrengthId;
        }

        public WorldOrder(Map map)
        {
            this.map = map;
            int n = map.map_width;
            int m = map.map_height;
            playerCount = map.myId;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (map.contents[i, j].owner > playerCount)
                        playerCount = map.contents[i, j].owner;
            playerCount++;

            strengths = new int[playerCount];
            productions = new int[playerCount];
            territories = new int[playerCount];
            atWar = new bool[playerCount, playerCount];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (map.contents[i, j].owner > 0)
                    {
                        productions[map.contents[i, j].owner] += map.contents[i, j].production;
                        strengths[map.contents[i, j].owner] += map.contents[i, j].strength;
                        territories[map.contents[i, j].owner]++;
                    }

            enemyCanReach = new bool[playerCount, n, m];
            for (int p = 1; p < playerCount; p++)
            {
                int[,] dd = new int[n, m];
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                        dd[i, j] = int.MaxValue;
                Queue<Location> queue = new Queue<Location>();
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                        if (map.contents[i, j].owner == p)
                        {
                            enemyCanReach[p, i, j] = true;
                            queue.Enqueue(new Location(i, j));
                            dd[i, j] = 0;
                        }

                while (queue.Count > 0)
                {
                    Location now = queue.Dequeue();
                    if (dd[now.x, now.y] > 3)
                        continue;
                    for (int k = 0; k < 4; k++)
                    {
                        Location newLoc = new Location(map.newX[k, now.x], map.newY[k, now.y]);
                        if (enemyCanReach[p, newLoc.x, newLoc.y])
                            continue;
                        if (map.contents[newLoc.x, newLoc.y].owner == 0 && map.contents[newLoc.x, newLoc.y].strength > 0)
                            continue;
                        if (map.contents[newLoc.x, newLoc.y].owner != p && map.contents[newLoc.x, newLoc.y].owner != 0)
                            continue;
                        enemyCanReach[p, newLoc.x, newLoc.y] = true;
                        queue.Enqueue(newLoc);
                        dd[newLoc.x, newLoc.y] = dd[now.x, now.y] + 1;
                    }
                }
            }

            for(int i=0;i<n;i++)
                for(int j=0;j<m;j++)
                    for(int p1=1;p1<playerCount;p1++)
                        if(enemyCanReach[p1,i,j])
                            for(int p2=p1+1;p2<playerCount;p2++)
                                if(enemyCanReach[p2,i,j])
                                {
                                    atWar[p1, p2] = true;
                                    atWar[p2, p1] = true;
                                }

            for (int i = 1; i < playerCount; i++)
            {
                atWar[0, i] = false;
                atWar[i, i] = false;
            }

            activePlayers = 0;
            for (int i = 1; i < strengths.Length; i++)
                if (strengths[i] > 0)
                    activePlayers++;

            enemiesToAttack = ifCanAttackEnemy(map);
        }

        public bool ifStartWar(int[,] moves)
        {
            int n = map.map_width;
            int m = map.map_height;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (map.contents[i, j].owner == map.myId && map.contents[i, j].strength > 0 && moves[i, j] < 4 && moves[i, j] >= 0)
                    {
                        Location now = new Location(map.newX[moves[i, j], i], map.newY[moves[i, j], j]);
                        bool[] newWars = enemiesMoveStartWarWith(now.x, now.y);
                        for (int l = 1; l < newWars.Length; l++)
                            if (l != map.myId && newWars[l])
                                return true;
                    }
            return false;
        }

        public bool isAtWar()
        {
            for (int i = 0; i < playerCount; i++)
                if (atWar[map.myId, i])
                    return true;
            return false;
        }

        public bool canMoveTo(int x, int y, int attackedStrength)
        {
            bool[] enemiesAtWar = enemiesMoveStartWarWith(x, y);
            for (int i = 1; i < playerCount; i++)
                if (enemiesAtWar[i] && (!enemiesToAttack[i] || (attackedStrength>0 && activePlayers>2)))
                    return false;
            return true;
        }

        public Location findBreakThroughLocation()
        {
            int n = map.map_width;
            int m = map.map_height;
            int enemyToAttack = bestEnemyToAttack();
      //      Console.Error.WriteLine("e: " + enemyToAttack);

            if (enemyToAttack == -1)
                return null;

            int[][,] distancesToEnemy = new int[playerCount][,];
            for (int i = 1; i < playerCount; i++)
                distancesToEnemy[i] = distanceToEnemy(i);

            Location res = null;
            int sumNearness = int.MaxValue;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (map.contents[i, j].owner == 0)
                        for (int k = 0; k < 4; k++)
                            if (map.contents[map.newX[k, i], map.newY[k, j]].owner == map.myId)
                            {
                                bool attackOther = false;
                                for (int kk = 0; kk < 4; kk++)
                                    if (map.contents[map.newX[kk, i], map.newY[kk, j]].owner > 0 && map.contents[map.newX[kk, i], map.newY[kk, j]].owner != map.myId && map.contents[map.newX[kk, i], map.newY[kk, j]].owner != enemyToAttack)
                                        attackOther = true;
                                if (attackOther)
                                    break;

                                for (int kk = 0; kk < 4; kk++)
                                    if (map.contents[map.newX[kk, i], map.newY[kk, j]].owner == enemyToAttack)
                                    {
                                        int nearness = 0;
                                        for (int l = 1; l < playerCount; l++)
                                            if (distancesToEnemy[l][map.newX[kk, i], map.newY[kk, j]] > 0)
                                            {
                                                if(l!=map.myId)
                                                    nearness += 100000 / distancesToEnemy[l][map.newX[kk, i], map.newY[kk, j]];
                                                else
                                                    nearness += 100000 / distancesToEnemy[l][map.newX[kk, i], map.newY[kk, j]];
                                            }
                                        if (nearness < sumNearness)
                                        {
                                            sumNearness = nearness;
                                            res = new Location(i, j);
                                        }
                                    }
                                break;
                            }
            return res;

        }
    }
}
