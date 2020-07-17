using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using TicTacToe.Models.Game;

namespace TicTacToe
{
    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, Player> _players =
            new ConcurrentDictionary<string, Player>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A reference to all games. Key is the group name of the game.
        /// Note that this collection uses a concurrent dictionary to handle multiple threads.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Game> _games =
            new ConcurrentDictionary<string, Game>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A queue of players that are waiting for an opponent.
        /// </summary>
        private static readonly ConcurrentQueue<Player> _waitingPlayers =
            new ConcurrentQueue<Player>();
        public async Task FindGame(string username)
        {
            Player joiningPlayer =
                CreatePlayer(username, Context.ConnectionId);
            await Clients.Caller.SendAsync("PlayerJoined", joiningPlayer);
            // Find any pending games if any
            Player opponent = GetWaitingOpponent();
            if (opponent == null)
            {
                // No waiting players so enter the waiting pool
                AddToWaitingPool(joiningPlayer);
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
            Player playerMakingTurn = GetPlayer(playerId: Context.ConnectionId);
            Player opponent;
            Game game = GetGame(playerMakingTurn, out opponent);
            if (game == null || !game.WhoseTurn.Equals(playerMakingTurn))
            {
                Clients.Caller.SendAsync("NotPlayersTurn");
            }
            GameStatus gameStatus = game.MakeMove(playerMakingTurn, row, col);
            if(gameStatus == GameStatus.InvalidMove)
            {
                Clients.Caller.SendAsync("NotValidMove");
                return;
            }
            Clients.Group(game.Id).SendAsync("PiecePlaced", row, col, playerMakingTurn.Mark);
            if (gameStatus == GameStatus.Win)
            {
                Clients.Group(game.Id).SendAsync("Winner", playerMakingTurn);
                RemoveGame(game.Id);
            }
            else if (gameStatus == GameStatus.Tie)
            {
                Clients.Group(game.Id).SendAsync("Tie");
                RemoveGame(game.Id);
            }
            else
            {
                Clients.Group(game.Id).SendAsync("UpdateTurn", game);
            }
        }
        private Player CreatePlayer(string username, string connectionId)
        {
            var player = new Player(username, connectionId);
            _players[connectionId] = player;
            return player;
        }

        private Player GetPlayer(string playerId)
        {
            Player foundPlayer;
            if (!_players.TryGetValue(playerId, out foundPlayer))
            {
                return null;
            }
            return foundPlayer;
        }

        private Game GetGame(Player player, out Player opponent)
        {
            opponent = null;
            Game foundGame = _games.Values.FirstOrDefault(g => g.Id == player.GameId);
            if (foundGame == null) return null;
            opponent = (player.Id == foundGame.Player1.Id) ?
                foundGame.Player2 :
                foundGame.Player1;
            return foundGame;
        }

        private Player GetWaitingOpponent()
        {
            Player foundPlayer;
            if (!_waitingPlayers.TryDequeue(out foundPlayer))
            {
                return null;
            }

            return foundPlayer;

        }

        public async Task RemoveGame(string gameId)
        {
            // Remove the game
            Game foundGame;
            if (_games.TryRemove(gameId, out foundGame))
            {
                throw new InvalidOperationException("Game not found.");
            }

            // Remove the players, best effort
            Player foundPlayer;
            _players.TryRemove(foundGame.Player1.Id, out foundPlayer);
            _players.TryRemove(foundGame.Player2.Id, out foundPlayer);
        }

        public async Task AddToWaitingPool(Player player)
        {
            _waitingPlayers.Enqueue(player);
        }
        public bool IsUsernameTaken(string username)
        {
            return _players.Values.FirstOrDefault(player => player.Name.Equals(username, StringComparison.InvariantCultureIgnoreCase)) != null;
        }

        public async Task<Game> CreateGame(Player firstPlayer, Player secondPlayer)
        {
            Game game = new Game(firstPlayer, secondPlayer);
            _games[game.Id] = game;
            await Groups.AddToGroupAsync(firstPlayer.Id, groupName: game.Id);
            await Groups.AddToGroupAsync(secondPlayer.Id, groupName: game.Id);
            return game;
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Player leavingPlayer = GetPlayer(playerId: Context.ConnectionId);

            // Only handle cases where user was a player in a game or waiting for an opponent
            if (leavingPlayer != null)
            {
                Player opponent;
                Game ongoingGame = GetGame(leavingPlayer, out opponent);
                if (ongoingGame != null)
                {
                    await Clients.Group(ongoingGame.Id).SendAsync("OpponentLeft");
                    RemoveGame(ongoingGame.Id);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
