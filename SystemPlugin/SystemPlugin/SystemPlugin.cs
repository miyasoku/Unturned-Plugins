using Rocket.Core;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Framework.Modules;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SystemPlugin
{
    public class SystemPlugin : RocketPlugin<SystemConfiguration>
    {

        public string Creator = "miyasoku";

        public string Version = "2.0.0";

        public bool Player_MaxSkill = false;

        public static SystemPlugin Instance;

        private string difficulty;

        private string cameraMode;

        // => Rocket.Core.R.Plugins.OnPluginsLoaded += () =>
        protected override void Load()
        {
            Logger.Log(Configuration.Instance.LoadMessage, ConsoleColor.Green);
            Logger.Log($"{Name} {Assembly.GetName().Version} has been loaded!", ConsoleColor.Green);

            U.Events.OnPlayerConnected += Events_OnPlayerConnected;
            U.Events.OnPlayerDisconnected += Events_OnPlayerDisconnected;
            UnturnedPlayerEvents.OnPlayerRevive += Events_OnPlayerRevive;

            Instance = this;

            string configDifficulty = Configuration.Instance.Difficulty.ToUpperInvariant();
            switch (configDifficulty)
            {
                case "NRM":
                case "NORMAL":
                    difficulty = "NRM";
                    break;
                case "HARD":
                case "HRD":
                    difficulty = "HRD";
                    break;
                case "EZY":
                case "EASY":
                    difficulty = "EZY";
                    break;
                default:
                    if (Configuration.Instance.Logging)
                        Logger.LogError($"Difficulty: {configDifficulty} not recognized. Defaulting to NRM (Normal)");
                    difficulty = "NRM";
                    break;
            }

            string configCameraMode = Configuration.Instance.CameraMode.ToUpperInvariant();
            switch (configCameraMode)
            {
                case "FIRST":
                case "1PP":
                    cameraMode = "1Pp";
                    break;
                case "BOTH":
                case "2PP":
                    cameraMode = "2Pp";
                    break;
                case "THIRD":
                case "3PP":
                    cameraMode = "3Pp";
                    break;
                case "VEHICLE":
                case "4PP":
                    cameraMode = "4Pp";
                    break;
                default:
                    if (Configuration.Instance.Logging)
                        Logger.LogError($"Camera Mode; {configCameraMode} not recognized. Defaulting to Both (2Pp)");
                    cameraMode = "2Pp";
                    break;
            }

            if (Level.isLoaded)
                StartModifyingLobbyInfo();

            Level.onPostLevelLoaded += OnPostLevelLoaded;
        }


        protected override void Unload()
        {
            Level.onPostLevelLoaded -= OnPostLevelLoaded;
            Logger.Log(Configuration.Instance.UnloadMessage, ConsoleColor.Green);
            Logger.Log($"{Name} {Assembly.GetName().Version} has been unloaded!", ConsoleColor.Green);
        }

        public void OnPostLevelLoaded(int _) => StartModifyingLobbyInfo();
        #region Helpers

        public static int GetWorkshopCount() =>
            (String.Join(",", Provider.getServerWorkshopFileIDs().Select(x => x.ToString()).ToArray()).Length - 1) / 120 + 1;

        public static int GetConfigurationCount() =>
            (String.Join(",", typeof(ModeConfigData).GetFields()
            .SelectMany(x => x.FieldType.GetFields().Select(y => y.GetValue(x.GetValue(Provider.modeConfigData))))
            .Select(x => x is bool v ? v ? "T" : "F" : (String.Empty + x)).ToArray()).Length - 1) / 120 + 1;

        private void StartModifyingLobbyInfo() => new Thread(ModifyLobbyInfo).Start();
        #endregion

        private void ModifyLobbyInfo()
        {
            // Why the hell are we doing this?
            // TODO: Find a new way to do this crap instead of spawning thread
            Thread.Sleep(1000);

            bool workshop = ModifyWorkshop();

            if (Configuration.Instance.MessConfig)
                ModifyConfig(workshop);

            if (Configuration.Instance.HideConfig)
                SteamGameServer.SetKeyValue("Browser_Config_Count", "0");
            else
                SteamGameServer.SetKeyValue("Browser_Config_Count", GetConfigurationCount().ToString());

            ModifyPlugins();
        }

        private bool ModifyWorkshop()
        {
            bool workshop = Provider.getServerWorkshopFileIDs().Count > 0;

            if (Configuration.Instance.HideWorkshop)
            {
                workshop = false;
                SteamGameServer.SetKeyValue("Browser_Workshop_Count", "0");
                Logger.Log($"Workshop Count: {GetWorkshopCount()}");
            }
            else if (Configuration.Instance.MessWorkshop)
            {
                workshop = true;
                string txt = String.Join(",", Configuration.Instance.Workshop);
                SteamGameServer.SetKeyValue("Browser_Workshop_Count", ((txt.Length - 1) / 120 + 1).ToString());

                int line = 0;
                for (int i = 0; i < txt.Length; i += 120)
                {
                    int num6 = 120;

                    if (i + num6 > txt.Length)
                        num6 = txt.Length - i;

                    string pValue2 = txt.Substring(i, num6);
                    SteamGameServer.SetKeyValue("Browser_Workshop_Line_" + line, pValue2);
                    line++;
                }
            }
            else
            {
                SteamGameServer.SetKeyValue("Browser_Workshop_Count", GetWorkshopCount().ToString());
            }

            return workshop;
        }

        private void ModifyConfig(bool workshop)
        {
            string tags = "";
            tags += String.Concat(new string[]
            {
                    Configuration.Instance.IsPVP ? "PVP" : "PVE",
                    ",<gm>",
                    Configuration.Instance.MessGamemode ? Configuration.Instance.Gamemode : Provider.gameMode.GetType().Name,
                    "</gm>,",
                    Configuration.Instance.HasCheats ? "CHy" : "CHn",
                    ",",
                    difficulty,
                    ",",
                    cameraMode,
                    ",",
                    workshop ? "WSy" : "WSn",
                    ",",
                    Configuration.Instance.GoldOnly ? "GLD" : "F2P",
                    ",",
                    Configuration.Instance.HasBattleye ? "BEy" : "BEn"
            });

            if (!String.IsNullOrEmpty(Provider.configData.Browser.Thumbnail))
                tags += ",<tn>" + Provider.configData.Browser.Thumbnail + "</tn>";


            SteamGameServer.SetGameTags(tags);
        }

        private void ModifyPlugins()
        {
            if (Configuration.Instance.InvisibleRocket)
                SteamGameServer.SetBotPlayerCount(0); // Bypasses unturned's filter for rocket <3

            if (!Configuration.Instance.HidePlugins)
            {
                if (Configuration.Instance.MessPlugins)
                    SteamGameServer.SetKeyValue("rocketplugins", string.Join(",", Configuration.Instance.Plugins));
                else
                    SteamGameServer.SetKeyValue("rocketplugins", string.Join(",", R.Plugins.GetPlugins().Select(p => p.Name).ToArray()));
            }
            else
            {
                SteamGameServer.SetKeyValue("rocketplugins", "");
            }

            if (Configuration.Instance.IsVanilla)
            {
                SteamGameServer.SetBotPlayerCount(0);
                SteamGameServer.SetKeyValue("rocketplugins", "");
                SteamGameServer.SetKeyValue("rocket", "");
            }
            else
            {
                if (!Configuration.Instance.InvisibleRocket)
                    SteamGameServer.SetBotPlayerCount(1);
                if (!Configuration.Instance.HidePlugins && !Configuration.Instance.MessPlugins)
                    SteamGameServer.SetKeyValue("rocketplugins", string.Join(",", R.Plugins.GetPlugins().Select(p => p.Name).ToArray()));
                string version = ModuleHook.modules.Find(a => a.config.Name == "Rocket.Unturned")?.config.Version ?? "0.0.0.69";
                SteamGameServer.SetKeyValue("rocket", version);
            }
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
