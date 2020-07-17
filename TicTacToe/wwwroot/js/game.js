var connection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();
var playerId;
document.getElementById("findGame").disabled = true;


connection.start().then(function () {
    document.getElementById("findGame").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("findGame").addEventListener("click", function (event) {
    var user = document.getElementById("username").value;
    connection.invoke("FindGame", user).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});


connection.on("PlayerJoined", function (player) {
    playerId = player.id;
});

connection.on("NewGame", function (game) {
    var opponent = getOpponent(game);
    var name = opponent.name;
    alert("You are playing against " + name);
});

function getOpponent(game) {
    if (playerId == game.player1.id) {
        return game.player2;
    } else {
        return game.player1;
    }
};