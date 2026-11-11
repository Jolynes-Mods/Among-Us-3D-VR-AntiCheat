using AntiCheat.Config;
using Il2CppSG.Airlock;
using Il2CppSG.Airlock.Network;
using Il2CppSG.Airlock.Roles;
using UnityEngine;

namespace AntiCheat.Managers
{
    internal class DebugGUI
    {
        internal static void Display()
        {
            if (!Settings.GUIEnabled || !Settings.DebugMode) return;

            if (GUI.Button(new Rect(20f, 50f, 160f, 30f), "No Cooldown"))
            {
                Settings.NoCooldown = !Settings.NoCooldown;
            }

            if (GUI.Button(new Rect(20f, 80f, 160f, 30f), "Become Imposter"))
            {
                GameReferences.Role!.AlterPlayerRole(Il2CppSG.Airlock.Roles.GameRole.Impostor, GameReferences.Runner!.LocalPlayer);
            }

            if (GUI.Button(new Rect(20f, 110f, 160f, 30f), "Start Game"))
            {
                GameReferences.Lobby!.RPC_StartGame();
                GameReferences.GameState!._preventMatchEnding.Value = true;
            }

            if (GUI.Button(new Rect(20f, 140f, 160f, 30f), "Revive All"))
            {
                foreach (PlayerState player in GameReferences.Spawn!.ActivePlayerStates)
                {
                    player.IsAlive = true;
                    player._IsAlive = true;
                }
            }

            if (GUI.Button(new Rect(20f, 170f, 160f, 30f), "Meeting"))
            {
                GameReferences.Vote!.RPC_CallVote(GameReferences.Runner!.LocalPlayer, false);
            }

            if (GUI.Button(new Rect(20f, 200f, 160f, 30f), "Body Meeting"))
            {
                GameReferences.Vote!.RPC_CallVote(GameReferences.Runner!.LocalPlayer.PlayerId, GameReferences.Runner!.LocalPlayer, false);
            }

            if (GUI.Button(new Rect(20f, 230f, 160f, 30f), "Body"))
            {
                GameReferences.Rig!.PState.LocomotionPlayer.SpawnBody();
            }

            if (GUI.Button(new Rect(20f, 260f, 160f, 30f), "Die"))
            {
                GameReferences.Killing!.RPC_TargetedAction(GameReferences.Runner.LocalPlayer, GameReferences.Runner.LocalPlayer, (int)ProximityTargetedAction.Kill);
            }

            if (GUI.Button(new Rect(20f, 290f, 160f, 30f), "vote"))
            {
                foreach (PlayerState player in GameReferences.Spawn!.ActivePlayerStates)
                {
                    if (player != GameReferences.Rig!.PState)
                        GameReferences.Vote!.RPC_Vote(player.PlayerId);
                }
            }

            if (GUI.Button(new Rect(20f, 320f, 160f, 30f), "open lobby doors"))
            {
                GameReferences.GameState!.RPC_ToggleLobbyDoors(false);
            }

            if (GUI.Button(new Rect(20f, 350f, 160f, 30f), "close lobby doors"))
            {
                GameReferences.GameState!.RPC_ToggleLobbyDoors(true);
            }
        }
    }
}
