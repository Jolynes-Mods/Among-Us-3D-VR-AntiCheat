using UnityEngine.InputSystem;

namespace AntiCheat.Config
{
    internal class Settings
    {
        public const string Version = "1.2.0";
        public static bool Outdated = false;
        internal static bool IgnoreUpdate { get; set; }
        internal static bool DebugMode { get; set; } = false;
        internal static bool KickCheaters { get; set; } = true;
        internal static bool InGame { get; set; } = false;
        internal static bool IsHost { get; set; } = false;
        internal static bool AntiCheatEnabled { get; set; } = true;
        internal static bool GUIEnabled { get; set; } = false;
        internal static Key GUIKey { get; set; } = Key.LeftCtrl;


        internal static bool NoCooldown { get; set; } = false;


        internal static bool CosmeticValidateModule { get; set; } = true;
        internal static bool CallMeetingValidateModule { get; set; } = true;
        internal static bool CallBodyReportValidateModule { get; set; } = true;
        internal static bool TaskCompletedValidateModule { get; set; } = true;
        internal static bool KickVoteValidateModule { get; set; } = true;
        internal static bool RequestPowerupValidateModule { get; set; } = true;
        internal static bool UsernameValidateModule { get; set; } = true;
        internal static bool SpawnBodyValidateModule { get; set; } = true;
        internal static bool PlayerJoinValidateModule { get; set; } = true;
        internal static bool TakePhotoValidateModule { get; set; } = true;
        internal static bool TargetedActionValidateModule { get; set; } = true;
        internal static bool ToggleLobbyDoorValidateModule { get; set; } = true;
        internal static bool UsePowerupValidateModule { get; set; } = true;
        internal static bool VentValidateModule { get; set; } = true;
        internal static bool VoteValidateModule { get; set; } = true;
        internal static bool PlayerSpeedValidateModule { get; set; } = true;
        internal static bool PlayerBlacklistModule { get; set; } = true;
        internal static bool PersistBlacklist { get; set; } = false;
        internal static bool BlacklistOnDetect { get; set; } = true;
    }
}
