using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite
{
    public class Move
    {
        public readonly int xWhere;
        public readonly int yWhere;

        public readonly int direction;
        public readonly int turn;

        public Move(int xWhere, int yWhere, int direction, int turn)
        {
            this.xWhere = xWhere;
            this.yWhere = yWhere;
            this.direction = direction;
            this.turn = turn;
        }
    }
}
