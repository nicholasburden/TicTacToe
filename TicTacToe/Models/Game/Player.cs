using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TicTacToe.Models.Game
{
    public class Player
    {
        public string Name { get; set; }
        public string Mark { get; set; }

        public string Id { get; set; }

        public string GameId { get; set; }

        public Player(string name, string id)
        {
            Name = name;
            Id = id;
        }
    }
}
