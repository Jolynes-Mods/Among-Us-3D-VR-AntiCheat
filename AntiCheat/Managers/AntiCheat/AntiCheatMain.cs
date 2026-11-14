using AntiCheat.Config;
using Il2CppFusion;
using Il2CppSG.Airlock;
using Il2CppSG.Airlock.Network;
using Il2CppSG.Airlock.Roles;
using MelonLoader;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AntiCheat.Managers.AntiCheat
{
    internal class AntiCheatMain
    {
        private static readonly Dictionary<int, int> MeetingsCalled = new();
        private static readonly Dictionary<PlayerState, int> FailedSpeedAttempts = new Dictionary<PlayerState, int>();
        private static readonly Dictionary<PlayerState, Vector3> PrevPosition = new Dictionary<PlayerState, Vector3>();
        private static readonly Dictionary<PlayerState, int> SpeedDetections = new Dictionary<PlayerState, int>();
        private static readonly Dictionary<PlayerState, int> PositionViolations = new Dictionary<PlayerState, int>();
        private static readonly HashSet<int> AllowedBodySpawns = new HashSet<int>();
        private static float SpeedTimer;
        private static float _checkTimer = 0f;

        internal static void Update()
        {
            if (!Settings.InGame || GameReferences.Rig == null)
            {
                _checkTimer = 0f;
                SpeedTimer = 0f;
                PrevPosition.Clear();
                SpeedDetections.Clear();
                return;
            }

            Settings.IsHost = GameReferences.Rig.PState.PlayerId == 9;
            CheckSpeed();

            if (Settings.NoCooldown && GameReferences.Rig.PState.ActionCooldownRemaining != 0)
                GameReferences.Killing!.SetMaxCooldown(0);

            _checkTimer += Time.deltaTime;
            if (_checkTimer >= 10f)
            {
                _checkTimer = 0f;

                try
                {
                    CheckPlayers();
                }
                catch (System.Exception ex)
                {
                    MelonLogger.Error($"Error occurred during CheckPlayers: {ex}");
                }
            }
        }





        private static void CheckPlayers()
        {
            if (!Settings.IsHost || !Settings.AntiCheatEnabled) return;
            foreach (PlayerState player in GameReferences.Spawn!.ActivePlayerStates)
            {
                if (player == null || !player.IsConnected || !player.IsSpawned) continue;

                if (Settings.PlayerBlacklistModule && player.IsBlacklisted() && !player.IsWhitelisted()) CheaterDetected(player, "Player is on the blacklist", false);
                if (Settings.CosmeticValidateModule && player.HatId == 98) CheaterDetected(player, "Illegal cosmetics");
                if (Settings.UsernameValidateModule && !VerifyName(player, player.NetworkName.Value)) CheaterDetected(player, "Illegal name");
            }
        }




        private static void CheckSpeed()
        {
            if (!Settings.IsHost || !Settings.AntiCheatEnabled || !Settings.PlayerSpeedValidateModule || GameReferences.Spawn == null)
            {
                SpeedTimer = 0f;
                PrevPosition.Clear();
                SpeedDetections.Clear();
                return;
            }

            SpeedTimer += Time.unscaledDeltaTime;
            if (SpeedTimer < 0.5f) return;

            float elapsed = SpeedTimer;
            SpeedTimer = 0f;

            if (elapsed > 1f)
            {
                PrevPosition.Clear();
                SpeedDetections.Clear();
                return;
            }

            float limit = 9.5f * elapsed + 0.5f;

            foreach (PlayerState player in GameReferences.Spawn.ActivePlayerStates)
            {
                if (player == null) continue;

                var body = player.LocomotionPlayer?.NetworkRigidbody?.Rigidbody;
                if (!player.IsConnected || !player.IsSpawned || body == null)
                {
                    PrevPosition.Remove(player);
                    SpeedDetections.Remove(player);
                    continue;
                }

                Vector3 position = body.position;
                bool tracked = PrevPosition.TryGetValue(player, out Vector3 previous);
                PrevPosition[player] = position;

                if (!tracked || (position - previous).sqrMagnitude <= limit * limit)
                {
                    SpeedDetections.Remove(player);
                    continue;
                }

                SpeedDetections.TryGetValue(player, out int violations);
                if (++violations < 4)
                {
                    SpeedDetections[player] = violations;
                    continue;
                }

                SpeedDetections.Remove(player);
                CheaterDetected(player, "Speed Hacks Detected");
            }
        }




        internal static void ResetAntiCheat(bool sceneReset = false)
        {
            Logger.DebugMsg("Resetting Anti-Cheat");
            Commands.LoadData();
            MeetingsCalled.Clear();
            PositionViolations.Clear();
            SpeedDetections.Clear();
            PrevPosition.Clear();
            FailedSpeedAttempts.Clear();
            AllowedBodySpawns.Clear();
            CompleteTaskPatch.Tasks.Clear();
            KickVotePatch.RecentKickVotes.Clear();
            SpeedTimer = 0f;

            if (sceneReset)
            {
                Commands.FetchGlobalBlacklist();
            }
        }

        internal static void CheaterDetected(PlayerState cheater, string reason, bool blacklist = true)
        {
            if (Settings.IsHost && !cheater.IsWhitelisted())
            {
                Logger.Warning($"Kicking {cheater.NetworkName.Value} for cheating. Reason: {reason}");
                if (Settings.KickCheaters) Commands.KickPlayerViaAntiCheat(cheater.PlayerId, reason, Settings.BlacklistOnDetect && blacklist);
            }
        }



        internal static bool VerifyKill(PlayerState killer, PlayerState victim)
        {
            if (killer == null || victim == null) return false;

            if (!victim.IsAlive || killer == victim)
                return false;

            if (GameReferences.GameState!.GameModeStateValue.GameMode == GameModes.Infection)
                return false;

            if (!GameReferences.GameState!.InTaskState())
                return false;

            GameRole KillerRole = Commands.GetPlayerRole(killer.PlayerId);
            GameRole VictimRole = Commands.GetPlayerRole(victim.PlayerId);

            if (!killer.IsAlive && KillerRole != GameRole.Revenger)
                return false;

            if (KillerRole != GameRole.Impostor && KillerRole != GameRole.Revenger && KillerRole != GameRole.Vigilante)
                return false;

            if (VictimRole == GameRole.Impostor || VictimRole == GameRole.Revenger)
                return false;

            if ((killer.LocomotionPlayer.RigidbodyPosition - victim.LocomotionPlayer.RigidbodyPosition).sqrMagnitude > 10f)
                return false;

            if (killer.ActionCooldownRemaining > 0.1f)
                return false;

            if (!AllowedBodySpawns.Contains(victim.PlayerId))
                AllowedBodySpawns.Add(victim.PlayerId);
  
            return true;
        }



        internal static bool VerifyInfect(PlayerState tagger, PlayerState victim)
        {
            if (tagger == null || victim == null) return false;
            GameRole victimRole = Commands.GetPlayerRole(victim.PlayerId);

            if (GameReferences.GameState!.GameModeStateValue.GameMode != GameModes.Infection)
                return false;

            if (victimRole == GameRole.Infected || tagger == victim)
                return false;

            if (!GameReferences.GameState!.InTaskState())
                return false;

            GameRole TaggerRole = Commands.GetPlayerRole(tagger.PlayerId);


            if (TaggerRole != GameRole.Infected)
                return false;

            if ((tagger.LocomotionPlayer.RigidbodyPosition - victim.LocomotionPlayer.RigidbodyPosition).sqrMagnitude > 10f)
                return false;

            if (tagger.ActionCooldownRemaining > 0.1f)
                return false;

            return true;
        }

        internal static bool VerifyMeeting(PlayerState caller, ref RpcInfo info)
        {
            if (caller == null)
                return false;

            if (info.Source.IsNone && caller.PlayerId == 9)
                info.Source = GameReferences.Runner!.LocalPlayer;

            PlayerState InfoCaller = Helpers.GetPlayerstateFromID(info.Source.PlayerId);

            if (InfoCaller == null || InfoCaller != caller)
                return false;

            if (!caller.IsAlive)
                return false;

            if (GameReferences.Button == null || GameReferences.GameState == null) GameReferences.ResetReferences();
            if (GameReferences.Button == null || GameReferences.GameState == null) return true;

            if (GameReferences.Button._inSabotage)
                return false;

            if (!GameReferences.GameState.InTaskState() ||
                GameReferences.GameState.GameModeStateValue.GameMode == GameModes.Infection)
                return false;

            MeetingsCalled.TryGetValue(info.Source.PlayerId, out int called);

            if (called >= GameReferences.Button._maxAllowedCalls)
                return false;

            if ((caller.LocomotionPlayer.RigidbodyPosition -
                 GameReferences.Button._buttonCollider.transform.position).sqrMagnitude > 100f)
                return false;

            MeetingsCalled[info.Source.PlayerId] = called + 1;
            return true;
        }

        internal static bool VerifyBodyReport(PlayerState caller, PlayerState reported, ref RpcInfo info)
        {
            if (caller == null || reported == null)
                return false;

            if (info.Source.IsNone && caller.PlayerId == 9)
                info.Source = GameReferences.Runner!.LocalPlayer;

            PlayerState InfoCaller = Helpers.GetPlayerstateFromID(info.Source.PlayerId);

            if (InfoCaller == null || InfoCaller != caller)
                return false;

            if (reported.IsAlive || !caller.IsAlive)
                return false;

            if (!GameReferences.Spawn!._playerIdToBody.TryGetValue(reported.PlayerId, out var BodyObj))
                return false;

            NetworkedBody body = BodyObj.GetComponent<NetworkedBody>();

            if (body == null || !body._playerBody.active)
                return false;

            if ((caller.LocomotionPlayer.RigidbodyPosition - body._playerBody.transform.position).sqrMagnitude > 13f)
                return false;

            if (GameReferences.GameState!.GameModeStateValue.GameMode == GameModes.Infection)
                return false;

            if (!GameReferences.GameState.InTaskState())
                return false;

            return true;
        }


        internal static bool VerifyBodySpawn(PlayerRef player, NetworkRigidbody rigidbody)
        {
            if (GameReferences.Spawn == null)
                return false;

            PlayerState BodyPState = Helpers.GetPlayerstateFromID(player.PlayerId);

            if (!player.IsValid)
                return false;

            if (BodyPState == null)
                return false;

            if (rigidbody != BodyPState.LocomotionPlayer.NetworkRigidbody)
                return false;

            if (BodyPState.IsAlive)
                return false;

            if (GameReferences.GameState == null)
                return false;

            if (GameReferences.GameState.GameModeStateValue.GameMode == GameModes.Infection)
                return false;

            if (!GameReferences.GameState.InTaskState())
                return false;

            if (!AllowedBodySpawns.Contains(BodyPState.PlayerId))
            {
                MelonLogger.Warning("Not in hashset");
                return false;
            }
            AllowedBodySpawns.Remove(BodyPState.PlayerId);
            return true;
        }

        internal static bool VerifySkipVote(PlayerRef voter, ref RpcInfo info)
        {
            if (info.Source.IsNone)
                info.Source = GameReferences.Runner!.LocalPlayer;

            PlayerState InfoVoter = Helpers.GetPlayerstateFromID(info.Source.PlayerId);

            if (InfoVoter == null)
                return false;

            if (voter != info.Source)
                return false;

            if (GameReferences.GameState == null || !GameReferences.GameState.InVotingState())
                return false;

            if (!InfoVoter.IsAlive)
                return false;

            return true;
        }

        internal static bool VerifyVote(PlayerRef voter, PlayerRef voted, ref RpcInfo info)
        {
            if (info.Source.IsNone)
                info.Source = GameReferences.Runner!.LocalPlayer;

            PlayerState InfoVoter = Helpers.GetPlayerstateFromID(info.Source.PlayerId);
            PlayerState VoterState = Helpers.GetPlayerstateFromID(voter.PlayerId);
            PlayerState VotedState = Helpers.GetPlayerstateFromID(voted.PlayerId);

            if (InfoVoter == null || VoterState == null || VotedState == null)
                return false;

            if (voter != info.Source)
                return false;

            if (GameReferences.GameState == null || !GameReferences.GameState.InVotingState())
                return false;

            if (!InfoVoter.IsAlive || !VotedState.IsAlive)
                return false;

            return true;
        }


        internal static bool VerifyCosmeticChange(NetworkedLocomotionPlayer instance, int cosmetic, int player)
        {
            if (instance.PState.PlayerId != player)
                return false;

            int count = GameReferences.Customization!._elementCollection.AllCustomizationElements.Count;

            if (cosmetic < 0 || cosmetic >= count)
                return false;

            if (cosmetic == 98)
                return false;
            return true;
        }

        internal static bool VerifyKickVote(int source, int voted)
        {
            if (source < 0 || source > 9 || voted < 0 || voted > 9 || source == voted)
                return false;

            PlayerState Source = Helpers.GetPlayerstateFromID(source);
            PlayerState Voted = Helpers.GetPlayerstateFromID(voted);

            if (Source == null || Voted == null || Source.IsSpectating || Voted.IsSpectating)
                return false;

            return true;
        }

        internal static bool VerifyDeputyVote(PlayerState deputy, PlayerState voted)
        {
            if (deputy == null || voted == null) return false;

            if (!voted.IsAlive)
                return false;

            if (deputy == voted)
                return false;

            if (!GameReferences.GameState!.InVotingState())
                return false;

            if (GameReferences.Vote!.SheriffId != deputy.PlayerId || Commands.GetPlayerRole(deputy.PlayerId) != GameRole.Sheriff)
                return false;

            if ((deputy.LocomotionPlayer.RigidbodyPosition - voted.LocomotionPlayer.RigidbodyPosition).sqrMagnitude > 10f)
                return false;

            return true;
        }

        internal static bool VerifyJoin(NetworkedLocomotionPlayer joining, string UserId, int hat)
        {
            if (hat == 98)
                return false;

            if (joining.Runner.GetPlayerUserId(joining.PState.PlayerId) != UserId)
                return false;

            return true;
        }

        internal static bool VerifyName(PlayerState player, string name)
        {
            if (player == null || string.IsNullOrEmpty(name))
                return false;

            if (name.Length > 17)
                return false;

            if (Regex.IsMatch(name, @"[@$%\^&()<>+=]"))
                return false;
            return true;
        }

        internal static bool VerifyVentEnter(NetworkedLocomotionPlayer player)
        {
            if (player == null) return false;

            GameRole VenterRole = Commands.GetPlayerRole(player.PState.PlayerId);

            if (VenterRole != GameRole.Impostor && VenterRole != GameRole.Engineer && player.PState.ActivePowerUps != PowerUps.CanVent)
                return false;

            return true;
        }

        internal static bool VerifyVentExit(NetworkedLocomotionPlayer player)
        {
            if (player == null) return false;

            GameRole VenterRole = Commands.GetPlayerRole(player.PState.PlayerId);

            if (VenterRole != GameRole.Impostor && VenterRole != GameRole.Engineer && player.PState.ActivePowerUps != PowerUps.CanVent)
                return false;
            return true;
        }

        internal static bool VerifyUsePowerup(PlayerState player)
        {
            if (player == null) return false;

            GameRole Role = Commands.GetPlayerRole(player.PlayerId);

            if (!GameReferences.GameState!.InTaskState() || GameReferences.GameState!.GameModeStateValue.GameMode != GameModes.Infection || Role == GameRole.Infected || player.ActivePowerUps == PowerUps.None)
                return false;
            return true;
        }

        internal static bool VerifyTaskComplete(PlayerState player)
        {
            if (player == null) return false;

            GameRole Role = Commands.GetPlayerRole(player.PlayerId);

            if (player.IsSpectating || !GameReferences.GameState!.InTaskState())
                return false;

            if (Role == GameRole.Impostor || Role == GameRole.Revenger || Role == GameRole.Infected)
                return false;

            return true;
        }

        internal static bool VerifyToggleLobbyDoors(GameStateManager instance, bool closed)
        {
            if (closed)
            {
                if (!instance.InLobbyState() && !instance.InVotingState()) return false;
            }
            else
            {
                if (instance.InLobbyState() || instance.InVotingState()) return false;
            }
            return true;
        }


        internal static bool VerifyPowerup(PlayerState player, PlayerState target, PowerUps powerUp)
        {
            if (player == null || target == null) return false;
            GameRole PowerUserRole = Commands.GetPlayerRole(player.PlayerId);
            GameRole TargetRole = Commands.GetPlayerRole(target.PlayerId);

            if ((player.LocomotionPlayer.RigidbodyPosition - target.LocomotionPlayer.RigidbodyPosition).sqrMagnitude > 10f)
                return false;

            switch (powerUp)
            {
                case PowerUps.Stun:
                    {
                        if (player.ActivePowerUps != PowerUps.Stun) return false;

                        if (GameReferences.GameState!.GameModeStateValue.GameMode != GameModes.Infection)
                            return false;

                        if (PowerUserRole != GameRole.Crewmember || TargetRole != GameRole.Infected)
                            return false;

                        if (!GameReferences.GameState.InTaskState())
                            return false;
                    }
                    break;

                case PowerUps.Guard:
                    {
                        if (GameReferences.GameState!.GameModeStateValue.GameMode == GameModes.Infection && player.ActivePowerUps != PowerUps.Guard)
                            return false;

                        if (GameReferences.GameState!.GameModeStateValue.GameMode != GameModes.Infection && (PowerUserRole != GameRole.GuardianAngel || player.IsAlive || !target.IsAlive))
                            return false;

                        if (!GameReferences.GameState.InTaskState())
                            return false;
                    }
                    break;
            }
            return true;
        }
    }
}
