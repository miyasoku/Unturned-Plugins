using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemPlugin
{
    public class SystemPlugin : RocketPlugin<SystemConfiguration>
    {

        public string Creator = "miyasoku";

        public string Version = "1.0.0";

        public bool Player_MaxSkill = false;

        // => Rocket.Core.R.Plugins.OnPluginsLoaded += () =>
        protected override void Load()
        {
            Logger.Log(Configuration.Instance.LoadMessage, ConsoleColor.Green);
            Logger.Log($"{Name} {Assembly.GetName().Version} has been loaded!", ConsoleColor.Green);

            U.Events.OnPlayerConnected += Events_OnPlayerConnected;
            U.Events.OnPlayerDisconnected += Events_OnPlayerDisconnected;
            UnturnedPlayerEvents.OnPlayerRevive += Events_OnPlayerRevive;

            Rocket.Core.R.Plugins.OnPluginsLoaded += () =>
            {
                SteamGameServer.SetKeyValue("Browser_Workshop_Count", "0");
                SteamGameServer.SetKeyValue("rocketplugins", "");
                SteamGameServer.SetKeyValue("Browser_Config_Count", "0");
            };
        }

        protected override void Unload()
        {
            Logger.Log(Configuration.Instance.UnloadMessage, ConsoleColor.Green);
            Logger.Log($"{Name} {Assembly.GetName().Version} has been unloaded!", ConsoleColor.Green);
        }

        private void Events_OnPlayerConnected(UnturnedPlayer player)
        {
            UnturnedChat.Say(player.CharacterName + " がサーバーに参加した。", UnityEngine.Color.cyan);
            UnturnedChat.Say(player.CharacterName + " has connected to the server.", UnityEngine.Color.cyan);
            if (Player_MaxSkill == false)
            {
                ChatManager.serverSendMessage("あなたのスキルは自動的に最大になりました！", UnityEngine.Color.yellow, null, player.SteamPlayer(), EChatMode.SAY);
                ChatManager.serverSendMessage("Your skills were automatically maxed!", UnityEngine.Color.yellow, null, player.SteamPlayer(), EChatMode.SAY);
                player.MaxSkills();
            }
        }

        private void Events_OnPlayerDisconnected(UnturnedPlayer player)
        {
            UnturnedChat.Say(player.CharacterName + " がサーバーから退出した。", UnityEngine.Color.cyan);
            UnturnedChat.Say(player.CharacterName + " has disconnected from the server.", UnityEngine.Color.cyan);
        }

        private void Events_OnPlayerRevive(UnturnedPlayer player, UnityEngine.Vector3 position, byte angle)
        {
            ChatManager.serverSendMessage("あなたのスキルは自動的に最大になりました！", UnityEngine.Color.yellow, null, player.SteamPlayer(), EChatMode.SAY);
            ChatManager.serverSendMessage("Your skills were automatically maxed!", UnityEngine.Color.yellow, null, player.SteamPlayer(), EChatMode.SAY);
            Player_MaxSkill = true;
            player.MaxSkills();
        }
    }
}
