using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    class ThirdBot : Bot
    {
        Random rand = new Random(7);

        Map map;
        int id;
        int playerCount;
        int n, m;
        Location me;
        int turnLimit;

        int[,] shortestMoves;

        int[] dx = new int[4] { 0, 1, 0, -1 };
        int[] dy = new int[4] { -1, 0, 1, 0 };
        int[] oppositeDir = new int[4] { 2, 3, 0, 1 };

        int[,] newX;
        int[,] newY;

        bool atWar = false;
        int turn = -1;

        int[,] lastEscapeMoves;
        int lastVacantCount = 0;

        Solution followedSolution;
        WorldOrder basicWorldOrder;

        public override void getFrame(Map map)
        {
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    this.map.contents[i, j] = map.contents[i, j];
        }

        public override void getInit(int id, Map map)
        {
            long time = DateTime.Now.ToFileTimeUtc();

            this.map = map;
            this.id = id;
            n = map.map_width;
            m = map.map_height;

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    if (map.contents[i, j].owner == id)
                        me = new Location(i, j);
                    if (map.contents[i, j].id > playerCount)
                        playerCount = map.contents[i, j].id;
                }

            playerCount++;
            turnAvailable = new int[n, m];
            turnVisited = new int[n, m];

            movesTypes = new List<int[,]>();
            for (int i = 0; i < 3000; i++)
                movesTypes.Add(new int[n, m]);

            newX = new int[4, n];
            newY = new int[4, m];
            for (int i = 0; i < n; i++)
            {
                newX[0, i] = i;
                newX[1, i] = (i + 1) % n;
                newX[2, i] = i;
                newX[3, i] = (i + n - 1) % n;
            }
            for (int i = 0; i < m; i++)
            {
                newY[0, i] = (i + m - 1) % m;
                newY[1, i] = i;
                newY[2, i] = (i + 1) % m;
                newY[3, i] = i;
            }

            initShortestMoves();

            map.initEnemyXY(id, me, newX, newY);

            basicWorldOrder = new WorldOrder(map);

            SolutionSet solutionSet = new SolutionSet(5);
            int added = 0;
            while (DateTime.Now.ToFileTimeUtc() - time < 25e7)
            {
                added++;
                solutionSet.addSolution(findNeighbor(solutionSet.getBest(), solutionSet.getAverageConflictTime()));
        //        break;
            }

            followedSolution = solutionSet.getBest();
            turnLimit = (int)(Math.Sqrt(n * m) * 10.0);
        }

        public override int[,] sendFrame()
        {
            turn++;

            WorldOrder worldOrder = new WorldOrder(map);
            if (turn >= followedSolution.moves.Length)
                atWar = true;
            if (!atWar && worldOrder.isAtWar())
                atWar = true;
            if (!atWar)
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                        if (map.contents[i, j].owner == id && followedSolution.moves[turn][i, j] == -1 && map.contents[i, j].strength > map.contents[i, j].production * followedSolution.whenMove)
                            atWar = true;
            if (!atWar && worldOrder.ifStartWar(followedSolution.moves[turn]))
                atWar = true;


            movesTypes.Add(new int[n, m]);
            if (atWar)
                return fight(worldOrder);

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (map.contents[i, j].owner == id && followedSolution.moves[turn][i, j] == -1)
                        followedSolution.moves[turn][i, j] = 4;

            int[,] owners = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    owners[i, j] = map.contents[i, j].owner;
            saveLastEscapeMoves(followedSolution.moves[turn],owners);

            return followedSolution.moves[turn];
            //  return getMoves();
        }

        public override string sendInit()
        {
            return "mzotkiew";
        }

        int[,] finish()
        {
            int[,] res = new int[n, m];
            int[,] moves = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    res[i, j] = -1;
            int[,] distance = new int[n, m];
            Queue<Location> queue = new Queue<Location>();
            for(int i=0;i<n;i++)
                for(int j=0;j<m;j++)
                {
                    if (map.contents[i, j].owner == id)
                        distance[i, j] = int.MaxValue;
                    else
                        queue.Enqueue(new Location(i, j));
                }


            while (queue.Count > 0)
            {
                Location now = queue.Dequeue();
                for (int k = 0; k < 4; k++)
                {
                    Location newLoc = new Location(newX[k, now.x], newY[k, now.y]);
                    if (distance[newLoc.x, newLoc.y] > distance[now.x, now.y] + 1)
                    {
                        distance[newLoc.x, newLoc.y] = distance[now.x, now.y] + 1;
                        queue.Enqueue(newLoc);
                        moves[newLoc.x, newLoc.y] = oppositeDir[k];
                        break;
                    }
                }
            }

            int[,] nowStrength = new int[n, m];
            int[,] newStrength = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    nowStrength[i, j] = map.contents[i, j].strength;


            for(int l=turnLimit-turn;l>0;l--)
            {
                bool moved = false;
                for(int i=0;i<n;i++)
                    for(int j=0;j<m;j++)
                        if(distance[i,j]==l && (l==turnLimit-turn || nowStrength[i,j]+newStrength[i,j]>255))
                        {
                            moved = true;
                            res[i, j] = moves[i, j];
                            nowStrength[i, j] -= map.contents[i, j].strength;
                            newStrength[newX[res[i,j],i],newY[res[i,j],j]]+= map.contents[i, j].strength;
                        }
                if (!moved)
                    break;
            }
            return res;

        }

        void saveLastEscapeMoves(int[,] moves, int[,] owners)
        {
            lastEscapeMoves = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    if (owners[i, j] ==id)
                        lastEscapeMoves[i, j] = moves[i, j];
                    else
                        lastEscapeMoves[i, j] = 4;
                }
        }

        Solution findNeighbor(Solution lastSolution, int turnLimit)
        {
            double distanceMultiplier = 0.2;
            int whenMove = 4;
            int targetCount = 200;
            int turnCount = 120;
            int regionPercent = 60;

            if (lastSolution != null)
            {
                whenMove = Math.Max(2, lastSolution.whenMove + rand.Next(-1, 2));
                distanceMultiplier = lastSolution.distanceMultiplier * (0.8 + rand.NextDouble() * 0.4);
                regionPercent = lastSolution.regionPercent * (80 + rand.Next(40)) / 100;
                if (regionPercent > 95)
                    regionPercent = 95;
                if (regionPercent < 50)
                    regionPercent = 50;
                turnCount = turnLimit * 3 / 2;
            }

            logTimesTenTable = getLogTimesTenTable(1.0 + distanceMultiplier);

            Location[] targets = findTargets(regionPercent);
            simpleChanges(targets);

            Location[] newTargets = new Location[targets.Length - 1];
            for (int i = 0; i < newTargets.Length; i++)
            {
                newTargets[i] = targets[i + 1];
         //       map.contents[newTargets[i].x, newTargets[i].y].id = i;
            }

            List<int[,]> moves = findMoves(newTargets, whenMove, targetCount);
            targetCount = 0;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (moves[moves.Count - 1][i, j] >= 0)
                        targetCount++;

            int[,] strengths = new int[n, m];
            int[,] owners = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    strengths[i, j] = map.contents[i, j].strength;
                    owners[i, j] = map.contents[i, j].owner;
                }
            for (int i = 0; i < moves.Count; i++)
            {
                int[,] newStrengths = null;
                int[,] newOwners = null;
                executeMove(strengths, owners, moves[i], out newStrengths, out newOwners);
                strengths = newStrengths;
                owners = newOwners;
            }

            while (moves.Count < turnCount)
            {
                int[,] escapeMoves = findEscapeMoves(basicWorldOrder, distanceMultiplier, strengths, owners,new List<Location>());
                howManyVacant(escapeMoves, strengths, owners, whenMove);
                int[,] newMoves = new int[n, m];
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                        newMoves[i, j] = -1;
                moveVacant(escapeMoves, newMoves, whenMove, strengths, owners);
                int[,] newStrengths = null;
                int[,] newOwners = null;
                executeMove(strengths, owners, newMoves, out newStrengths, out newOwners);
                strengths = newStrengths;
                owners = newOwners;
                moves.Add(newMoves);
            }

            return new Solution(moves.ToArray(), map, whenMove, distanceMultiplier, turnCount, targetCount, regionPercent);
        }

        void executeMove(int[,] strengths, int[,] owners, int[,] moves, out int[,] newStrengths, out int[,] newOwners)
        {
            newStrengths = new int[n, m];
            newOwners = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    newOwners[i, j] = owners[i, j];
                    newStrengths[i, j] = strengths[i, j];
                }
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (owners[i, j] == id)
                    {
                        if (moves[i, j] == 4)
                            newStrengths[i, j] += map.contents[i, j].production;
                        else if (moves[i, j] >= 0)
                        {
                            newStrengths[i, j] -= strengths[i, j];
                            newStrengths[newX[moves[i, j], i], newY[moves[i, j], j]] += strengths[i, j];
                            newOwners[newX[moves[i, j], i], newY[moves[i, j], j]] = id;
                        }
                    }
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (owners[i, j] != newOwners[i, j])
                        newStrengths[i, j] -= strengths[i, j] + strengths[i, j];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (newStrengths[i, j] > 255)
                        newStrengths[i, j] = 255;
        }

        void initShortestMoves()
        {
            shortestMoves = new int[n, m];
            int[,] distance = new int[n, m];
            Queue<Location>[] queues = new Queue<Location>[10000];
            for (int i = 0; i < queues.Length; i++)
                queues[i] = new Queue<Location>();
            queues[0].Enqueue(me);

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    distance[i, j] = int.MaxValue;
            distance[me.x, me.y] = 0;
            shortestMoves[me.x, me.y] = -1;

            for (int l = 0; l < queues.Length; l++)
                while (queues[l].Count > 0)
                {
                    Location now = queues[l].Dequeue();
                    if (distance[now.x, now.y] < l)
                        continue;

                    for (int k = 0; k < 4; k++)
                    {
                        Location newLoc = new Location(newX[k, now.x], newY[k, now.y]);
                        int newDist = distance[now.x, now.y] + 100 * map.contents[newLoc.x, newLoc.y].strength / (map.contents[newLoc.x, newLoc.y].production * 10 + 1);

                        if (newDist >= queues.Length)
                            newDist = queues.Length - 1;

                        if (newDist < distance[newLoc.x, newLoc.y])
                        {
                            distance[newLoc.x, newLoc.y] = newDist;
                            queues[newDist].Enqueue(newLoc);
                            shortestMoves[newLoc.x, newLoc.y] = oppositeDir[k];
                        }
                    }
                }
        }

        int[] logTimesTenTable;

        int[] getLogTimesTenTable(double distanceMultiplier)
        {
            int[] res = new int[3000];

            for(int i=1;i<res.Length;i++)
            {
                double now = (double)i / 10.0;
                double log = Math.Log(now, distanceMultiplier);

                res[i] = (int)(log * 10.0);

                if (res[i] < 0)
                    res[i] = 0;
            }
            return res;
        }

        int getStrengthToProductionLog2(int x, int y, int[,] strengthToProduction)
        {
            int index = strengthToProduction[x, y];
            if (index >= logTimesTenTable.Length)
                index = logTimesTenTable.Length-1;
            return logTimesTenTable[index];
        }

        int[,] getInducedStrengthToProduction(int[,] owners, int[,] strengths, out int[,] inducedStrength)
        {
            int[,] distances = new int[n, m];
            Location[,] parentLocations = new Location[n, m];
            inducedStrength = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    distances[i, j] = int.MaxValue;
                    parentLocations[i, j] = null;
                    inducedStrength[i, j] = strengths[i, j];
                }
           
            Queue<Location> queue = new Queue<Location>();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (owners[i, j] == 0)
                    {
                        bool enemyNear = false;
                        for (int k = 0; k < 4; k++)
                            if (owners[newX[k, i], newY[k, j]] > 0 && owners[newX[k, i], newY[k, j]] != id)
                            {
                                enemyNear = true;
                                break;
                            }
                        for (int k = 0; k < 4; k++)
                            if (owners[newX[k, i], newY[k, j]] == id)
                            {
                                distances[i, j] = 1;
                                parentLocations[i, j] = new Location(i, j);
                                if(!enemyNear)
                                    queue.Enqueue(new Location(i, j));
                                break;
                            }
                    }

            while (queue.Count > 0)
            {
                Location now = queue.Dequeue();
                for (int k = 0; k < 4; k++)
                {
                    Location newLoc = new Location(newX[k, now.x], newY[k, now.y]);
                    if (distances[newLoc.x, newLoc.y] <= distances[now.x,now.y])
                        continue;
                    if (owners[newLoc.x, newLoc.y] > 0)
                        continue;
                    if (distances[newLoc.x, newLoc.y] == distances[now.x, now.y] + 1)
                    {
                        int oldStrength = 1000* strengths[parentLocations[newLoc.x, newLoc.y].x, parentLocations[newLoc.x, newLoc.y].y];
                        int oldProduction = 1 + 10 *map.contents[parentLocations[newLoc.x, newLoc.y].x, parentLocations[newLoc.x, newLoc.y].y].production;

                        int newStrength = 1000*strengths[parentLocations[now.x, now.y].x, parentLocations[now.x, now.y].y];
                        int newProduction = 1+10*map.contents[parentLocations[now.x, now.y].x, parentLocations[now.x, now.y].y].production;

                        if (newStrength / newProduction > oldStrength / oldProduction)
                            continue;
                    }
                    parentLocations[newLoc.x, newLoc.y] = parentLocations[now.x, now.y];
                    if(distances[newLoc.x, newLoc.y]==int.MaxValue)
                        queue.Enqueue(newLoc);
                    distances[newLoc.x, newLoc.y] = distances[now.x, now.y] + 1;
                }
            }

            int[,] strengthSums = new int[n, m];
            int[,] productionSums = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (owners[i, j] == 0)
                    {
                        strengthSums[i, j] = 10000 * strengths[i, j];
                        productionSums[i, j] = 100 * map.contents[i, j].production+1;
                    }
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (owners[i, j] == 0 && parentLocations[i,j]!=null)
                    {
                        int ii = parentLocations[i, j].x;
                        int jj = parentLocations[i, j].y;
                  //      if(ii!=i || jj!=j)
                            inducedStrength[ii, jj] += strengths[i, j];
                        if (strengthSums[i, j] / productionSums[i, j] < strengthSums[ii, jj] / productionSums[ii, jj])
                        {
                            strengthSums[ii, jj] += strengthSums[i, j] / distances[i, j];
                            productionSums[ii,jj] += productionSums[i, j] / distances[i, j];
                        }
                    }

            int[,] res = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    if (distances[i, j] == 1)
                    {
                        if (strengthSums[i, j] < productionSums[i, j] * 100)
                            strengthSums[i, j] = productionSums[i, j] * 100;
                        res[i, j] = 10 + strengthSums[i, j] / productionSums[i, j] / 10;
                        if (res[i, j] < 1)
                            res[i, j] = 1;
                    }
                    else
                        res[i, j] = int.MaxValue;
                }

            return res;
        }

        int[,] newK = new int[2, 4] { { 0, 2, 1, 3 }, { 1, 3, 0, 2 } };

        int howManyVacant(int[,] moves, int[,] strengths, int[,] owners, int whenMoveLimit)
        {
            int res = 0;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if ( moves[i, j] > 4)
                    {
                        moves[i, j] -= 5;
                        if (owners[i, j] == id &&  strengths[i, j] > map.contents[i, j].production * whenMoveLimit)
                            res += strengths[i, j];
                    }
            return res;
        }

        int[,] findEscapeMoves(WorldOrder worldOrder, double distanceMultiplier, int[,] strengths, int[,] owners, List<Location> breakingPoints)
        {
            int[,] toNeutralProd = new int[n, m];
            int[,] toNeutralMove = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    toNeutralMove[i, j] = 9;
            Queue<Location>[,] queues = new Queue<Location>[2,10000];
            for(int l=0;l<2;l++)
            for (int i = 0; i < queues.GetLength(1); i++)
                queues[l,i] = new Queue<Location>();


            int[,] inducedStrength = null;
            int[,] strengthToProduction = getInducedStrengthToProduction(owners, strengths, out inducedStrength);

            Location[,] parentLocation = new Location[n, m];

            bool[,] forbidden = new bool[n, m];
            bool[,] visitedFirst = new bool[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (worldOrder!=null)
                        forbidden[i, j] = !worldOrder.canMoveTo(i, j, strengths[i, j]);
            foreach (Location breakingPoint in breakingPoints)
            {
                forbidden[breakingPoint.x, breakingPoint.y] = false;
                inducedStrength[breakingPoint.x, breakingPoint.y] = 1000;
                strengths[breakingPoint.x, breakingPoint.y] = 200;
            }

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    if (owners[i, j] == 0 && !forbidden[i, j])
                    {
                        toNeutralProd[i, j] += getStrengthToProductionLog2(i, j, strengthToProduction) + 100;
                        if (strengths[i, j] == 0)
                            inducedStrength[i, j] = 1000;

                        queues[0, toNeutralProd[i, j]].Enqueue(new Location(i, j));
                        visitedFirst[i, j] = true;
                        parentLocation[i, j] = new Location(i, j);
                    }
                    else
                        toNeutralProd[i, j] = int.MaxValue;
                }

            for (int l = 0; l < 2; l++)
                for (int i = 0; i < queues.GetLength(1); i++)
                    while (queues[l, i].Count > 0)
                    {
                        Location now = queues[l, i].Dequeue();
                        if (toNeutralProd[now.x, now.y] < i)
                            continue;

                        inducedStrength[parentLocation[now.x, now.y].x, parentLocation[now.x, now.y].y] -= strengths[now.x, now.y];
                        if (l == 0 && inducedStrength[parentLocation[now.x, now.y].x, parentLocation[now.x, now.y].y] < 0)
                        {
                            queues[1, i].Enqueue(now);
                            continue;
                        }

                        for (int kk = 0; kk < 4; kk++)
                        {
                            int k = newK[(now.x + now.y) % 2, kk];
                            Location newLoc = new Location(newX[k, now.x], newY[k, now.y]);
                            if (forbidden[newLoc.x, newLoc.y])
                                continue;
                            if (l == 1 && visitedFirst[newLoc.x, newLoc.y])
                                continue;
                            if (owners[newLoc.x, newLoc.y] != 0 || strengths[newLoc.x, newLoc.y] == 0)
                            {
                                int newProd = toNeutralProd[now.x, now.y] + 10;
                                if (toNeutralProd[newLoc.x, newLoc.y] > newProd)
                                {
                                    toNeutralMove[newLoc.x, newLoc.y] = oppositeDir[k];
                                    toNeutralProd[newLoc.x, newLoc.y] = newProd;
                                    parentLocation[newLoc.x, newLoc.y] = parentLocation[now.x, now.y];
                                    queues[l, newProd].Enqueue(newLoc);
                                    if (l == 0)
                                        visitedFirst[newLoc.x, newLoc.y] = true;
                                    if (l == 1)
                                        toNeutralMove[newLoc.x, newLoc.y] += 5;
                                }
                            }
                        }
                    }
/*
            Queue<Location> queue = new Queue<Location>();
            bool[,] visited = new bool[n, m];
            for(int i=0;i<n;i++)
                for(int j=0;j<m;j++)
                    if(owners[i,j]==id)
                        for(int k=0;k<4;k++)
                            if(owners[newX[k,i],newY[k,j]]==0)
                            {
                                queue.Enqueue(new Location(i, j));
                                visited[i, j] = true;
                                break;
                            }

            while (queue.Count > 0)
            {
                Location now = queue.Dequeue();
                for (int kk = 0; kk < 4; kk++)
                {
                    int k = newK[(now.x + now.y) % 2, kk];
                    Location newLoc = new Location(newX[k, now.x], newY[k, now.y]);
                    if (visited[newLoc.x, newLoc.y] || owners[newLoc.x, newLoc.y] != id)
                        continue;
                    visited[newLoc.x, newLoc.y] = true;
                    queue.Enqueue(newLoc);
                    if (toNeutralMove[newLoc.x, newLoc.y] == 4)
                        toNeutralMove[newLoc.x, newLoc.y] = oppositeDir[k];
                }
            }
            */
            return toNeutralMove;
        }

        void moveVacant(int[,] escapeMoves, int[,] res, int whenMoveLimit, int[,] strengths, int[,] owners)
        {
            int[,] newStrength = new int[n, m];
            int[,] nowStrength = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    nowStrength[i, j] = strengths[i, j];

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    if (res[i, j] >= 0 && res[i, j] < 4)
                    {
                        newStrength[newX[res[i, j], i], newY[res[i, j], j]] += nowStrength[i, j];
                        nowStrength[i, j] = 0;
                    }
                    if (res[i, j] == 4)
                    {
                        newStrength[i, j] += nowStrength[i, j];
                        nowStrength[i, j] = 0;
                    }
                }

            Queue<Location> queue = new Queue<Location>();
            int[,] distances = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (owners[i,j] == id && escapeMoves[i,j]!=4 && owners[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]] != id)
                        queue.Enqueue(new Location(i, j));

            int maxDist = 0;
            while (queue.Count > 0)
            {
                Location now = queue.Dequeue();
                for (int k = 0; k < 4; k++)
                {
                    Location newLoc = new Location(newX[k, now.x], newY[k, now.y]);
                    if (owners[newLoc.x,newLoc.y]!=id || escapeMoves[newLoc.x, newLoc.y] != oppositeDir[k])
                        continue;
                    distances[newLoc.x, newLoc.y] = distances[now.x, now.y] + 1;
                    maxDist = distances[newLoc.x, newLoc.y];
                    queue.Enqueue(newLoc);
                }
            }
            maxDist++;

            List<Location>[] lists = new List<Location>[maxDist];
            for (int i = 0; i < maxDist; i++)
                lists[i] = new List<Location>();

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (owners[i, j] == id && res[i, j] < 0)
                        lists[distances[i, j]].Add(new Location(i, j));

            for (int l = 0; l < maxDist; l++)
            {
                Location[] locs = lists[l].ToArray();
                int[] strengthsArray = new int[locs.Length];
                for (int i = 0; i < locs.Length; i++)
                    strengthsArray[i] = -strengths[locs[i].x, locs[i].y];
                Array.Sort(strengthsArray, locs);

                for (int ii = 0; ii < locs.Length; ii++)
                {
                    int i = locs[ii].x;
                    int j = locs[ii].y;
                    if (escapeMoves[i,j]!=4 && strengths[i, j] > map.contents[i, j].production * whenMoveLimit && nowStrength[i, j] + newStrength[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]] <= 255)
                    {
                        if (owners[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]] == 0 && strengths[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]] >= nowStrength[i, j])
                            continue;
                        if (nowStrength[i, j] + newStrength[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]] + nowStrength[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]] > 255 && nowStrength[i, j] <= nowStrength[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]])
                            continue;

                        res[i, j] = escapeMoves[i, j];
                        newStrength[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]] += nowStrength[i, j];
                        nowStrength[i, j] = 0;
                        if (newStrength[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]] + nowStrength[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]] > 255 && owners[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]] == id)
                        {
                            res[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]] = oppositeDir[escapeMoves[i, j]];
                            newStrength[i, j] += nowStrength[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]];
                            nowStrength[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]] = 0;
                        }
                    }
                }
            }
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (owners[i, j] == id && res[i, j] == -1)
                        res[i, j] = 4;
        }

        int[,] fight(WorldOrder worldOrder)
        {
            long time = DateTime.Now.ToFileTimeUtc();

            int[,] res = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    res[i, j] = -1;

            bool[,] myFields = Strategy.findMyFields(map, id);
            bool[,] hisFields = Strategy.findHisFields(map, id);
            bool[,] myImportant = Strategy.findMyImportant(map, myFields, hisFields);
            bool[,] hisImportant = Strategy.findMyImportant(map, hisFields, myFields);

            Strategy[] myStrategies = new Strategy[1] { new Strategy(map, myImportant, myFields) };

            int enemyStrategiesCount = 10;
            Strategy[] hisStrategies = new Strategy[enemyStrategiesCount];
            for (int i = 0; i < enemyStrategiesCount; i++)
            {
                hisStrategies[i] = new Strategy(map, hisImportant, hisFields);
                hisStrategies[i].attack(rand.NextDouble(), rand,2+rand.Next(10));
            }

            myStrategies[0].attack(1.0, rand,1);

            bool[,] forbidden = new bool[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    forbidden[i, j] = !worldOrder.canMoveTo(i, j, map.contents[i,j].strength);

            int ll = 0;
            double temperature = 3.0;
            while(DateTime.Now.ToFileTimeUtc()-time<5e6)
            {
                temperature *= 0.999;
                ll++;
                myStrategies[0].tryUpgrade(hisStrategies, rand,temperature, forbidden);
                //        hisStrategies[rand.Next(strategiesCount)].tryUpgrade(myStrategies, rand,temperature);
      //          break;
            }

   //         Console.WriteLine("lines: "+ll+" temp: "+temperature);

            Strategy myStrategy = Strategy.selectBest(myStrategies, hisStrategies);
            res = myStrategy.getMoves();

            int[,] strengths = new int[n, m];
            int[,] owners = new int[n, m];
            for(int i=0;i<n;i++)
                for(int j=0;j<m;j++)
                {
                    strengths[i, j] = map.contents[i, j].strength;
                    owners[i, j] = map.contents[i, j].owner;
                }

            List<Location> breakingPoints = new List<Location>();
            if (lastVacantCount > 5000)
            {
                Location newBreakingPoint = worldOrder.findBreakThroughLocation();
                if (newBreakingPoint != null)
                {
                    breakingPoints.Add(newBreakingPoint);
          //          Console.Error.WriteLine(newBreakingPoint.x + " " + newBreakingPoint.y);
                }
            }

            
            int[,] escapeMoves = findEscapeMoves(worldOrder,followedSolution.distanceMultiplier,strengths,owners,breakingPoints);

            int vacantCount = howManyVacant(escapeMoves, strengths, owners, followedSolution.whenMove);
            lastVacantCount = vacantCount;
     //       Console.Error.WriteLine(turn+" "+lastVacantCount);

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (escapeMoves[i,j]!=4 && lastEscapeMoves[newX[escapeMoves[i, j], i], newY[escapeMoves[i, j], j]]!=4 && escapeMoves[i, j] == oppositeDir[lastEscapeMoves[newX[escapeMoves[i, j],i],newY[escapeMoves[i, j],j]]])
                        escapeMoves[i, j] = 4;

            saveLastEscapeMoves(escapeMoves,owners);

            moveVacant(escapeMoves, res,followedSolution.whenMove,strengths,owners);

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (res[i, j] == -1 && map.contents[i, j].owner == id)
                            res[i, j] = 4;

            int[,] finishMoves = finish();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (finishMoves[i, j] >= 0)
                        res[i, j] = finishMoves[i, j];

            return res;
        }

        int[,] turnVisited;
        int[,] order;
        int[,] turnAvailable;

        int neededTurns(Location[] targets, int startProduction, int startStrength)
        {
            int production = startProduction;
            int strength = startStrength;
            int turn = 0;
            turnVisited[targets[0].x, targets[0].y] = 0;
            turnAvailable[targets[0].x, targets[0].y] = 0;
            
            for (int i = 1; i < targets.Length; i++)
            {
                int turnPossible = int.MaxValue;
                int bestDirection = -1;
                for (int k = 0; k < 4; k++)
                    if (turnAvailable[newX[k, targets[i].x], newY[k, targets[i].y]] + 2 < turnPossible)
                    {
                        turnPossible = turnAvailable[newX[k, targets[i].x], newY[k, targets[i].y]] + 2;
                        bestDirection = k;
                    }
                if (turnPossible < turn)
                    turnPossible = turn;
                    
                int mustWait = map.contents[targets[i].x, targets[i].y].strength - strength;
                if (mustWait >= 0)
                
                    mustWait = (mustWait + production - 1) / production;
                         if (mustWait < turnPossible - turn)
                             mustWait = turnPossible - turn;

                    turn += mustWait;
                

                if (mustWait > 0)
                {
                    strength += (mustWait - 1) * production *2/3;
            //        if (strength > (i + 4) * 60)
            //            strength = (i + 4) * 60;
                    strength += production *2/3;
                }

                strength -= map.contents[targets[i].x, targets[i].y].strength;
                production += map.contents[targets[i].x, targets[i].y].production;

                turnVisited[targets[i].x, targets[i].y] = turn;
                turnAvailable[targets[i].x, targets[i].y] = turn;
                turnAvailable[newX[bestDirection, targets[i].x], newY[bestDirection, targets[i].y]] += 2;
            }
            for (int i = 0; i < targets.Length; i++)
            {
                turnVisited[targets[i].x, targets[i].y] = int.MaxValue;
                turnAvailable[targets[i].x, targets[i].y] = int.MaxValue;
            }
            return turn;
        }

        void simpleChanges(Location[] targets)
        {
            order = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    order[i, j] = int.MaxValue;
            for (int i = 0; i < targets.Length; i++)
                order[targets[i].x, targets[i].y] = i;

            for (int i = 1; i < targets.Length - 1; i++)
            {
                bool update = false;
                for (int j = i + 1; j < targets.Length; j++)
                {
                    int a = j - 1;
                    int b = j;

                    double scoreA = (double)map.contents[targets[a].x, targets[a].y].production / (double)map.contents[targets[a].x, targets[a].y].strength;
                    double scoreB = (double)map.contents[targets[b].x, targets[b].y].production / (double)map.contents[targets[b].x, targets[b].y].strength;
                    if (scoreA >= scoreB)
                        continue;

                    bool validMove = false;
                    for (int k = 0; k < 4; k++)
                        if (order[newX[k, targets[b].x], newY[k, targets[b].y]] < a)
                            validMove = true;
                    if (!validMove)
                        continue;

                    for (int k = 0; k < 4; k++)
                        if (order[newX[k, targets[a].x], newY[k, targets[a].y]] < b && order[newX[k, targets[a].x], newY[k, targets[a].y]] > a)
                        {
                            bool hasOther = false;
                            for (int kk = 0; kk < 4; kk++)
                                if (kk != oppositeDir[k] && (order[newX[kk, newX[k, targets[a].x]], newY[kk, newY[k, targets[a].y]]] < order[newX[k, targets[a].x], newY[k, targets[a].y]] || (newX[kk, newX[k, targets[a].x]] == targets[b].x && newY[kk, newY[k, targets[a].y]] == targets[b].y)))
                                {
                                    hasOther = true;
                                    break;
                                }
                            if (!hasOther)
                            {
                                validMove = false;
                                break;
                            }
                        }
                    if (!validMove)
                        continue;
                    Location tt = targets[a];
                    targets[a] = targets[b];
                    targets[b] = tt;
                    order[targets[a].x, targets[a].y] = a;
                    order[targets[b].x, targets[b].y] = b;
                    update = true;
                }
                if (update)
                    i--;
            }
        }

        int dist(int x1, int x2, int size)
        {
            int res = x1 - x2;
            if (res < 0)
                res = -res;
            if (res > size / 2)
                res = size - res;
            return res;
        }

        int dist(int x1, int y1, int x2, int y2)
        {
            return dist(x1, x2, n) + dist(y1, y2, m);
        }

        int countLocationStrength(int x, int y, int distMultiplier)
        {
            return map.contents[x, y].production * 10000 / map.contents[x, y].strength * (distMultiplier-dist(x, y, me.x, me.y))/distMultiplier;
        }

        Location[] shortestFromMe(int x, int y)
        {
            List<Location> res = new List<Location>();
            Location now = new Location(x, y);
            while (now.x != me.x || now.y != me.y)
            {
                res.Add(now);
                int move = shortestMoves[now.x, now.y];
                now = new Location(newX[move, now.x], newY[move, now.y]);
            }
            res.Reverse();
            return res.ToArray();
        }

        Location[] findOrderFromRegions(int[] order, Location[][] regions)
        {
            List<Location> res = new List<Location>();
            bool[,] visited = new bool[n, m];
            bool[,] canAdd = new bool[n, m];
            visited[me.x, me.y] = true;
            res.Add(me);
            canAdd[me.x, me.y] = true;
            for (int k = 0; k < 4; k++)
                canAdd[newX[k, me.x], newY[k, me.y]] = true;

            for(int o=0;o<order.Length;o++)
            {
                Location[] shortest = shortestFromMe(regions[order[o]][0].x, regions[order[o]][0].y);
                for (int i = 0; i < shortest.Length; i++)
                    if (!visited[shortest[i].x, shortest[i].y])
                    {
                        visited[shortest[i].x, shortest[i].y] = true;
                        for (int k = 0; k < 4; k++)
                            canAdd[newX[k, shortest[i].x], newY[k, shortest[i].y]] = true;
                        res.Add(shortest[i]);
                    }

                Location[] region = regions[order[o]];
                bool[] added = new bool[region.Length];
                for (int i = 0; i < region.Length; i++)
                {
                    for (int j = 0; j < region.Length; j++)
                        if (!added[j] && canAdd[region[j].x, region[j].y])
                        {
                            added[j] = true;
                            if(!visited[region[j].x, region[j].y])
                            {
                                visited[region[j].x, region[j].y] = true;
                                for (int k = 0; k < 4; k++)
                                    canAdd[newX[k, region[j].x], newY[k, region[j].y]] = true;
                                res.Add(region[j]);
                            }
                            break;
                        }
                }
            }
            return res.ToArray();
        }

        int[] basicOrder(Location[][] regions)
        {
            bool[,] visited = new bool[n, m];
            int[] res = new int[regions.Length];
            bool[] used = new bool[regions.Length];

            int currentProduction = map.contents[me.x, me.y].production;
            int currentStrength = map.contents[me.x, me.y].strength;
            for (int loop = 0; loop < regions.Length; loop++)
            {
                int bestRegion = -1;
                double bestScore = 0.0;
                List<Location> bestRoute = null;
                for (int i = 0; i < regions.Length; i++)
                    if (!used[i])
                    {
                        Location[] path = shortestFromMe(regions[i][0].x, regions[i][0].y);
                        List<Location> finalRoute = new List<Location>();
                        finalRoute.Add(me);
                        foreach (Location loc in path)
                            if (!visited[loc.x, loc.y])
                                finalRoute.Add(loc);
                        foreach (Location loc in regions[i])
                            if (!visited[loc.x, loc.y])
                            {
                                bool add = true;
                                foreach (Location loc2 in finalRoute)
                                    if (loc.x == loc2.x && loc.y == loc2.y)
                                        add = false;
                                if (add)
                                    finalRoute.Add(loc);
                            }

                        int productionSum = 0;
                        foreach (Location loc in finalRoute)
                            productionSum += map.contents[loc.x, loc.y].production;
                        int turns = neededTurns(finalRoute.ToArray(),currentProduction,currentStrength);

                        double tempScore = Math.Pow((double)productionSum / (double)map.contents[me.x, me.y].production, 1.0 / (double)(turns + 1));

                        if (tempScore > bestScore)
                        {
                            bestScore = tempScore;
                            bestRegion = i;
                            bestRoute = finalRoute;
                        }
                    }
                res[loop] = bestRegion;
                foreach (Location loc in bestRoute)
                {
                    visited[loc.x, loc.y] = true;
                    if (loc != me)
                        currentProduction += map.contents[loc.x, loc.y].production;
                }
                currentStrength = 0;
            }
            return res;
        }
        
        Location[] findBestRegions(Location[][] regions)
        {
            Location[] bestRes = findOrderFromRegions(basicOrder(regions), regions);

            return bestRes;
        }

        Location[] findTargets(int acceptablePercent)
        {
            int enemiesCount = 0;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (map.contents[i, j].owner > 0)
                        enemiesCount++;

            List<Location> enemies = new List<Location>();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (map.contents[i, j].owner > 0 && map.contents[i, j].owner != id)
                        enemies.Add(new Location(i, j));

            List<Location> res = new List<Location>();
            bool[,] usable = new bool[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    int distMe = dist(i, j, me.x, me.y);
                    int nearerEnemySum = 0;
                    foreach (Location enemy in enemies)
                        if (dist(i, j, enemy.x, enemy.y) < distMe)
                            nearerEnemySum += distMe - dist(i, j, enemy.x, enemy.y);
                    if (nearerEnemySum*2<distMe)
                        usable[i, j] = true;
                }

            List<Location> importantLocations = new List<Location>();
            List<int> importantStrengths = new List<int>();
            int[,] regions = new int[n, m];
            Queue<Location> queue = new Queue<Location>();

            int distMultiplier = rand.Next((n + m + 1) / 2, 2 * (n + m));

            while (true)
            {
                Location bestLocation = null;
                int bestStrength = 0;
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                    {
                        if (dist(i, j, me.x, me.y) == 0 || !usable[i, j] || regions[i, j] > 0)
                            continue;
                        int tempStrength = countLocationStrength(i, j,distMultiplier);
                        if (tempStrength > bestStrength)
                        {
                            bestStrength = tempStrength;
                            bestLocation = new Location(i, j);
                        }
                    }
                if (bestLocation == null)
                    break;

                importantLocations.Add(bestLocation);
                importantStrengths.Add(bestStrength);
                queue.Enqueue(bestLocation);
                regions[bestLocation.x, bestLocation.y] = importantLocations.Count;

                while (queue.Count > 0)
                {
                    Location now = queue.Dequeue();
                    for (int k = 0; k < 4; k++)
                    {
                        int x = newX[k, now.x];
                        int y = newY[k, now.y];
                        if (!usable[x, y] || regions[x, y] > 0 || countLocationStrength(x, y,distMultiplier) * 100 < importantStrengths[regions[now.x, now.y] - 1] * acceptablePercent)
                            continue;
                        regions[x, y] = regions[now.x, now.y];
                        queue.Enqueue(new Location(x, y));
                    }
                }
               
            }


            int[] regionSizes = new int[importantLocations.Count+1];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    regionSizes[regions[i, j]]++;
            bool[] deletedRegions = new bool[importantLocations.Count+1];

            int smallestRegionSizeLimit = 10;
            while(true)
            {
                int smallestRegion = 1;
                int smallestRegionSize = int.MaxValue;
                for (int i = 1; i < regionSizes.Length; i++)
                    if (!deletedRegions[i] && regionSizes[i] < smallestRegionSize)
                    {
                        smallestRegionSize = regionSizes[i];
                        smallestRegion = i;
                    }
                if (smallestRegionSize >= smallestRegionSizeLimit)
                    break;

                queue.Clear();
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                        if (regions[i, j] != smallestRegion)
                            queue.Enqueue(new Location(i, j));

                while(queue.Count>0)
                {
                    Location now = queue.Dequeue();
                    for(int k=0;k<4;k++)
                    {
                        Location newLoc = new Location(newX[k, now.x], newY[k, now.y]);
                        if(regions[newLoc.x,newLoc.y]==smallestRegion)
                        {
                            regions[newLoc.x, newLoc.y] = regions[now.x,now.y];
                            regionSizes[regions[newLoc.x, newLoc.y]]++;
                            queue.Enqueue(newLoc);
                        }
                    }
                }
                deletedRegions[smallestRegion] = true;
                regionSizes[smallestRegion] = 0;
            }


            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    map.contents[i, j].id = regions[i, j];

            List<Location[]> regionLocations = new List<Location[]>();
            for (int l = 0; l < importantLocations.Count; l++)
            {
                Location[] toVisit = new Location[regionSizes[l+1]];
                int[] priorities = new int[regionSizes[l+1]];
                int count = 0;
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                        if (regions[i, j] == l + 1)
                        {
                            int ratio = 2600;
                            if (map.contents[i, j].production > 0)
                                ratio = 10 * map.contents[i, j].strength / map.contents[i, j].production;
                            toVisit[count] = new Location(i, j);
                            priorities[count] = ratio;
                            count++;
                        }

                Array.Sort(priorities, toVisit);
                List<Location> allLocations = new List<Location>();
                foreach (Location location in toVisit)
                    allLocations.Add(location);
                if (allLocations.Count > 0)
                    regionLocations.Add(allLocations.ToArray());
            }

            return findBestRegions(regionLocations.ToArray());
        }

        List<Move> findMovesForOne(Location target, int[,] strength, int[,] delay, int[,] mineSince, out int afterAttackStrength, int targetStrength, int targetProduction, out int finalScore, out int finalDelay)
        {
            int[,] move = new int[n, m];
            int[,] visitedAfter = new int[n, m];
            bool[,] canVisitTable = new bool[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    visitedAfter[i, j] = -1;
                    move[i, j] = -1;
                }

            int[,] myNeigh = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (mineSince[i, j] < int.MaxValue)
                        for (int k = 0; k < 4; k++)
                            if (mineSince[newX[k, i], newY[k, j]] < int.MaxValue)
                                myNeigh[i, j]++;

            int targetDelay = int.MaxValue;
            List<Location> canVisit = new List<Location>();
            for (int k = 0; k < 4; k++)
                if (mineSince[newX[k, target.x], newY[k, target.y]] < int.MaxValue)
                {
                    canVisit.Add(new Location(newX[k, target.x], newY[k, target.y]));
                    canVisitTable[newX[k, target.x], newY[k, target.y]] = true;
                    if (delay[newX[k, target.x], newY[k, target.y]] < targetDelay)
                        targetDelay = delay[newX[k, target.x], newY[k, target.y]];
                }
            visitedAfter[target.x, target.y] = 0;

            List<Location> alreadyVisited = new List<Location>();

            int bestDelay = 0;
            int bestScore = int.MaxValue;
            int bestLocationsCount = 0;
            int bestAfterAttackStrength = 0;

            int strengthAccumulated = 0;
            int productionAccumulated = 0;
            int minDelay = 0;
            while (canVisit.Count > 0 && productionAccumulated + targetProduction < bestScore)
            {
                Location now = canVisit[0];
                int index = 0;
                for (int i = 1; i < canVisit.Count; i++)
                    if (delay[now.x, now.y] > delay[canVisit[i].x, canVisit[i].y] || (delay[now.x, now.y] == delay[canVisit[i].x, canVisit[i].y] && myNeigh[now.x, now.y] < myNeigh[canVisit[i].x, canVisit[i].y]))
                    {
                        now = canVisit[i];
                        index = i;
                    }
                canVisit.RemoveAt(index);

                for (int k = 0; k < 4; k++)
                {
                    Location newLoc = new Location(newX[k, now.x], newY[k, now.y]);
                    if (!canVisitTable[newLoc.x, newLoc.y] && mineSince[newLoc.x, newLoc.y] < int.MaxValue)
                    {
                        canVisit.Add(newLoc);
                        canVisitTable[newLoc.x, newLoc.y] = true;
                    }
                }

                int bestDir = -1;
                int shortestTime = int.MaxValue;
                for (int k = 0; k < 4; k++)
                {
                    Location newLoc = new Location(newX[k, now.x], newY[k, now.y]);
                    if (((newLoc.x == target.x && newLoc.y == target.y) || mineSince[newLoc.x, newLoc.y] < int.MaxValue) && visitedAfter[newLoc.x, newLoc.y] >= 0 && visitedAfter[newLoc.x, newLoc.y] + 1 < shortestTime)
                    {
                        shortestTime = visitedAfter[newLoc.x, newLoc.y] + 1;
                        bestDir = k;
                    }
                }
                visitedAfter[now.x, now.y] = shortestTime;
                move[now.x, now.y] = bestDir;

                alreadyVisited.Add(now);
                productionAccumulated += map.contents[now.x, now.y].production;
                strengthAccumulated += strength[now.x, now.y];
                strengthAccumulated-=(delay[now.x, now.y] + visitedAfter[now.x, now.y]) * map.contents[now.x, now.y].production;

                if (minDelay < delay[now.x, now.y] + visitedAfter[now.x, now.y])
                    minDelay = delay[now.x, now.y] + visitedAfter[now.x, now.y];


                int currentStrength = strengthAccumulated + productionAccumulated * minDelay;

                int neededDelay = minDelay;
                if (currentStrength <= targetStrength)
                {
                    if (productionAccumulated > 0)
                        neededDelay += (targetStrength - currentStrength + productionAccumulated) / productionAccumulated;
                    else
                        continue;
                }
                int totalStrength = currentStrength + (neededDelay - minDelay) * productionAccumulated;
                int capLost = 0;
                if (totalStrength > 255)
                    capLost = totalStrength - 255;

                int tempScore = neededDelay * targetProduction  + productionAccumulated + capLost;
                if (tempScore < bestScore)
                {
                    bestScore = tempScore;
                    bestDelay = neededDelay;
                    bestLocationsCount = alreadyVisited.Count;
                    bestAfterAttackStrength = totalStrength - capLost - targetStrength;
                }
            }

            List<Move> res = new List<Move>();
            for (int o = 0; o < bestLocationsCount; o++)
            {
                int i = alreadyVisited[o].x;
                int j = alreadyVisited[o].y;
                {
                    for (int k = delay[i, j]; k < bestDelay - visitedAfter[i, j]; k++)
                        res.Add(new Move(i, j, 4, k));// res[k][i, j] = 4;

                    res.Add(new Move(i, j, move[i, j], bestDelay - visitedAfter[i, j]));
                //    res[bestDelay - visitedAfter[i, j]][i, j] = move[i, j];
                }
            }
            afterAttackStrength= bestAfterAttackStrength;
            finalDelay = bestDelay;
            finalScore = bestScore;
            return res;
        }

        bool ifNeighbors(Location a, Location b)
        {
            if (dist(a.x, a.y, b.x, b.y) == 1)
                return true;
            return false;
        }

        int dirFromTo(Location a, Location b)
        {
            for(int k=0;k<4;k++)
            {
                int x = newX[k, a.x];
                if (x != b.x)
                    continue;
                int y = newY[k, a.y];
                if (y == b.y)
                    return k;
            }
            return -1;
        }

        List<int[,]> findMoves(Location[] targets, int whenMove, int targetLoopLimit)
        {
            long time = DateTime.Now.ToFileTimeUtc();

            int[][,] res = new int[n*m*5][,];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = new int[n, m];
                for (int j = 0; j < n; j++)
                    for (int k = 0; k < m; k++)
                        res[i][j, k] = -1;
            }
            int[,] strength = new int[n, m];
            int[,] delay = new int[n, m];

            int[,] mineSince = new int[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    strength[i, j] = map.contents[i, j].strength;
                    if (map.contents[i, j].owner == id)
                        mineSince[i, j] = 0;
                    else
                        mineSince[i, j] = int.MaxValue;
                }

            int previousFinalScore = 0;
            int previousFinalDelay = 0;
            List<Move> previousMoves = null;
            int[,] previousStrength = new int[n, m];
            int[,] previousDelay = new int[n, m];
            int previousTargetStrength = 0;
            int previousTargetProduction = 0;
            List<Location> previousTargetList = null;

            for(int loop=0;loop< targetLoopLimit; loop++)
            { 
                int bestDelay = int.MaxValue;
                int l = -1;

                    for (int ll = 0; ll < targets.Length; ll++)
                        if (mineSince[targets[ll].x, targets[ll].y] == int.MaxValue)
                        {
                            int nowDelay = int.MaxValue / 2;
                            for (int k = 0; k < 4; k++)
                            {
                                Location newLoc = new Location(newX[k, targets[ll].x], newY[k, targets[ll].y]);
                                if (mineSince[newLoc.x, newLoc.y] < int.MaxValue && delay[newLoc.x, newLoc.y] < nowDelay)
                                    nowDelay = delay[newLoc.x, newLoc.y];
                            }
                            if (nowDelay + (ll - l) * 2 < bestDelay)
                            {
                                bestDelay = nowDelay;
                                l = ll;
                            }
                        }
                if (bestDelay == int.MaxValue)
                    break;

                List<Location> targetList = new List<Location>();
                targetList.Add(targets[l]);
                int targetStrength = strength[targets[l].x, targets[l].y];
                int targetProduction = map.contents[targets[l].x, targets[l].y].production;
                int afterAttackStrength = 0;
                int finalScore = 0;
                int finalDelay = 0;
                List<Move> moves = findMovesForOne(targets[l], strength, delay, mineSince, out afterAttackStrength, targetStrength, targetProduction, out finalScore, out finalDelay);

                if (previousTargetList != null && ifNeighbors(targets[l], previousTargetList[previousTargetList.Count - 1]) && targetStrength + previousTargetStrength < 255)
                {
                    foreach (Move move in previousMoves)
                        res[move.turn][move.xWhere, move.yWhere] = -1;
                    for (int i = 0; i < previousTargetList.Count; i++)
                    {
                        mineSince[previousTargetList[i].x, previousTargetList[i].y] = int.MaxValue;
                        res[previousFinalDelay + i][previousTargetList[i].x, previousTargetList[i].y] = -1;
                    }

                    int targetStrengthDouble = targetStrength + previousTargetStrength;
                    int targetProductionDouble = targetProduction + previousTargetProduction;

                    int afterAttackStrengthDouble = 0;
                    int finalScoreDouble = 0;
                    int finalDelayDouble = 0;
                    List<Location> targetListDouble = new List<Location>();
                    for (int i = 0; i < previousTargetList.Count; i++)
                        targetListDouble.Add(previousTargetList[i]);
                    targetListDouble.Add(targets[l]);

                    List<Move> movesDouble = findMovesForOne(targetListDouble[0], previousStrength, previousDelay, mineSince, out afterAttackStrengthDouble, targetStrengthDouble, targetProductionDouble, out finalScoreDouble, out finalDelayDouble);

                    int independentLoss = previousFinalScore + finalScore;
                    int relatedLoss = finalScoreDouble + targetProduction;

                    if (relatedLoss < independentLoss)
                    {
                        moves = movesDouble;
                        finalDelay = finalDelayDouble;
                        finalScore = finalScoreDouble + targetProduction;
                        targetStrength = targetStrengthDouble;
                        targetProduction = targetProductionDouble;
                        targetList = targetListDouble;
                        afterAttackStrength = afterAttackStrengthDouble;
                        for (int i = 0; i < n; i++)
                            for (int j = 0; j < m; j++)
                            {
                                delay[i, j] = previousDelay[i, j];
                                strength[i, j] = previousStrength[i, j];
                            }
                    }
                    else
                    {
                        foreach (Move move in previousMoves)
                            res[move.turn][move.xWhere, move.yWhere] = move.direction;
                        for (int i = 0; i < previousTargetList.Count; i++)
                        {
                            mineSince[previousTargetList[i].x, previousTargetList[i].y] = previousFinalDelay + 1 + i;
                            //       mineSince[previousTargetList[i].x, previousTargetList[i].y] = previousDelay[previousTargetList[i].x, previousTargetList[i].y];
                            if (i < previousTargetList.Count - 1)
                                res[previousFinalDelay + i][previousTargetList[i].x, previousTargetList[i].y] = dirFromTo(previousTargetList[i], previousTargetList[i + 1]);
                            else
                                res[previousFinalDelay + i][previousTargetList[i].x, previousTargetList[i].y] = 4;
                        }
                    }
                }

                previousMoves = moves;
                previousFinalDelay = finalDelay;
                previousFinalScore = finalScore;
                previousTargetStrength = targetStrength;
                previousTargetProduction = targetProduction;
                previousTargetList = targetList;
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                    {
                        previousDelay[i, j] = delay[i, j];
                        previousStrength[i, j] = strength[i, j];
                    }

                foreach (Move move in moves)
                {
                    res[move.turn][move.xWhere, move.yWhere] = move.direction;                    
                    delay[move.xWhere, move.yWhere] = move.turn + 1;
                    strength[move.xWhere, move.yWhere] = 0;
                }
                
                Location lastTarget = targetList[targetList.Count - 1];
                strength[lastTarget.x, lastTarget.y] = afterAttackStrength + map.contents[lastTarget.x, lastTarget.y].production;
                for (int i = 0; i < targetList.Count; i++)
                {
                    delay[targetList[i].x, targetList[i].y] = finalDelay + 1 + i;
                    mineSince[targetList[i].x, targetList[i].y] = delay[targetList[i].x, targetList[i].y];
                    if (i < targetList.Count - 1)
                    {
                        res[finalDelay + i][targetList[i].x, targetList[i].y] = dirFromTo(targetList[i], targetList[i + 1]);
                        strength[targetList[i].x, targetList[i].y] = 0;
                    }
                    else
                        res[finalDelay + i][targetList[i].x, targetList[i].y] = 4;
                }
            }

            int maxTurn = int.MaxValue;
            for (int k = 0; k < res.Length; k++)
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                        if (mineSince[i,j]<k && res[k][i, j] < 0)
                        {
                            maxTurn = k + whenMove;
                            j = m;
                            i = n;
                            k = res.Length;
                        }

            List<int[,]> newRes = new List<int[,]>();
            for (int k = 0; k < maxTurn; k++)
                newRes.Add(res[k]);
           return newRes;            
        }

    }
}
