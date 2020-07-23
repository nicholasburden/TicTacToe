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

        public async Task ChangeElo(string winner, string loser)
        {
            User winnerFromDb = await _dbContext.FindAsync<User>(winner);
            User loserFromDb = await _dbContext.FindAsync<User>(loser);
            winnerFromDb.Elo += 1;
            loserFromDb.Elo -= 1;
            _dbContext.Update(winnerFromDb);
            _dbContext.Update(loserFromDb);
            await _dbContext.SaveChangesAsync();
        }

        private async Task AddUserToDb(string username)
        {
            User existingUser = await _dbContext.FindAsync<User>(username);
            if (existingUser == null){
                _dbContext.Add(new User(username, 1600));
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
