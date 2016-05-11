using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WakeMe.Interfaces;
using WakeMe.Models;

namespace WakeMe.Controllers
{
    public class HomeController : Controller
    {
        private readonly IItemStorage _itemStorage;
        private readonly IWakeOnLan _wakeOnLan;

        public HomeController() : this(new JsonItemStorage(), new WakeOnLanProvider())
        {
            
        }
        public HomeController(IItemStorage itemStorage, IWakeOnLan wakeOnLan)
        {
            _itemStorage = itemStorage;
            _wakeOnLan = wakeOnLan;
        }

        [HttpGet]
        public ActionResult Index()
        {
            var items = _itemStorage.GetEntries().ToArray();
            return View(items);
        }

        [HttpPost]
        public ActionResult Index(WakeOnLanEntry model)
        {
            _wakeOnLan.Wake(model.MacAddress);
            return Index();
        }
    }
}