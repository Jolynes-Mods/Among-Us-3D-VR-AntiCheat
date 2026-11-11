using AntiCheat.Config;
using Il2CppFusion;
using Il2CppFusion.Photon.Realtime;
using Il2CppSG.Airlock;
using Il2CppSG.Airlock.Roles;
using MelonLoader;
using MelonLoader.Utils;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace AntiCheat.Managers
{
    internal static class Commands
    {
        private static readonly HashSet<string> WhitelistedPlayers = new HashSet<string>();
        private static readonly SortedDictionary<string, (string Username, string Platform, string Reason)> BlacklistedPlayers = new SortedDictionary<string, (string Username, string Platform, string Reason)>(StringComparer.Ordinal);
        private static readonly HashSet<string> GloballyBlacklistedPlayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool DoorCorrectionPending;

        internal static void KickPlayerViaAntiCheat(int player, string reason, bool blacklist)
        {
            if (Settings.IsHost)
            {
                if (blacklist) BlacklistPlayer(Helpers.GetPlayerstateFromID(player), reason, false);
                MelonCoroutines.Start(QueueKick(player));
            }
        }

        private static IEnumerator QueueKick(int cheater)
        {
            yield return new WaitForEndOfFrame();
            if (GameReferences.Moderation == null) GameReferences.ResetReferences();
            GameReferences.Moderation!.RPC_KickPlayer(cheater);
            yield return new WaitForSeconds(0.25f);
            GameReferences.Runner!.Disconnect(cheater);
        }


        internal static async void FetchGlobalBlacklist()
        {
            GloballyBlacklistedPlayers.Clear();

            try
            {
                using (var Client = new System.Net.Http.HttpClient())
                {
                    string Data = await Client.GetStringAsync("https://jolynesbackend.xyz/anticheat/blacklistedusers");

                    foreach (string UserId in Data.Split(','))
                    {
                        string PlayerId = UserId.Trim();
                        if (!string.IsNullOrEmpty(PlayerId)) GloballyBlacklistedPlayers.Add(PlayerId);
                    }
                }
            }
            catch { }
        }


        internal static string Hash(string Text)
        {
            if (string.IsNullOrEmpty(Text)) return string.Empty;

            using (SHA256 Sha256 = SHA256.Create())
                return Convert.ToHexString(Sha256.ComputeHash(Encoding.UTF8.GetBytes(Text))).ToLowerInvariant();
        }

        public static bool IsWhitelisted(this PlayerState player)
        {
            if (!Settings.IsHost || player == null || GameReferences.Runner == null)
                return false;

            string userId = GameReferences.Runner.GetPlayerUserId(player.PlayerId);
            return !string.IsNullOrEmpty(userId) && WhitelistedPlayers.Contains(Hash(userId));
        }

        internal static void RestoreWhitelistedPlayer(string UserId)
        {
            if (string.IsNullOrEmpty(UserId)) return;
            string HashedId = Hash(UserId);
            WhitelistedPlayers.Add(HashedId);
            BlacklistedPlayers.Remove(UserId);
        }

        internal static void UnwhitelistUserId(string UserId)
        {
            if (string.IsNullOrEmpty(UserId)) return;
            WhitelistedPlayers.Remove(Hash(UserId));
            GUIManager.ForgetWhitelist(UserId);
        }

        public static bool IsBlacklisted(this PlayerState Player)
        {
            if (!Settings.IsHost || Player == null || GameReferences.Runner == null) return false;

            LoadData();
            string UserId = GameReferences.Runner.GetPlayerUserId(Player.PlayerId);
            return !string.IsNullOrEmpty(UserId) && (BlacklistedPlayers.ContainsKey(UserId) || GloballyBlacklistedPlayers.Contains(Hash(UserId)));
        }

        public static void WhitelistPlayer(PlayerState Player)
        {
            if (!Settings.IsHost || Player == null || GameReferences.Runner == null) return;

            string UserId = GameReferences.Runner.GetPlayerUserId(Player.PlayerId);
            if (string.IsNullOrEmpty(UserId)) return;

            string HashedId = Hash(UserId);
            WhitelistedPlayers.Add(HashedId);
            BlacklistedPlayers.Remove(HashedId);
            GUIManager.RememberWhitelist(Player, UserId);
        }

        public static void UnwhitelistPlayer(PlayerState Player)
        {
            if (!Settings.IsHost || Player == null || GameReferences.Runner == null) return;

            string UserId = GameReferences.Runner.GetPlayerUserId(Player.PlayerId);
            if (string.IsNullOrEmpty(UserId)) return;

            UnwhitelistUserId(UserId);
        }

        public static void BlacklistPlayer(PlayerState Player, string Reason, bool Kick = true)
        {
            if (!Settings.IsHost || Player == null || Player == GameReferences.Rig!.PState || GameReferences.Runner == null) return;

            LoadData();
            string UserId = GameReferences.Runner.GetPlayerUserId(Player.PlayerId);
            if (string.IsNullOrEmpty(UserId)) return;

            BlacklistedPlayers[UserId] = (Player.NetworkName.Value, Helpers.GetPlayerPlatform(Player), Reason);
            WhitelistedPlayers.Remove(Hash(UserId));
            GUIManager.ForgetWhitelist(UserId);
            SaveData();

            if (Kick) KickPlayerViaAntiCheat(Player.PlayerId, Reason, false);
        }




        private static bool DataLoaded;
        private static string DataPath => Path.Combine(MelonEnvironment.UserDataDirectory, "AntiCheat.dat");

        internal static int GlobalBlacklistCount => GloballyBlacklistedPlayers.Count;

        internal static IEnumerable<KeyValuePair<string, (string Username, string Platform, string Reason)>> GetBlacklistedPlayers()
        {
            LoadData();
            return BlacklistedPlayers;
        }

        internal static void RemoveBlacklistedPlayer(string UserId)
        {
            if (!Settings.IsHost || !BlacklistedPlayers.Remove(UserId)) return;
            SaveData();
        }

        internal static void SetBlacklistPersistence(bool Enabled)
        {
            if (!Settings.IsHost) return;
            Settings.PersistBlacklist = Enabled;
            SaveData();
        }


        internal static void LoadData()
        {
            if (DataLoaded) return;
            DataLoaded = true;

            if (!File.Exists(DataPath))
            {
                ImportOldData();
                return;
            }

            try
            {
                using FileStream Stream = File.OpenRead(DataPath);
                using BinaryReader Reader = new BinaryReader(Stream);
                int Key = Reader.ReadInt32();
                if (Key > 0 && Key < (int)UnityEngine.InputSystem.Key.IMESelected) Settings.GUIKey = (UnityEngine.InputSystem.Key)Key;
                Settings.PersistBlacklist = Reader.ReadBoolean();

                int Count = Reader.ReadInt32();
                if (Count < 0 || Count > 10000) throw new InvalidDataException();

                for (int Index = 0; Index < Count; Index++)
                {
                    string UserId = Reader.ReadString();
                    string Username = Reader.ReadString();
                    string Platform = Reader.ReadString();
                    string Reason = Reader.ReadString();
                    if (!string.IsNullOrEmpty(UserId)) BlacklistedPlayers[UserId] = (Username, Platform, Reason);
                }

                GUIManager.ReadWhitelist(Reader);
            }
            catch (Exception Exception)
            {
                MelonLogger.Error($"Failed to load data: {Exception.Message}");
            }
        }

        private static void ImportOldData()
        {
            string Folder = MelonEnvironment.UserDataDirectory;
            string KeybindPath = Path.Combine(Folder, "AntiCheatKeybind.dat");
            string SettingsPath = Path.Combine(Folder, "AntiCheatBlacklistSettings.dat");
            string BlacklistPath = Path.Combine(Folder, "AntiCheatBlacklist.dat");
            string WhitelistPath = Path.Combine(Folder, "AntiCheatWhitelist.dat");
            if (!File.Exists(KeybindPath) && !File.Exists(SettingsPath) && !File.Exists(BlacklistPath) && !File.Exists(WhitelistPath)) return;

            try
            {
                if (File.Exists(KeybindPath) && int.TryParse(File.ReadAllText(KeybindPath), out int Key) && Key > 0 && Key < (int)UnityEngine.InputSystem.Key.IMESelected)
                    Settings.GUIKey = (UnityEngine.InputSystem.Key)Key;

                if (File.Exists(SettingsPath))
                {
                    using BinaryReader Reader = new BinaryReader(File.OpenRead(SettingsPath));
                    Settings.PersistBlacklist = Reader.ReadBoolean();
                }

                if (File.Exists(BlacklistPath))
                {
                    using BinaryReader Reader = new BinaryReader(File.OpenRead(BlacklistPath));
                    int Count = Reader.ReadInt32();
                    if (Count < 0 || Count > 10000) throw new InvalidDataException();

                    for (int Index = 0; Index < Count; Index++)
                    {
                        string UserId = Reader.ReadString();
                        string Username = Reader.ReadString();
                        string Platform = Reader.ReadString();
                        string Reason = Reader.ReadString();
                        if (!string.IsNullOrEmpty(UserId)) BlacklistedPlayers[UserId] = (Username, Platform, Reason);
                    }
                }

                if (File.Exists(WhitelistPath))
                {
                    using BinaryReader Reader = new BinaryReader(File.OpenRead(WhitelistPath));
                    GUIManager.ReadWhitelist(Reader);
                }
            }
            catch (Exception Exception)
            {
                MelonLogger.Error($"Failed to import old data: {Exception.Message}");
            }

            SaveData();
        }

        internal static void SaveData()
        {
            try
            {
                Directory.CreateDirectory(MelonEnvironment.UserDataDirectory);
                using FileStream Stream = File.Create(DataPath);
                using BinaryWriter Writer = new BinaryWriter(Stream);
                Writer.Write((int)Settings.GUIKey);
                Writer.Write(Settings.PersistBlacklist);
                Writer.Write(Settings.PersistBlacklist ? BlacklistedPlayers.Count : 0);

                if (Settings.PersistBlacklist)
                {
                    foreach (KeyValuePair<string, (string Username, string Platform, string Reason)> Player in BlacklistedPlayers)
                    {
                        Writer.Write(Player.Key ?? string.Empty);
                        Writer.Write(Player.Value.Username ?? string.Empty);
                        Writer.Write(Player.Value.Platform ?? string.Empty);
                        Writer.Write(Player.Value.Reason ?? string.Empty);
                    }
                }

                GUIManager.WriteWhitelist(Writer);
            }
            catch (Exception Exception)
            {
                MelonLogger.Error($"Failed to save data: {Exception.Message}");
            }
        }








        internal static GameRole GetPlayerRole(PlayerRef player)
        {
            if (!player.IsValid) return GameRole.NotSet;

            foreach (var pair in GameReferences.Role!.gameRoleToPlayerIds)
                if (pair.Value.Contains(player)) return pair.Key;

            return GameRole.NotSet;
        }

        internal static IEnumerator CorrectLobbyDoors(GameStateManager manager)
        {
            if (DoorCorrectionPending) yield break;
            DoorCorrectionPending = true;

            try
            {
                yield return new WaitForSeconds(0.35f);

                if (manager == null || !Settings.IsHost || !Settings.AntiCheatEnabled)
                    yield break;

                bool close = manager.InLobbyState() || manager.InVotingState();
                manager.RPC_ToggleLobbyDoors(close);
            }
            finally
            {
                DoorCorrectionPending = false;
            }
        }
    }
}
