using AntiCheat.Config;
using Il2CppSG.Airlock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiCheat
{
    internal class Helpers
    {
        public static PlayerState GetPlayerstateFromID(int id)
        {
            foreach (PlayerState player in GameReferences.Spawn!.ActivePlayerStates)
                if (player.PlayerId == id)
                    return player;

            return null;
        }

        public static string GetPlayerPlatform(PlayerState player)
        {
            string UserId = GameReferences.Runner!.GetPlayerUserId(player.PlayerId);
            if (string.IsNullOrEmpty(UserId)) return "unknown";

            if (UserId.StartsWith("Steam") && player.Is3DPlayer)
                return "Steam 3D";
            if (UserId.StartsWith("Steam") && !player.Is3DPlayer)
                return "Steam VR";
            if (UserId.StartsWith("Meta"))
                return "Meta Quest";
            if (UserId.StartsWith("PS5"))
                return "Playstation VR";
            if (UserId.StartsWith("Pico"))
                return "Pico VR";
            return "unknown";
        }
    }
}
