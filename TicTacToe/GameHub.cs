using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using TicTacToe.Migrations;
using TicTacToe.Models;
using TicTacToe.Models.Game;

namespace TicTacToe
{
    public class GameHub : Hub
    {
        private const int ELO_ADJUSTMENT_RATE = 30;
        private readonly ApplicationDbContext _dbContext;
        public GameHub(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task FindGame(string username)
        {
            Player joiningPlayer =
                GameState.CreatePlayer(username, Context.ConnectionId);
            await Clients.Caller.SendAsync("PlayerJoined", joiningPlayer);

            await AddUserToDb(username);
            // Find any pending games if any
            Player opponent = GameState.GetWaitingOpponent();
            if (opponent == null)
            {
                // No waiting players so enter the waiting pool
                GameState.AddToWaitingPool(joiningPlayer);
                await Clients.Caller.SendAsync("WaitingList");
            }
            else
            {
                // An opponent was found so join a new game and start the game
                // Opponent is first player since they were waiting first
                Game newGame = await CreateGame(opponent, joiningPlayer);
                await Clients.Group(newGame.Id).SendAsync("NewGame", newGame);
            }
        }

        public async Task PlacePiece(int row, int col)
        {
            Player playerMakingTurn = GameState.GetPlayer(playerId: Context.ConnectionId);
            Player opponent;
            Game game = GameState.GetGame(playerMakingTurn, out opponent);
            GameStatus gameStatus = game.MakeMove(playerMakingTurn, row, col);
            await Clients.Group(game.Id).SendAsync("PiecePlaced", row, col, playerMakingTurn.Id == game.Player1.Id);
            if (gameStatus == GameStatus.Win)
            {
                await Clients.Group(game.Id).SendAsync("Winner", playerMakingTurn);
                GameState.RemoveGame(game.Id);
            }
            else if (gameStatus == GameStatus.Tie)
            {
                await Clients.Group(game.Id).SendAsync("Tie");
                GameState.RemoveGame(game.Id);
            }
            else
            {
                await Clients.Group(game.Id).SendAsync("UpdateTurn");
            }
        }

      public async Task<Game> CreateGame(Player firstPlayer, Player secondPlayer)
        {
            Game game = new Game(firstPlayer, secondPlayer);
            firstPlayer.GameId = game.Id;
            secondPlayer.GameId = game.Id;
            GameState.AddGame(game);
            await Groups.AddToGroupAsync(firstPlayer.Id, groupName: game.Id);
            await Groups.AddToGroupAsync(secondPlayer.Id, groupName: game.Id);
            return game;
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Player leavingPlayer = GameState.GetPlayer(playerId: Context.ConnectionId);

            // Only handle cases where user was a player in a game or waiting for an opponent
            if (leavingPlayer != null)
            {
                Player opponent;
                Game ongoingGame = GameState.GetGame(leavingPlayer, out opponent);
                if (ongoingGame != null)
                {
                    await Clients.Group(ongoingGame.Id).SendAsync("OpponentLeft");
                    GameState.RemoveGame(ongoingGame.Id);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task ChangeElo(string me, string them, bool iWon, bool tie)
        {
            User meFromDb = await _dbContext.FindAsync<User>(me);
            User themFromDb = await _dbContext.FindAsync<User>(them);
            int winnerElo = iWon ? meFromDb.Elo : themFromDb.Elo;
            int loserElo = iWon ? themFromDb.Elo : meFromDb.Elo;
            (int, int) newElos = CalculateNewElos(winnerElo, loserElo, tie);
            var myNewElo = iWon ? newElos.Item1 : newElos.Item2;
            var increase = myNewElo - meFromDb.Elo;
            meFromDb.Elo = myNewElo;
            _dbContext.Update(meFromDb);
            await _dbContext.SaveChangesAsync();
            await Clients.Caller.SendAsync("UpdateElo", myNewElo, increase);
        }

        private async Task AddUserToDb(string username)
        {
            User existingUser = await _dbContext.FindAsync<User>(username);
            if (existingUser == null){
                _dbContext.Add(new User(username, 1600));
                await _dbContext.SaveChangesAsync();
            }
        }

        private (int, int) CalculateNewElos(int winner, int loser, bool tie)
        {
            var actual1 = tie ? 0.5 : 1;
            var actual2 = tie ? 0.5 : 0;
            double p1 = (1.0 / (1.0 + Math.Pow(10, ((loser - winner) / 400))));
            double p2 = (1.0 / (1.0 + Math.Pow(10, ((winner - loser) / 400))));
            int newWinner = (int)(winner + (ELO_ADJUSTMENT_RATE * (actual1 - p1)));
            int newLoser = (int)(loser + (ELO_ADJUSTMENT_RATE * (actual2 - p2)));
            return (newWinner, newLoser);
        }
    }
}
