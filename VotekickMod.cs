using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using System;
using Il2CppInterop.Runtime;
using InnerNet;

namespace VotekickMod
{
[BepInPlugin("com.votekick.mod", "Votekick Mod", "1.1.0")]
public class VotekickPlugin : BasePlugin
{
public static ManualLogSource Logger;
public static bool showGui = false;

    public override void Load()
    {
        Logger = Log;
        var harmony = new Harmony("com.votekick.mod");
        harmony.PatchAll();

        AddComponent<VotekickMenu>();
        AddComponent<ImmortalityHandler>();
    }

    public class VotekickMenu : MonoBehaviour
    {
        public VotekickMenu(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                showGui = !showGui;
                Cursor.visible = showGui;
                Cursor.lockState = showGui ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }

        private void OnGUI()
        {
            if (!showGui) return;

            GUI.Box(new Rect(10, 10, 250, 500), "Mod Menu");

            if (GUI.Button(new Rect(20, 40, 230, 30), "Votekick All"))
            {
                VotekickAll();
            }

            int yOffset = 80;
            var players = PlayerControl.AllPlayerControls;
            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (p == null || p.AmOwner || p.Data == null) continue;

                    if (GUI.Button(new Rect(20, yOffset, 230, 25), "Kick " + p.Data.PlayerName))
                    {
                        SendKick(p.Data.ClientId);
                    }
                    yOffset += 30;
                }
            }
        }

        private void VotekickAll()
        {
            if (VoteBanSystem.Instance == null) return;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p != null && !p.AmOwner && p.Data != null) SendKick(p.Data.ClientId);
            }
        }

        private void SendKick(int clientId)
        {
            if (VoteBanSystem.Instance == null) return;
            VoteBanSystem.Instance.CmdAddVote(clientId);
            VoteBanSystem.Instance.CmdAddVote(clientId);
            VoteBanSystem.Instance.CmdAddVote(clientId);
        }
    }

    public class ImmortalityHandler : MonoBehaviour
    {
        public ImmortalityHandler(IntPtr ptr) : base(ptr) { }

        public static bool Active = false;
        private float timer = 0f;

        void Update()
        {
            timer += Time.deltaTime;
            if (timer < 2.0f) return;
            timer = 0f;

            if (PlayerControl.LocalPlayer?.Data != null && !PlayerControl.LocalPlayer.Data.IsDead)
            {
                if (!Active) Toggle(true);
            }
            else if (Active)
            {
                Toggle(false);
            }
        }

        public static void Toggle(bool state)
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.inVent) return;
            VentilationSystem.Update(state ? VentilationSystem.Operation.Enter : VentilationSystem.Operation.Exit, 50);
            Active = state;
        }
    }

    [HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Update))]
    class VentPatch {
        static bool Prefix(int ventId) {
            if (ImmortalityHandler.Active && ventId != 50) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    class MeetingPatch {
        static void Postfix() {
            if (ImmortalityHandler.Active) ImmortalityHandler.Toggle(true);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    class MurderPatch {
        static void Postfix(PlayerControl target) {
            if (ImmortalityHandler.Active && target == PlayerControl.LocalPlayer) {
                Logger.LogInfo("Blocked kill attempt.");
            }
        }
    }
}
}
