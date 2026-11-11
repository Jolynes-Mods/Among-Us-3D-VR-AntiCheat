using AntiCheat.Config;
using AntiCheat.Managers;
using AntiCheat.Managers.AntiCheat;
using MelonLoader;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(AntiCheat.Main), "Anti-Cheat Mod", Settings.Version, "Jolyne")]
[assembly: MelonGame("Innersloth", null)]
namespace AntiCheat
{
    public class Main : MelonMod
    {
        public override void OnInitializeMelon()
        {
#if DEBUG
            Settings.DebugMode = true;
#else
            Settings.DebugMode = false;
#endif
            AntiCheatMain.ResetAntiCheat(true);
            AutoUpdateManager.Check();
            Logger.Msg("Anti-Cheat loaded");
            Logger.Msg($"Anti-Cheat UI toggle: {Settings.GUIKey}");
        }


        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            Settings.InGame = sceneName != "Boot" && sceneName != "Title";
            AntiCheatMain.ResetAntiCheat(true);
            Logger.DebugMsg($"Scene {sceneName}");
        }

        public override void OnGUI()
        {
            DebugGUI.Display();
            GUIManager.Display();
        }

        public override void OnUpdate()
        {
            if (GUIManager.Binding)
            {
                for (Key key = Key.Space; key < Key.IMESelected; key++)
                {
                    if (!Keyboard.current[key].wasPressedThisFrame) continue;
                    GUIManager.SetKeybind(key);
                    break;
                }
            }
            else if (Keyboard.current[Settings.GUIKey].wasPressedThisFrame)
                Settings.GUIEnabled = !Settings.GUIEnabled;

            if (Settings.InGame && GameReferences.Rig == null) GameReferences.ResetReferences();

            AntiCheatMain.Update();
        }
    }
}

/* done
 *
 * Kicks cheaters with anti kick
 * Kill verification
 * Infect verification
 * Stun Powerup verification
 * Guard Powerup verification
 * SheriffVote verification
 * Meeting verification
 * Body Report verification
 * Body Spawn verification
 * vote verification
 * KickVote Verification
 * TakePhoto Verification
 * Cosmetic Change Verification
 * Join Verification
 * Name Change Verification
 * Venting verification
 * Wardrobe enter verification
 * Use Powerup verification
 * Speed Hack detection
 * Task completion verification
 * Lobby Door toggle verification
 * Anti task reset exploit
*/