using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemPlugin
{
    public class SystemConfiguration : IRocketPluginConfiguration
    {

        public string LoadMessage { get; set; }
        public string UnloadMessage { get; set; }

        public void LoadDefaults()
        {
            LoadMessage = "This is the testing plugin.";
            UnloadMessage = "This is the testing plugin.";
        }
    }
}
