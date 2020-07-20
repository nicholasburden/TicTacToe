using System;

namespace TicTacToe.Models.Game
{
    public class Game
    {
        private readonly string[,] _board;
        public Player WhoseTurn { get; set; }
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }

        public string Id { get; set; }
        public Game(Player player1, Player player2)
        {
            Player1 = player1;
            Player2 = player2;
            _board = new string[3,3];
            for(int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    _board[i, j] = "";
                }
            }
            Id = Guid.NewGuid().ToString("d");
            WhoseTurn = Player1;

        }
        public GameStatus MakeMove(Player player, int x, int y)
        {
            if (IsValid(x,y))
            {
                _board[x, y] = player.Id == Player1.Id ? "X" : "O";
                WhoseTurn = WhoseTurn.Equals(Player1) ? Player2 : Player1;
                return CheckWin();
            }
            return GameStatus.InvalidMove;
        }
        public bool IsValid(int x, int y)
        {
            return x >= 0 && x < 3 && y >= 0 && y < 3 && _board[x,y].Equals("");
        }

        private GameStatus CheckWin()
        {
           

            //Check diagonals
            if(_board[0,0] == _board[1,1] && _board[1, 1] == _board[2, 2] && !_board[0, 0].Equals(""))
            {
                return GameStatus.Win;
            }
            if (_board[0, 2] == _board[1, 1] && _board[1, 1] == _board[2, 0] && !_board[0, 2].Equals(""))
            {
                return GameStatus.Win;
            }

            //Check each row
            for (int i = 0; i < 3; i++)
            {
                if (_board[i, 0] == _board[i, 1] && _board[i, 1] == _board[i, 2] && !_board[i, 0].Equals(""))
                {
                    return GameStatus.Win;
                }
            }

            //Check each col
            for (int j = 0; j < 3; j++)
            {
                if (_board[0, j] == _board[1, j] && _board[1, j] == _board[2, j] && !_board[0, j].Equals(""))
                {
                    return GameStatus.Win;
                }
            }
            //Check tie
            if (!SpacesLeft()) return GameStatus.Tie;
            return GameStatus.NotFinished;
        }
        private bool SpacesLeft()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (_board[i, j].Equals(""))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public enum GameStatus
    {
        Win,
        NotFinished,
        InvalidMove,
        Tie
    }
}
