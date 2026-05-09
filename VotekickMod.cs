using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using System;
using InnerNet;

namespace VotekickMod
{
    [BepInPlugin("com.votekick.mod", "Votekick Mod", "1.0.0")]
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
            AddComponent<ImmortalityLogic>();
        }

        public class VotekickMenu : MonoBehaviour
        {
            public VotekickMenu(IntPtr ptr) : base(ptr) { }

            private Rect windowRect = new Rect(20, 20, 250, 500);

            private void Update()
            {
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    showGui = !showGui;
                }
            }

            private void OnGUI()
            {
                if (!showGui) return;
                windowRect = GUI.Window(0, windowRect, (GUI.WindowFunction)DrawWindow, "Votekick Mod");
            }

            private void DrawWindow(int windowID)
            {
                if (GUI.Button(new Rect(20, 40, 210, 30), "Votekick All"))
                {
                    VotekickAllOnce();
                    DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage("Votekicked Everyone");
                }

                int yOffset = 80;
                var players = PlayerControl.AllPlayerControls;
                if (players != null)
                {
                    for (int i = 0; i < players.Count; i++)
                    {
                        var p = players[i];
                        if (p == null || p.AmOwner || p.Data == null) continue;
                        if (GUI.Button(new Rect(20, yOffset, 210, 25), "Kick " + p.Data.PlayerName))
                        {
                            SendKick(p.Data.ClientId);
                            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage("Votekicked " + p.Data.PlayerName);
                        }
                        yOffset += 30;
                    }
                }

                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
            }

            private void VotekickAllOnce()
            {
                if (VoteBanSystem.Instance == null) return;
                var players = PlayerControl.AllPlayerControls;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (p != null && !p.AmOwner && p.Data != null) SendKick(p.Data.ClientId);
                }
            }

            private void SendKick(int clientId)
            {
                if (VoteBanSystem.Instance == null) return;
                VoteBanSystem.Instance.AddVote(clientId);
                VoteBanSystem.Instance.AddVote(clientId);
                VoteBanSystem.Instance.AddVote(clientId);
            }
        }

        public class ImmortalityLogic : MonoBehaviour
        {
            public ImmortalityLogic(IntPtr ptr) : base(ptr) { }

            private static readonly int CUSTOM_VENT_ID = 50;
            public static bool _enabled = false;
            private float _checkTimer = 0f;

            public static bool Enabled
            {
                get => _enabled;
                set
                {
                    if (value == _enabled) return;
                    if (PlayerControl.LocalPlayer != null && !PlayerControl.LocalPlayer.inVent)
                    {
                        if (value) VentilationSystem.Update(VentilationSystem.Operation.Enter, CUSTOM_VENT_ID);
                        else VentilationSystem.Update(VentilationSystem.Operation.Exit, CUSTOM_VENT_ID);
                    }
                    _enabled = value;
                }
            }

            private void Update()
            {
                _checkTimer += Time.deltaTime;
                if (_checkTimer < 2.0f) return;
                _checkTimer = 0f;

                if (PlayerControl.LocalPlayer?.Data != null && !PlayerControl.LocalPlayer.Data.IsDead)
                {
                    if (!Enabled) Enabled = true;
                }
                else
                {
                    if (Enabled) Enabled = false;
                }
            }
        }

        [HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Update))]
        class BlockSendingUpdates
        {
            static bool Prefix(VentilationSystem.Operation op, int ventId)
            {
                if (ventId != 50 && ImmortalityLogic.Enabled && (op == VentilationSystem.Operation.Enter || op == VentilationSystem.Operation.Exit || op == VentilationSystem.Operation.Move))
                {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake))]
        class OnShipStatusCreate
        {
            static void Prefix()
            {
                ImmortalityLogic._enabled = false;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        class OnMurder
        {
            static void Postfix(PlayerControl __instance, PlayerControl target)
            {
                if (ImmortalityLogic.Enabled && target == PlayerControl.LocalPlayer)
                {
                    DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(__instance.Data.PlayerName + " Attempted to kill you but failed :D");
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
        class OnMeetingEnd
        {
            static void Postfix()
            {
                if (!ImmortalityLogic.Enabled || PlayerControl.LocalPlayer.Data.IsDead) return;
                VentilationSystem.Update(VentilationSystem.Operation.Enter, 50);
            }
        }
    }
}
