using AntiCheat;
using AntiCheat.Config;
using AntiCheat.Managers.AntiCheat;
using HarmonyLib;
using Il2CppFusion;
using Il2CppSG.Airlock;
using System.Runtime.InteropServices;

[HarmonyPatch(typeof(VoteManager), nameof(VoteManager.RPC_CallVote), new Type[] { typeof(PlayerRef), typeof(NetworkBool), typeof(RpcInfo) })]
public static class CallVotePatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerRef sourcePlayer, NetworkBool forceVote, ref RpcInfo info)
    {
        if (Settings.IsHost && Settings.AntiCheatEnabled && Settings.CallMeetingValidateModule)
        {
            if (info.Source.IsNone || sourcePlayer == GameReferences.Runner?.LocalPlayer || info.Source == GameReferences.Runner?.LocalPlayer) return true;

            PlayerState caller = Helpers.GetPlayerstateFromID(sourcePlayer.PlayerId);

            if (!AntiCheatMain.VerifyMeeting(caller, ref info))
            {
                PlayerState InfoCaller = Helpers.GetPlayerstateFromID(info.Source.PlayerId);

                if (InfoCaller != null)
                    AntiCheatMain.CheaterDetected(InfoCaller, "Illegal meeting call");

                return false;
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(VoteManager), nameof(VoteManager.RPC_CallVote), new Type[] { typeof(int), typeof(PlayerRef), typeof(NetworkBool), typeof(RpcInfo) })]
public static class CallVotePatch2
{
    [HarmonyPrefix]
    public static bool Prefix(int foundPlayer, PlayerRef sourcePlayer, NetworkBool forceVote, [Optional] ref RpcInfo info)
    {
        if (Settings.IsHost && Settings.AntiCheatEnabled && Settings.CallBodyReportValidateModule)
        {
            if (info.Source.IsNone || sourcePlayer == GameReferences.Runner?.LocalPlayer || info.Source == GameReferences.Runner?.LocalPlayer) return true;

            PlayerState caller = Helpers.GetPlayerstateFromID(sourcePlayer.PlayerId);
            PlayerState body = Helpers.GetPlayerstateFromID(foundPlayer);

            if (!AntiCheatMain.VerifyBodyReport(caller, body, ref info))
            {
                PlayerState InfoCaller = Helpers.GetPlayerstateFromID(info.Source.PlayerId);

                if (InfoCaller != null)
                    AntiCheatMain.CheaterDetected(InfoCaller, "Illegal body report");

                return false;
            }
        }

        return true;
    }
}