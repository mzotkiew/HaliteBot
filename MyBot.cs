using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite;

    class MyBot
    {
        public static void Main(string[] args)
        {
            Bot bot = new ThirdBot();
            int id;
            int width, height;
            Map map;
           


            id = int.Parse(Console.ReadLine());
            string[] temp = Console.ReadLine().Split();
            width = int.Parse(temp[0]);
            height = int.Parse(temp[1]);

            string[] production = Console.ReadLine().Trim().Split();

            string[] state = Console.ReadLine().Trim().Split();


            map = new Map(production, state, width, height);

            bot.getInit(id, map);
            Console.WriteLine(bot.sendInit());

            while (true)
            {
                state = Console.ReadLine().Trim().Split();
                map = new Map(production, state, width, height);

                bot.getFrame(map);
                Console.WriteLine(makeAnswer(bot.sendFrame()));
            }

        }

        static string makeAnswer(int[,] answer)
        {
            string res = "";
            for (int i = 0; i < answer.GetLength(0); i++)
                for (int j = 0; j < answer.GetLength(1); j++)
                {
                    if (answer[i, j] == 4)
                        res += i.ToString() + " " + j.ToString() + " 0 ";
                    if (answer[i, j] == 0)
                        res += i.ToString() + " " + j.ToString() + " 1 ";
                    if (answer[i, j] == 1)
                        res += i.ToString() + " " + j.ToString() + " 2 ";
                    if (answer[i, j] == 2)
                        res += i.ToString() + " " + j.ToString() + " 3 ";
                    if (answer[i, j] == 3)
                        res += i.ToString() + " " + j.ToString() + " 4 ";
                }
            return res.TrimEnd();
        }
    }
