using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

#if RUST
using UnityEngine;
using ConVar;
#endif

namespace Oxide.Plugins
{
    [Info("TitleManager", "Fadir", "1.3.0")]
    [Description("Standalone title & chat prefix system driven purely by permissions.")]
    public class TitleManager : CovalencePlugin
    {
        private ConfigData config;
        private string PermPrefix => Name.ToLower();

        // Vanilla-like colors
        private const string AdminColor = "#55ff55";
        private const string PlayerColor = "#5af";

        #region Config

        private class TitleEntry
        {
            [JsonProperty("Text")]
            public string Text = "TITLE";

            [JsonProperty("Color")]
            public string Color = "#7A1F1F";

            [JsonProperty("Permission")]
            public string Permission = "esquire";

            [JsonProperty("Priority")]
            public int Priority = 0;
        }

        private class ConfigData
        {
            [JsonProperty("Titles")]
            public Dictionary<string, TitleEntry> Titles = new Dictionary<string, TitleEntry>();

            [JsonProperty("Chat Format")]
            public string ChatFormat = "{Title} {Name}: {Message}";
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData
            {
                ChatFormat = "{Title} {Name}: {Message}",
                Titles = new Dictionary<string, TitleEntry>
                {
                    ["esquire"] = new TitleEntry
                    {
                        Text = "Esquire",
                        Color = "#7A1F1F",
                        Permission = "esquire",
                        Priority = 10
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<ConfigData>();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        #endregion

        #region Init

        private void OnServerInitialized()
        {
            RegisterPermissions();
        }

        private void RegisterPermissions()
        {
            foreach (var t in config.Titles.Values)
            {
                var perm = GetPerm(t);
                if (!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);
            }
        }

        #endregion

        #region Chat Hook

#if RUST
        private object OnPlayerChat(BasePlayer basePlayer, string message, Chat.ChatChannel channel)
        {
            var player = basePlayer.IPlayer;
            var title = GetPlayerTitle(player);

            // No title → vanilla Rust chat
            if (title == null)
                return null;

            var titleText = FormatTitle(title);

            // Apply vanilla-like name coloring
            var isAdmin = basePlayer.IsAdmin || permission.UserHasPermission(player.Id, "admin");
            var nameColor = isAdmin ? AdminColor : PlayerColor;
            var nameText = $"<color={nameColor}>{basePlayer.displayName}</color>";

            var output = config.ChatFormat
                .Replace("{Title}", titleText)
                .Replace("{Name}", nameText)
                .Replace("{Message}", message);

            if (channel == Chat.ChatChannel.Team)
            {
                var team = basePlayer.Team;
                if (team != null)
                {
                    foreach (var id in team.members)
                    {
                        var member = BasePlayer.FindByID(id);
                        if (member != null)
                            member.SendConsoleCommand("chat.add", (int)channel, basePlayer.userID, output);
                    }
                }
            }
            else
            {
                foreach (var p in BasePlayer.activePlayerList)
                    p.SendConsoleCommand("chat.add", (int)channel, basePlayer.userID, output);
            }

            Puts($"[{channel}] {basePlayer.displayName}: {message}");
            return true; // Block default chat for titled players
        }
#endif

        #endregion

        #region Title Logic

        private string GetPerm(TitleEntry t)
        {
            return t.Permission.StartsWith($"{PermPrefix}.")
                ? t.Permission
                : $"{PermPrefix}.{t.Permission}";
        }

        private TitleEntry GetPlayerTitle(IPlayer player)
        {
            return config.Titles.Values
                .Where(t => permission.UserHasPermission(player.Id, GetPerm(t)))
                .OrderByDescending(t => t.Priority)
                .FirstOrDefault();
        }

        private string FormatTitle(TitleEntry title)
        {
            var color = title.Color.StartsWith("#") ? title.Color : $"#{title.Color}";
            return $"<color={color}>[{title.Text}]</color>";
        }

        #endregion
    }
}
