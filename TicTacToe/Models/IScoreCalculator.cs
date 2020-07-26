using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TicTacToe.Models
{
    public interface IScoreCalculator
    {
        public float CalculateScore(float mine, float theirs);
    }
}
