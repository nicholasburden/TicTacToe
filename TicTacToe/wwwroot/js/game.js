var connection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();
var playerId;
const cellElements = document.querySelectorAll('[data-cell]')
const board = document.getElementById('board')
var gameGlobal;

const X_CLASS = 'x'
const CIRCLE_CLASS = 'circle'

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
    document.getElementById("findGame").disabled = true;
    document.getElementById("waiting").innerHTML = "Waiting for opponent";
});

connection.on("NewGame", function (game) {
    gameGlobal = game;
    document.getElementById("register").style.display = "none";
    document.getElementById("playingAgainst").innerHTML = "You are playing against " + getOpponent(game).name;;
    startGame();
});

connection.on("PiecePlaced", function (row, col, isPlayerOne) {
    var id = row.toString() + col.toString()
    var c = isPlayerOne ? X_CLASS : CIRCLE_CLASS
    placeMark(document.getElementById(id), c)
});

function getOpponent(game) {
    if (playerId == game.player1.id) {
        return game.player2;
    } else {
        return game.player1;
    }
};


function startGame() {
    cellElements.forEach(cell => {
        cell.classList.remove(X_CLASS)
        cell.classList.remove(CIRCLE_CLASS)
        cell.removeEventListener('click', handleClick)
        cell.addEventListener('click', handleClick, { once: true })
    })
    setBoardHoverClass()
    winningMessageElement.classList.remove('show')
}

function handleClick(e) {
    const cell = e.target
    var rowCol = getRowCol(cell.id)
    connection.invoke("PlacePiece", rowCol[0], rowCol[1])
    placeMark(cell, currentClass)
    setBoardHoverClass()   
}

function setBoardHoverClass() {
    board.classList.remove(X_CLASS)
    board.classList.remove(CIRCLE_CLASS)
    if (gameGlobal.whoseTurn.id == playerId) {
        if (circleTurn()) {
            board.classList.add(CIRCLE_CLASS)
        } else {
            board.classList.add(X_CLASS)
        }
    }
}

function placeMark(cell, currentClass) {
    cell.classList.add(currentClass)
}

function circleTurn() {
    return gameGlobal.whoseTurn.id == gameGlobal.player2.id
}

function getRowCol(id) {
    return [parseInt(id[0]), parseInt(id[1])]
}
