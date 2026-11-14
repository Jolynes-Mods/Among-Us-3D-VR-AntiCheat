using AntiCheat.Config;
using Il2CppSG.Airlock;
using MelonLoader;
using MelonLoader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AntiCheat.Managers
{
    internal static class GUIManager
    {
        private static Rect Window = new Rect(22f, 22f, 940f, 650f);
        private static int Category;
        private static int WhitelistPage;
        private static int BlacklistPage;
        private static GUIStyle WindowStyle;
        private static GUIStyle TitleStyle;
        private static GUIStyle SectionStyle;
        private static GUIStyle TextStyle;
        private static GUIStyle DescriptionStyle;
        private static GUIStyle WarningStyle;
        private static GUIStyle CategoryStyle;
        private static GUIStyle SelectedCategoryStyle;
        private static GUIStyle PanelStyle;
        private static GUIStyle ButtonStyle;
        private static GUIStyle RemoveButtonStyle;
        private static GUIStyle EmptyStyle;
        private static Texture2D Background;
        private static Texture2D Panel;
        private static Texture2D PanelHover;
        private static Texture2D Blue;
        private static Texture2D BlueHover;
        private static Texture2D DarkBlue;
        private static Texture2D Red;
        private static Texture2D RedHover;
        private static Texture2D Disabled;
        private static Texture2D White;
        private static readonly Dictionary<string, float> Animations = new Dictionary<string, float>();
        private static readonly Dictionary<string, WhitelistEntry> Whitelist = new Dictionary<string, WhitelistEntry>(StringComparer.Ordinal);

        internal static bool Binding;

        private sealed class WhitelistEntry
        {
            internal string UserId;
            internal string Name;
            internal string Platform;

            internal WhitelistEntry(string UserId, string Name, string Platform)
            {
                this.UserId = UserId;
                this.Name = Name;
                this.Platform = Platform;
            }
        }

        private readonly struct Module
        {
            internal readonly string Name;
            internal readonly string Description;
            internal readonly Func<bool> Get;
            internal readonly Action<bool> Set;
            internal readonly bool Warning;

            internal Module(string Name, string Description, Func<bool> Get, Action<bool> Set, bool Warning = false)
            {
                this.Name = Name;
                this.Description = Description;
                this.Get = Get;
                this.Set = Set;
                this.Warning = Warning;
            }
        }

        private static readonly Module[] Modules =
        {
            new Module("Cosmetic Validation", "Checks cosmetic changes and invalid items", () => Settings.CosmeticValidateModule, Value => Settings.CosmeticValidateModule = Value),
            new Module("Meeting Validation", "Verifies the caller and that it wasn't called with exploits", () => Settings.CallMeetingValidateModule, Value => Settings.CallMeetingValidateModule = Value),
            new Module("Body Report Validation", "Verifies the reportrer and that it wasn't called with exploits", () => Settings.CallBodyReportValidateModule, Value => Settings.CallBodyReportValidateModule = Value),
            new Module("Task Validation", "Verifies tasks were not completed with exploits", () => Settings.TaskCompletedValidateModule, Value => Settings.TaskCompletedValidateModule = Value),
            new Module("Kick Vote Validation", "Checks invalid and repeated/exploited kick votes", () => Settings.KickVoteValidateModule, Value => Settings.KickVoteValidateModule = Value),
            new Module("Powerup Request Validation", "Checks whether a powerup can be requested", () => Settings.RequestPowerupValidateModule, Value => Settings.RequestPowerupValidateModule = Value),
            new Module("Username Validation", "Verifies names are not using illegal characters", () => Settings.UsernameValidateModule, Value => Settings.UsernameValidateModule = Value),
            new Module("Body Spawn Validation", "Verifies bodies have a legal cause of spawning", () => Settings.SpawnBodyValidateModule, Value => Settings.SpawnBodyValidateModule = Value),
            new Module("Player Join Validation", "Verifies the joining player's identity", () => Settings.PlayerJoinValidateModule, Value => Settings.PlayerJoinValidateModule = Value),
            new Module("Crash Prevention", "Warning: May remove innocent players to keep the lobby from crashing.", () => Settings.TakePhotoValidateModule, Value => Settings.TakePhotoValidateModule = Value, true),
            new Module("Targeted Action Validation", "Verify all actions, e.g kills, infect, guard...", () => Settings.TargetedActionValidateModule, Value => Settings.TargetedActionValidateModule = Value),
            new Module("Lobby Door Validation", "Verifies when the lobby doors should be toggled", () => Settings.ToggleLobbyDoorValidateModule, Value => Settings.ToggleLobbyDoorValidateModule = Value),
            new Module("Powerup Use Validation", "Checks whether a powerup can be used when called", () => Settings.UsePowerupValidateModule, Value => Settings.UsePowerupValidateModule = Value),
            new Module("Vent Validation", "Verifies the player is allowed to vent", () => Settings.VentValidateModule, Value => Settings.VentValidateModule = Value),
            new Module("Vote Validation", "Varifies that the meeting vote is not exploited", () => Settings.VoteValidateModule, Value => Settings.VoteValidateModule = Value),
            new Module("Player Speed Validation", "Checks for suspicious player movements", () => Settings.PlayerSpeedValidateModule, Value => Settings.PlayerSpeedValidateModule = Value)
        };

        internal static void Display()
        {
            if (!Settings.GUIEnabled) return;
            if (!Settings.IsHost) Settings.AntiCheatEnabled = false;

            Commands.LoadData();
            Initialize();

            int Rows = Mathf.CeilToInt(Modules.Length / 2f);
            Window.height = Category == 0 ? Mathf.Max(650f, 158f + Rows * 61f) : 650f;
            Window.x = Mathf.Clamp(Window.x, 0f, Mathf.Max(0f, Screen.width - Window.width));
            Window.y = Mathf.Clamp(Window.y, 0f, Mathf.Max(0f, Screen.height - 42f));
            Window = GUI.Window(420, Window, (GUI.WindowFunction)DrawWindow, GUIContent.none, WindowStyle);
        }


        private static void DrawOutdated()
        {
            GUI.Box(new Rect(14f, 49f, Window.width - 28f, 180f), GUIContent.none, PanelStyle);
            GUI.Label(new Rect(34f, 69f, 500f, 30f), "Update available", TitleStyle);
            GUI.Label(new Rect(34f, 105f, 700f, 24f), $"Installed: {Settings.Version}    Latest: {AutoUpdateManager.LatestVersion}", TextStyle);
            GUI.Label(new Rect(34f, 135f, 700f, 24f), AutoUpdateManager.Status, DescriptionStyle);

            if (!AutoUpdateManager.Updating && GUI.Button(new Rect(34f, 175f, 150f, 34f), "Auto update and restart", ButtonStyle)) AutoUpdateManager.Update();
            if (!AutoUpdateManager.Updating && GUI.Button(new Rect(196f, 175f, 150f, 34f), "Open latest release", ButtonStyle)) Application.OpenURL("https://github.com/NotJolyne1/AntiCheat/releases");

            if (!AutoUpdateManager.Updating && GUI.Button(new Rect(358f, 175f, 110f, 34f), "Ignore", RemoveButtonStyle))
            {
                Settings.IgnoreUpdate = true;
                Settings.Outdated = false;
            }
        }

        internal static void RememberWhitelist(PlayerState Player, string UserId)
        {
            Commands.LoadData();
            Whitelist[UserId] = new WhitelistEntry(UserId, Player.NetworkName.Value, Helpers.GetPlayerPlatform(Player));
            Commands.SaveData();
        }

        internal static void ForgetWhitelist(string UserId)
        {
            Commands.LoadData();
            if (!Whitelist.Remove(UserId)) return;
            Commands.SaveData();
        }

        internal static void SetKeybind(Key Key)
        {
            Settings.GUIKey = Key;
            Binding = false;
            Commands.SaveData();
        }

        private static void DrawWindow(int WindowId)
        {
            if (Settings.Outdated && !Settings.IgnoreUpdate)
            {
                DrawOutdated();
                GUI.DragWindow(new Rect(0f, 0f, Window.width, 45f));
                return;
            }

            GUI.Label(new Rect(20f, 12f, 300f, 28f), $"Anti-Cheat V{Settings.Version}", TitleStyle);
            GUI.Label(new Rect(Window.width - 405f, 7f, 395f, 36f), Settings.IsHost ? "" : "Host required to modify", new GUIStyle(Settings.IsHost ? TextStyle : WarningStyle) { alignment = TextAnchor.MiddleRight, fontSize = 14 });
            GUI.Box(new Rect(14f, 49f, 150f, Window.height - 63f), GUIContent.none, PanelStyle);

            DrawCategory("Anti-Cheat", 0, 62f);
            DrawCategory("Whitelist", 1, 104f);
            DrawCategory("Blacklist", 2, 146f);
            if (GUI.Button(new Rect(22f, Window.height - 56f, 134f, 34f), Binding ? "Press a key" : $"Menu key: {Settings.GUIKey}", CategoryStyle)) Binding = !Binding;

            GUI.BeginGroup(new Rect(178f, 49f, Window.width - 192f, Window.height - 63f));
            if (Category == 0) DrawAntiCheat();
            if (Category == 1) DrawWhitelist();
            if (Category == 2) DrawBlacklist();
            GUI.EndGroup();

            GUI.DragWindow(new Rect(0f, 0f, Window.width, 45f));
        }

        private static void DrawAntiCheat()
        {
            bool HostEnabled = Settings.IsHost;
            Settings.AntiCheatEnabled = DrawLargeToggle(new Rect(0f, 0f, 358f, 60f), "Enable Anti-Cheat", "Turns lobby protection on or off", "Main", Settings.AntiCheatEnabled, HostEnabled);
            Settings.PlayerBlacklistModule = DrawLargeToggle(new Rect(370f, 0f, 358f, 60f), "Enable Blacklist", "Auto kick blacklisted/known abusive cheaters", "Blacklist", Settings.PlayerBlacklistModule, HostEnabled && Settings.AntiCheatEnabled);

            GUI.Label(new Rect(2f, 71f, 300f, 24f), "Protection modules", SectionStyle);
            bool ModulesEnabled = HostEnabled && Settings.AntiCheatEnabled;

            for (int Index = 0; Index < Modules.Length; Index++)
            {
                int Column = Index % 2;
                int Row = Index / 2;
                DrawModule(new Rect(Column * 370f, 98f + Row * 61f, 358f, 55f), Modules[Index], ModulesEnabled);
            }
        }

        private static void DrawWhitelist()
        {
            GUI.Label(new Rect(2f, 2f, 300f, 28f), "Whitelist", TitleStyle);
            GUI.Label(new Rect(2f, 34f, 720f, 38f), "Warning: These players will be immune to all anti-cheat modules. Use this only for trusted players.", WarningStyle);

            if (GameReferences.Spawn == null || GameReferences.Rig == null || GameReferences.Runner == null)
            {
                GUI.Label(new Rect(2f, 78f, 500f, 24f), "Waiting for players", DescriptionStyle);
                return;
            }

            GUI.Label(new Rect(2f, 76f, 300f, 22f), "Lobby players", SectionStyle);
            GUI.Label(new Rect(372f, 76f, 300f, 22f), "Saved players outside lobby", SectionStyle);

            int LobbyIndex = 0;

            foreach (PlayerState Player in GameReferences.Spawn.ActivePlayerStates)
            {
                if (Player == null) continue;

                string UserId = GameReferences.Runner.GetPlayerUserId(Player.PlayerId);
                string Platform = Helpers.GetPlayerPlatform(Player);
                DrawLobbyPlayer(new Rect(0f, 103f + LobbyIndex * 50f, 358f, 44f), Player, UserId, Platform);
                LobbyIndex++;
            }

            List<WhitelistEntry> Offline = GetOfflineWhitelist();
            int PageCount = Mathf.Max(1, Mathf.CeilToInt(Offline.Count / 9f));
            WhitelistPage = Mathf.Clamp(WhitelistPage, 0, PageCount - 1);
            int Start = WhitelistPage * 9;
            int End = Mathf.Min(Start + 9, Offline.Count);

            if (Offline.Count == 0) GUI.Label(new Rect(374f, 108f, 330f, 24f), "No saved players", DescriptionStyle);

            for (int Index = Start; Index < End; Index++)
                DrawOfflinePlayer(new Rect(370f, 103f + (Index - Start) * 50f, 358f, 44f), Offline[Index]);

            if (PageCount > 1)
            {
                if (GUI.Button(new Rect(370f, 558f, 82f, 28f), "Previous", ButtonStyle) && WhitelistPage > 0) WhitelistPage--;
                GUI.Label(new Rect(464f, 562f, 170f, 22f), $"Page {WhitelistPage + 1} of {PageCount}", DescriptionStyle);
                if (GUI.Button(new Rect(646f, 558f, 82f, 28f), "Next", ButtonStyle) && WhitelistPage < PageCount - 1) WhitelistPage++;
            }
        }

        private static void DrawBlacklist()
        {
            GUI.Label(new Rect(2f, 2f, 300f, 28f), "Blacklist", TitleStyle);
            GUI.Label(new Rect(2f, 33f, 500f, 20f), $"Loaded: {Commands.GlobalBlacklistCount} globally blacklisted players", DescriptionStyle);

            bool Persist = DrawLargeToggle(new Rect(0f, 62f, 358f, 60f), "Persist blacklist", "Remember blacklisted players after closing the game.", "PersistBlacklist", Settings.PersistBlacklist, Settings.IsHost);
            if (Persist != Settings.PersistBlacklist) Commands.SetBlacklistPersistence(Persist);

            Settings.BlacklistOnDetect = DrawLargeToggle(new Rect(370f, 62f, 358f, 60f), "Blacklist on detect", "Add detected players to your local blacklist.", "BlacklistOnDetect", Settings.BlacklistOnDetect, Settings.IsHost);
            List<KeyValuePair<string, (string Username, string Platform, string Reason)>> Players = new List<KeyValuePair<string, (string Username, string Platform, string Reason)>>(Commands.GetBlacklistedPlayers());
            int PageCount = Mathf.Max(1, Mathf.CeilToInt(Players.Count / 10f));
            BlacklistPage = Mathf.Clamp(BlacklistPage, 0, PageCount - 1);
            int Start = BlacklistPage * 10;
            int End = Mathf.Min(Start + 10, Players.Count);

            GUI.Label(new Rect(2f, 136f, 300f, 22f), "Local blacklist", SectionStyle);

            if (Players.Count == 0) GUI.Label(new Rect(2f, 168f, 500f, 24f), "No players are locally blacklisted.", DescriptionStyle);

            for (int Index = Start; Index < End; Index++)
            {
                int Position = Index - Start;
                int Column = Position % 2;
                int Row = Position / 2;
                DrawBlacklistedPlayer(new Rect(Column * 370f, 164f + Row * 76f, 358f, 70f), Players[Index]);
            }

            if (PageCount > 1)
            {
                if (GUI.Button(new Rect(0f, 550f, 82f, 28f), "Previous", ButtonStyle) && BlacklistPage > 0) BlacklistPage--;
                GUI.Label(new Rect(94f, 554f, 170f, 22f), $"Page {BlacklistPage + 1} of {PageCount}", DescriptionStyle);
                if (GUI.Button(new Rect(276f, 550f, 82f, 28f), "Next", ButtonStyle) && BlacklistPage < PageCount - 1) BlacklistPage++;
            }
        }

        private static void DrawLobbyPlayer(Rect Area, PlayerState Player, string UserId, string Platform)
        {
            bool Listed = Player.IsWhitelisted();
            GUI.DrawTexture(Area, Area.Contains(Event.current.mousePosition) ? PanelHover : Panel);
            GUI.Label(new Rect(Area.x + 10f, Area.y + 4f, Area.width - 94f, 18f), Player.NetworkName.Value, TextStyle);
            GUI.Label(new Rect(Area.x + 10f, Area.y + 22f, Area.width - 94f, 18f), $"{Platform}  {UserId}", DescriptionStyle);

            if (Listed)
            {
                if (GUI.Button(new Rect(Area.xMax - 78f, Area.y + 8f, 68f, 28f), "Remove", RemoveButtonStyle)) Commands.UnwhitelistPlayer(Player);
            }
            else
            {
                if (GUI.Button(new Rect(Area.xMax - 78f, Area.y + 8f, 68f, 28f), "Add", ButtonStyle)) Commands.WhitelistPlayer(Player);
            }
        }

        private static void DrawOfflinePlayer(Rect Area, WhitelistEntry Entry)
        {
            GUI.DrawTexture(Area, Area.Contains(Event.current.mousePosition) ? PanelHover : Panel);
            GUI.Label(new Rect(Area.x + 10f, Area.y + 4f, Area.width - 94f, 18f), string.IsNullOrEmpty(Entry.Name) ? "Saved player" : Entry.Name, TextStyle);
            GUI.Label(new Rect(Area.x + 10f, Area.y + 22f, Area.width - 94f, 18f), $"{Entry.Platform}  {Entry.UserId}", DescriptionStyle);
            if (GUI.Button(new Rect(Area.xMax - 78f, Area.y + 8f, 68f, 28f), "Remove", RemoveButtonStyle)) Commands.UnwhitelistUserId(Entry.UserId);
        }

        private static void DrawBlacklistedPlayer(Rect Area, KeyValuePair<string, (string Username, string Platform, string Reason)> Player)
        {
            GUI.DrawTexture(Area, Area.Contains(Event.current.mousePosition) ? PanelHover : Panel);
            GUI.Label(new Rect(Area.x + 10f, Area.y + 4f, Area.width - 94f, 18f), string.IsNullOrEmpty(Player.Value.Username) ? "Unknown player" : Player.Value.Username, TextStyle);
            GUI.Label(new Rect(Area.x + 10f, Area.y + 21f, Area.width - 94f, 18f), $"Platform: {Player.Value.Platform}", DescriptionStyle);
            GUI.Label(new Rect(Area.x + 10f, Area.y + 36f, Area.width - 94f, 18f), $"ID: {Player.Key}", DescriptionStyle);
            GUI.Label(new Rect(Area.x + 10f, Area.y + 51f, Area.width - 94f, 18f), $"Reason: {Player.Value.Reason}", WarningStyle);
            if (Settings.IsHost && GUI.Button(new Rect(Area.xMax - 78f, Area.y + 21f, 68f, 28f), "Remove", RemoveButtonStyle)) Commands.RemoveBlacklistedPlayer(Player.Key);
        }

        private static List<WhitelistEntry> GetOfflineWhitelist()
        {
            List<WhitelistEntry> Offline = new List<WhitelistEntry>();

            if (GameReferences.Spawn == null || GameReferences.Runner == null)
            {
                Offline.AddRange(Whitelist.Values);
                return Offline;
            }

            foreach (WhitelistEntry Entry in Whitelist.Values)
            {
                bool Online = false;

                foreach (PlayerState Player in GameReferences.Spawn.ActivePlayerStates)
                {
                    if (Player == null) continue;
                    if (GameReferences.Runner.GetPlayerUserId(Player.PlayerId) != Entry.UserId) continue;
                    Online = true;
                    break;
                }

                if (!Online) Offline.Add(Entry);
            }

            Offline.Sort((First, Second) => string.Compare(First.Name, Second.Name, StringComparison.OrdinalIgnoreCase));
            return Offline;
        }

        private static void DrawModule(Rect Area, Module Module, bool Enabled)
        {
            bool Value = Module.Get();
            bool Hovered = Area.Contains(Event.current.mousePosition);
            GUI.DrawTexture(Area, Hovered && Enabled ? PanelHover : Panel);
            GUI.Label(new Rect(Area.x + 12f, Area.y + 6f, Area.width - 74f, 19f), Module.Name, TextStyle);
            GUI.Label(new Rect(Area.x + 12f, Area.y + 25f, Area.width - 74f, 26f), Module.Description, Module.Warning ? WarningStyle : DescriptionStyle);
            DrawSwitch(new Rect(Area.xMax - 52f, Area.y + 16f, 40f, 23f), Module.Name, Value, Enabled);
            if (Enabled && GUI.Button(Area, GUIContent.none, EmptyStyle)) Module.Set(!Value);
        }

        private static bool DrawLargeToggle(Rect Area, string Name, string Description, string Key, bool Value, bool Enabled)
        {
            bool Hovered = Area.Contains(Event.current.mousePosition);
            GUI.DrawTexture(Area, Hovered && Enabled ? PanelHover : Panel);
            GUI.Label(new Rect(Area.x + 14f, Area.y + 8f, Area.width - 84f, 21f), Name, SectionStyle);
            GUI.Label(new Rect(Area.x + 14f, Area.y + 32f, Area.width - 84f, 20f), Description, DescriptionStyle);
            DrawSwitch(new Rect(Area.xMax - 59f, Area.y + 18f, 45f, 25f), Key, Value, Enabled);
            if (Enabled && GUI.Button(Area, GUIContent.none, EmptyStyle)) return !Value;
            return Value;
        }

        private static void DrawSwitch(Rect Area, string Key, bool Value, bool Enabled)
        {
            if (!Animations.TryGetValue(Key, out float Position)) Position = Value ? 1f : 0f;
            if (Event.current.type == EventType.Repaint) Position = Mathf.MoveTowards(Position, Value ? 1f : 0f, Time.unscaledDeltaTime * 8f);
            Animations[Key] = Position;

            GUI.DrawTexture(Area, Enabled ? Position > 0.35f ? Blue : DarkBlue : Disabled);
            float Size = Area.height - 6f;
            float PositionX = Mathf.Lerp(Area.x + 3f, Area.xMax - Size - 3f, Position);
            GUI.DrawTexture(new Rect(PositionX, Area.y + 3f, Size, Size), White);
        }

        private static void DrawCategory(string Name, int Index, float Position)
        {
            GUIStyle Style = Category == Index ? SelectedCategoryStyle : CategoryStyle;
            if (GUI.Button(new Rect(22f, Position, 134f, 34f), Name, Style)) Category = Index;
        }

        internal static void ReadWhitelist(BinaryReader Reader)
        {
            int Count = Reader.ReadInt32();
            if (Count < 0 || Count > 10000) throw new InvalidDataException();

            for (int Index = 0; Index < Count; Index++)
            {
                string UserId = Reader.ReadString();
                string Name = Reader.ReadString();
                string Platform = Reader.ReadString();

                if (!string.IsNullOrEmpty(UserId))
                {
                    Whitelist[UserId] = new WhitelistEntry(UserId, Name, Platform);
                    Commands.RestoreWhitelistedPlayer(UserId);
                }
            }
        }

        internal static void WriteWhitelist(BinaryWriter Writer)
        {
            Writer.Write(Whitelist.Count);

            foreach (WhitelistEntry Entry in Whitelist.Values)
            {
                Writer.Write(Entry.UserId ?? string.Empty);
                Writer.Write(Entry.Name ?? string.Empty);
                Writer.Write(Entry.Platform ?? string.Empty);
            }
        }

        private static void Initialize()
        {
            if (WindowStyle != null && Background != null && Panel != null && Blue != null && DarkBlue != null && Disabled != null && White != null) return;

            Background = CreateTexture(new Color(0.035f, 0.052f, 0.082f));
            Panel = CreateTexture(new Color(0.062f, 0.088f, 0.13f));
            PanelHover = CreateTexture(new Color(0.075f, 0.12f, 0.19f));
            Blue = CreateTexture(new Color(0.08f, 0.46f, 0.9f));
            BlueHover = CreateTexture(new Color(0.12f, 0.54f, 1f));
            DarkBlue = CreateTexture(new Color(0.07f, 0.14f, 0.23f));
            Red = CreateTexture(new Color(0.55f, 0.12f, 0.15f));
            RedHover = CreateTexture(new Color(0.72f, 0.16f, 0.19f));
            Disabled = CreateTexture(new Color(0.12f, 0.13f, 0.15f));
            White = CreateTexture(new Color(0.9f, 0.94f, 1f));

            WindowStyle = new GUIStyle(GUI.skin.window);
            SetBackground(WindowStyle, Background);

            PanelStyle = new GUIStyle(GUI.skin.box);
            SetBackground(PanelStyle, Panel);

            TitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            TitleStyle.normal.textColor = new Color(0.27f, 0.66f, 1f);

            SectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            SectionStyle.normal.textColor = Color.white;

            TextStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, clipping = TextClipping.Clip };
            TextStyle.normal.textColor = new Color(0.87f, 0.92f, 1f);

            DescriptionStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true, clipping = TextClipping.Clip };
            DescriptionStyle.normal.textColor = new Color(0.58f, 0.68f, 0.8f);

            WarningStyle = new GUIStyle(DescriptionStyle);
            WarningStyle.normal.textColor = new Color(1f, 0.58f, 0.27f);

            CategoryStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontSize = 12, padding = new RectOffset(12, 6, 0, 0) };
            SetBackground(CategoryStyle, Panel);
            CategoryStyle.normal.textColor = new Color(0.65f, 0.74f, 0.85f);
            CategoryStyle.hover.textColor = Color.white;
            CategoryStyle.active.textColor = Color.white;

            SelectedCategoryStyle = new GUIStyle(CategoryStyle);
            SetBackground(SelectedCategoryStyle, Blue);
            SelectedCategoryStyle.hover.background = BlueHover;
            SelectedCategoryStyle.active.background = BlueHover;
            SelectedCategoryStyle.normal.textColor = Color.white;

            ButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 11 };
            SetBackground(ButtonStyle, Blue);
            ButtonStyle.hover.background = BlueHover;
            ButtonStyle.active.background = BlueHover;
            ButtonStyle.normal.textColor = Color.white;

            RemoveButtonStyle = new GUIStyle(ButtonStyle);
            SetBackground(RemoveButtonStyle, Red);
            RemoveButtonStyle.hover.background = RedHover;
            RemoveButtonStyle.active.background = RedHover;

            EmptyStyle = new GUIStyle();
        }

        private static void SetBackground(GUIStyle Style, Texture2D Texture)
        {
            Style.normal.background = Texture;
            Style.hover.background = Texture;
            Style.active.background = Texture;
            Style.focused.background = Texture;
            Style.onNormal.background = Texture;
            Style.onHover.background = Texture;
            Style.onActive.background = Texture;
            Style.onFocused.background = Texture;
        }

        private static Texture2D CreateTexture(Color Color)
        {
            Texture2D Texture = new Texture2D(1, 1);
            Texture.hideFlags = HideFlags.HideAndDontSave;
            Texture.SetPixel(0, 0, Color);
            Texture.Apply();
            return Texture;
        }
    }
}