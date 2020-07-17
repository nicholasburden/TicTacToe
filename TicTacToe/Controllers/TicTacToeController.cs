using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Mvc;

namespace TicTacToe.Controllers
{
    public class TicTacToeController : Controller
    {
        public IActionResult Play()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Register(string username)
        {
            return View();
        }
    }
}
