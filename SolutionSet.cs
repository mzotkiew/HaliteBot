using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    class SolutionSet
    {
        int size;
        Solution[] solutions;
        int[,] results;
        int[,] conflictTime;

        public SolutionSet(int size)
        {
            this.size = size;
            results = new int[size, size];
            conflictTime= new int[size, size];
            solutions = new Solution[size];
        }

        int[] getSumScore()
        {
            int[] sumScore = new int[solutions.Length];
            for (int i = 0; i < solutions.Length; i++)
                if (solutions[i] != null)
                    for (int j = 0; j < solutions.Length; j++)
                        if (i != j && solutions[j] != null)
                        {
                            sumScore[i] += results[i, j];
                            if (results[i, j] > 0)
                                sumScore[i] += 100000;
                            if (results[i, j] < 0)
                                sumScore[i] -= 100000;
                        }

            return sumScore;
        }

        public int getAverageConflictTime()
        {
            int res = 0;
            int count = 0;
            for (int i = 0; i < size; i++)
                if (solutions[i] != null)
                    for (int j = i + 1; j < size; j++)
                        if (solutions[j] != null)
                        {
                            count++;
                            res += conflictTime[i, j];
                        }
            if (count == 0)
                return 120;
            return res / count;
        }

        public void addSolution(Solution solution)
        {
            for(int i=0;i<size;i++)
                if(solutions[i]==null)
                {
                    solutions[i] = solution;
                    for(int j=0;j<size;j++)
                        if(i!=j && solutions[j]!=null)
                        {
                            int time = 0;
                            int result = solution.compare(solutions[j], out time);
                            results[i, j] = result;
                            results[j, i] = -result;
                            conflictTime[i, j] = conflictTime[j, i] = time;
                        }
                    break;
                }

            bool allOccupied = true;
            foreach (Solution sol in solutions)
                if (sol == null)
                    allOccupied = false;
            if (!allOccupied)
                return;

            int[] sumScore = getSumScore();

            int worst = 0;
            for (int i = 1; i < solutions.Length; i++)
                if (sumScore[i] < sumScore[worst])
                    worst = i;
            solutions[worst] = null;
        }

        public Solution getBest()
        {
            int[] sumScore = getSumScore();

            int best = 0;
            for (int i = 1; i < sumScore.Length; i++)
                if (solutions[best]==null || (solutions[i]!=null && sumScore[i] > sumScore[best]))
                    best = i;
            return solutions[best];
        }
    }
}
