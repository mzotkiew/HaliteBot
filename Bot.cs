using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    public abstract class Bot
    {
        public abstract void getInit(int id, Map map);
        public abstract string sendInit();
        public abstract void getFrame(Map map);
        public abstract int[,] sendFrame();

        public List<int[,]> movesTypes;
    }
}
