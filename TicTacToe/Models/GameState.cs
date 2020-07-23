using System;
using System.Collections.Concurrent;
using System.Linq;
using TicTacToe.Models.Game;

namespace TicTacToe.Models
{
    public static class GameState
    {
        private static readonly ConcurrentDictionary<string, Player> _players =
          new ConcurrentDictionary<string, Player>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A reference to all games. Key is the group name of the game.
        /// Note that this collection uses a concurrent dictionary to handle multiple threads.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Game.Game> _games =
            new ConcurrentDictionary<string, Game.Game>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A queue of players that are waiting for an opponent.
        /// </summary>
        private static readonly ConcurrentQueue<Player> _waitingPlayers =
            new ConcurrentQueue<Player>();

        public static Player CreatePlayer(string username, string connectionId)
        {
            var player = new Player(username, connectionId);
            _players[connectionId] = player;
            return player;
        }

        public static Player GetPlayer(string playerId)
        {
            Player foundPlayer;
            if (!_players.TryGetValue(playerId, out foundPlayer))
            {
                return null;
            }
            return foundPlayer;
        }

        public static Game.Game GetGame(Player player, out Player opponent)
        {
            opponent = null;
            Game.Game foundGame = _games.Values.FirstOrDefault(g => g.Id == player.GameId);
            if (foundGame == null) return null;
            opponent = (player.Id == foundGame.Player1.Id) ?
                foundGame.Player2 :
                foundGame.Player1;
            return foundGame;
        }

        public static Player GetWaitingOpponent()
        {
            Player foundPlayer;
            if (!_waitingPlayers.TryDequeue(out foundPlayer))
            {
                return null;
            }

            return foundPlayer;

        }
        public static void RemoveGame(string gameId)
        {
            // Remove the game
            Game.Game foundGame;
            if (!_games.TryRemove(gameId, out foundGame))
            {
                throw new InvalidOperationException("Game not found.");
            }

            // Remove the players, best effort
            Player foundPlayer;
            _players.TryRemove(foundGame.Player1.Id, out foundPlayer);
            _players.TryRemove(foundGame.Player2.Id, out foundPlayer);
        }

        public static void AddToWaitingPool(Player player)
        {
            _waitingPlayers.Enqueue(player);
        }
        public static bool IsUsernameTaken(string username)
        {
            return _players.Values.FirstOrDefault(player => player.Name.Equals(username, StringComparison.InvariantCultureIgnoreCase)) != null;
        }

        public static void AddGame(Game.Game game)
        {
            _games[game.Id] = game;
        }
    }
}
