var connection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();
const cellElements = document.querySelectorAll('[data-cell]')
const board = document.getElementById('board')
const helpBlock = document.getElementById("helpBlock")
const findGame = document.getElementById("findGame")
var statusMessage;
var playerId;
var whoseTurn;
var playerOne;
var playerTwo;
const X_CLASS = 'x'
const CIRCLE_CLASS = 'circle'
board.style.display = "none";
findGame.disabled = true;

connection.start().then(function () {
    findGame.disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

findGame.addEventListener("click", function (event) {
    var user = document.getElementById("usernameInput").value;
    connection.invoke("FindGame", user).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});


connection.on("PlayerJoined", function (player) {
    playerId = player.id;
    findGame.disabled = true;
    helpBlock.innerHTML = "Waiting for opponent";
});

connection.on("NewGame", function (game) {
    whoseTurn = game.whoseTurn
    playerOne = game.player1;
    playerTwo = game.player2;
    statusMessage = "You are playing against " + getOpponent(game).name + "<br />"
    helpBlock.innerHTML = statusMessage
    var turnMessage = whoseTurn.id == playerId ? "Your turn" : whoseTurn.name + "'s turn"
    helpBlock.innerHTML = statusMessage + turnMessage
    board.style.display = "grid"
    startGame();
});



connection.on("PiecePlaced", function (row, col, isPlayerOne) {
    var id = row.toString() + col.toString()
    var c = isPlayerOne ? X_CLASS : CIRCLE_CLASS
    placeMark(document.getElementById(id), c)
    setBoardHoverClass()  
});

connection.on("UpdateTurn", function () {
    whoseTurn = whoseTurn.id == playerOne.id ? playerTwo : playerOne
    var turnMessage = whoseTurn.id == playerId ? "Your turn" : whoseTurn.name + "'s turn"
    helpBlock.innerHTML = statusMessage + turnMessage
    setBoardHoverClass()
});

connection.on("Winner", function (player) {
    finishGame()
    if (player.id == playerId) {
        helpBlock.innerHTML = "Congratulations! <br /> You Won!"
    }
    else {
        helpBlock.innerHTML = "Unlucky! <br /> " + player.name + " won!"
    }
    
});

connection.on("Tie", function (player) {
    finishGame()
    helpBlock.innerHTML = "Nearly! <br /> It's a tie!"
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
}

function setBoardHoverClass() {
    board.classList.remove(X_CLASS)
    board.classList.remove(CIRCLE_CLASS)
    if (whoseTurn.id == playerId) {
        cellElements.forEach(cell => {
            cell.addEventListener('click', handleClick)
        })
        if (xTurn()) {
            board.classList.add(X_CLASS)
        } else {
            board.classList.add(CIRCLE_CLASS)
        }
    }
    else {
        cellElements.forEach(cell => {
            cell.removeEventListener('click', handleClick)
        })
    }
}

function placeMark(cell, currentClass) {
    cell.classList.add(currentClass)
}

function xTurn() {
    return whoseTurn.id == playerOne.id;
}

function getRowCol(id) {
    return [parseInt(id[0]), parseInt(id[1])]
}

function finishGame() {
    cellElements.forEach(cell => {
        cell.removeEventListener('click', handleClick)
    })
    findGame.disabled = false
}
