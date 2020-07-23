using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TicTacToe.Models
{
    public class User
    {
        public User(string username, int elo)
        {
            Username = username;
            Elo = elo;
        }
        [Key]
        public string Username{ get; set; }
        public int Elo { get; set; }
    }
}
