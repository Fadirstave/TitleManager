using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TitleManager", "FadirStave", "1.0.3")]
    [Description("UI-based chat title selector for BetterChat (restored UI + correct toggling)")]
    public class TitleManager : RustPlugin
    {
        private const string UI_NAME = "TitleManager.UI";

        [PluginReference] private Plugin ImageLibrary;

        private const string BG_IMAGE_ID = "titlemanager_bg";
        private const string BG_IMAGE_URL = "https://cdn3.mapstr.gg/deca10271d9d3441ad21d1580d39468b.png";

        // BetterChat default group (your server uses "default")
        private const string DEFAULT_GROUP = "default";

        // 🎨 UI Colors
        private const string OVERLAY = "0 0 0 0.85";
        private const string FRAME = "0.18 0.18 0.18 0.75";
        private const string BORDER = "0.58 0.24 0.24 0.85";      // thin red border
        private const string PARCHMENT = "0.94 0.88 0.76 0.90";  // parchment color
        private const string WHITE = "1 1 1 1";
        private const string TEXT_DARK = "0.18 0.12 0.07 1";
        private const string BUTTON_RED = "0.58 0.24 0.24 0.95";
        private const string BUTTON_GREY = "0.45 0.45 0.45 0.55";

        private const string RUSTY_ORANGE = "#C66A2B";

        // Title groups ONLY (do not include default here)
        private readonly List<string> TitleGroups = new()
        {
            "esquire",
            "fisherman",
            "hunter",
            "diver",
            "lumberjack",
            "pirate",
            "bountyhunter",
            "explorer",
            "smasher"
        };

        #region Init

        private void OnServerInitialized()
        {
            // Cache image for RawImage usage (same pattern as your InfoPanel)
            ImageLibrary?.Call("AddImage", BG_IMAGE_URL, BG_IMAGE_ID);
        }

        #endregion

        #region Chat Command

        [ChatCommand("title")]
        private void CmdTitle(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            DrawUI(player);
        }

        #endregion

        #region UI

        private void DrawUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UI_NAME);
            var container = new CuiElementContainer();

            // Overlay (cursor enabled so buttons are clickable)
            container.Add(new CuiPanel
            {
                Image = { Color = OVERLAY },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true
            }, "Overlay", UI_NAME);

            // Frame
            container.Add(new CuiPanel
            {
                Image = { Color = FRAME },
                RectTransform = { AnchorMin = "0.34 0.22", AnchorMax = "0.66 0.78" }
            }, UI_NAME, "TM.Frame");

            // Thin border (half thickness feel)
            container.Add(new CuiPanel
            {
                Image = { Color = BORDER },
                RectTransform = { AnchorMin = "0.01 0.01", AnchorMax = "0.99 0.99" }
            }, "TM.Frame", "TM.Border");

            // Board (parchment)
            container.Add(new CuiPanel
            {
                Image = { Color = PARCHMENT },
                RectTransform = { AnchorMin = "0.01 0.01", AnchorMax = "0.99 0.99" }
            }, "TM.Border", "TM.Board");

            // Background image (80% alpha)
            var bg = ImageLibrary?.Call<string>("GetImage", BG_IMAGE_ID);
            if (!string.IsNullOrEmpty(bg))
            {
                container.Add(new CuiElement
                {
                    Parent = "TM.Board",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = bg,
                            Color = "1 1 1 0.8"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                });
            }

            // Close X
            container.Add(new CuiButton
            {
                Button = { Color = BORDER, Command = "titlemanager.close" },
                RectTransform = { AnchorMin = "0.92 0.92", AnchorMax = "0.97 0.97" },
                Text = { Text = "X", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = WHITE }
            }, "TM.Board");

            // Header bar (expanded) – BOTH lines must show
            container.Add(new CuiPanel
            {
                Image = { Color = BORDER },
                RectTransform = { AnchorMin = "0.06 0.72", AnchorMax = "0.94 0.90" }
            }, "TM.Board", "TM.Header");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.03 0.10", AnchorMax = "0.97 0.90" },
                Text =
                {
                    Text =
                        "<b>Earned titles are shown in red below.</b>\n" +
                        "<size=12>Select one to wear it proudly, or choose None to go without.</size>",
                    FontSize = 16,
                    Align = TextAnchor.MiddleCenter,
                    Color = WHITE
                }
            }, "TM.Header");

            // Buttons – pushed down (your requested layout)
            AddButtons(container, player);

            CuiHelper.AddUi(player, container);
        }

        private void AddButtons(CuiElementContainer container, BasePlayer player)
        {
            // ⬇ Buttons lower on purpose (your request)
            float startY = 0.52f;

            float buttonHeight = 0.075f;
            float spacingY = 0.022f;

            float leftX = 0.12f;
            float rightX = 0.52f;
            float width = 0.36f;

            int index = 0;

            // NONE always available
            AddButton(container, player, "none", "None", true, index++, leftX, rightX, startY, buttonHeight, spacingY, width);

            // Always show placeholders; grey unless unlocked
            foreach (var title in TitleGroups)
            {
                bool unlocked = HasTitleUnlocked(player, title);
                AddButton(container, player, title, UpperFirst(title), unlocked, index++, leftX, rightX, startY, buttonHeight, spacingY, width);
            }
        }

        private void AddButton(
            CuiElementContainer container,
            BasePlayer player,
            string title,
            string label,
            bool enabled,
            int index,
            float leftX,
            float rightX,
            float startY,
            float height,
            float spacing,
            float width)
        {
            float yMin = startY - (index / 2) * (height + spacing);
            float yMax = yMin + height;

            float xMin = (index % 2 == 0) ? leftX : rightX;
            float xMax = xMin + width;

            container.Add(new CuiButton
            {
                Button =
                {
                    Color = enabled ? BUTTON_RED : BUTTON_GREY,
                    Command = enabled ? $"titlemanager.set {title}" : ""
                },
                RectTransform =
                {
                    AnchorMin = $"{xMin} {yMin}",
                    AnchorMax = $"{xMax} {yMax}"
                },
                Text =
                {
                    Text = label,
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = TEXT_DARK
                }
            }, "TM.Board");
        }

        #endregion

        #region Logic

        [ConsoleCommand("titlemanager.set")]
        private void CmdSetTitle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || arg.Args == null || arg.Args.Length < 1) return;

            string selected = arg.Args[0].ToLower();
            string id = player.UserIDString;

            // Safety: NONE always allowed, titles must be unlocked
            if (selected != "none" && !HasTitleUnlocked(player, selected))
            {
                player.ChatMessage("You haven't earned that title yet.");
                return;
            }

            if (selected == "none")
            {
                // Clear ONLY title groups (do NOT touch permissions)
                RemoveAllTitleGroups(id);

                // Always ensure default group is applied
                ConsoleSystem.Run(ConsoleSystem.Option.Server, $"chat user add {id} {DEFAULT_GROUP}");

                player.ChatMessage("Chat title cleared.");
                return;
            }

            // If already active, do nothing (prevents “clears everything” feeling)
            if (permission.UserHasGroup(id, selected))
            {
                player.ChatMessage("That title is already active.");
                return;
            }

            // Remove other title groups, but do NOT “disable” anything
            foreach (var group in TitleGroups)
            {
                if (group != selected && permission.UserHasGroup(id, group))
                    ConsoleSystem.Run(ConsoleSystem.Option.Server, $"chat user remove {id} {group}");
            }

            // Remove default to keep one visible title (matches your original behavior/logs)
            if (permission.UserHasGroup(id, DEFAULT_GROUP))
                ConsoleSystem.Run(ConsoleSystem.Option.Server, $"chat user remove {id} {DEFAULT_GROUP}");

            // Apply selected
            ConsoleSystem.Run(ConsoleSystem.Option.Server, $"chat user add {id} {selected}");

            player.ChatMessage($"Chat title set to <color={RUSTY_ORANGE}>{UpperFirst(selected)}</color>");
        }

        private void RemoveAllTitleGroups(string userId)
        {
            foreach (var group in TitleGroups)
            {
                if (permission.UserHasGroup(userId, group))
                    ConsoleSystem.Run(ConsoleSystem.Option.Server, $"chat user remove {userId} {group}");
            }
        }

        private bool HasTitleUnlocked(BasePlayer player, string title)
        {
            // Unlock condition: either a permission (quest reward style) OR they’re already in that group
            return permission.UserHasPermission(player.UserIDString, $"title.{title}")
                   || permission.UserHasGroup(player.UserIDString, title);
        }

        [ConsoleCommand("titlemanager.close")]
        private void CmdClose(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, UI_NAME);
        }

        #endregion

        #region Helpers

        private static string UpperFirst(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, UI_NAME);
        }

        #endregion
    }
}
