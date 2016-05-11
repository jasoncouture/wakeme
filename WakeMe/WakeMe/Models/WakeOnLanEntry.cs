using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WakeMe.Models
{
    public class WakeOnLanEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("n");
        public string MacAddress { get; set; }
        public string Name { get; set; }
    }
}