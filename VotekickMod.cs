using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using System;
using InnerNet;
using System.Collections.Generic;

namespace VotekickMod
{
    [BepInPlugin("com.votekick.mod", "Votekick Mod", "1.0.0")]
    public class VotekickPlugin : BasePlugin
    {
        public static ManualLogSource Logger;
        public static bool showGui = false;
        public static bool forceHostActive = false;

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

                if (forceHostActive)
                {
                    if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                    {
                        forceHostActive = false;
                        if (DestroyableSingleton<HudManager>.Instance?.Notifier != null)
                            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage("Got Host :D");
                    }
                    else
                    {
                        VotekickAllDirect();
                    }
                }
            }

            private void OnGUI()
            {
                if (!showGui) return;
                windowRect = GUI.Window(0, windowRect, (GUI.WindowFunction)DrawWindow, "Votekick Mod");
            }

            private void DrawWindow(int windowID)
            {
                if (GUI.Button(new Rect(20, 40, 210, 30), "Votekick All With Rejoin"))
                {
                    VotekickAllDirect();
                    RejoinLobby();
                }

                if (GUI.Button(new Rect(20, 80, 210, 30), "Votekick All Without Rejoin"))
                {
                    VotekickAllDirect();
                }

                if (GUI.Button(new Rect(20, 120, 210, 30), forceHostActive ? "Force Host: On" : "Force Host: Off"))
                {
                    forceHostActive = !forceHostActive;
                }

                if (GUI.Button(new Rect(20, 160, 210, 30), ImmortalityLogic.ModEnabled ? "Immortality: On" : "Immortality: Off"))
                {
                    ImmortalityLogic.ModEnabled = !ImmortalityLogic.ModEnabled;
                }

                int yOffset = 200;
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
                            if (DestroyableSingleton<HudManager>.Instance?.Notifier != null)
                                DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage("Votekicked " + p.Data.PlayerName);
                        }
                        yOffset += 30;
                    }
                }

                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
            }

            private void VotekickAllDirect()
            {
                if (VoteBanSystem.Instance == null) return;
                var players = PlayerControl.AllPlayerControls;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (p != null && !p.AmOwner && p.Data != null) SendKick(p.Data.ClientId);
                }
            }

            private void SendKick(int targetClientId)
            {
                if (VoteBanSystem.Instance == null) return;
                VoteBanSystem.Instance.CmdAddVote(targetClientId);
                VoteBanSystem.Instance.CmdAddVote(targetClientId);
                VoteBanSystem.Instance.CmdAddVote(targetClientId);
            }

            private void RejoinLobby()
            {
                if (AmongUsClient.Instance != null)
                {
                    string code = AmongUsClient.Instance.GameCode;
                    if (string.IsNullOrEmpty(code) && GameStartManager.Instance != null)
                    {
                        code = GameStartManager.Instance.LastJoinCode;
                    }

                    AmongUsClient.Instance.ExitGame(0);

                    if (!string.IsNullOrEmpty(code))
                    {
                        this.StartCoroutine(DoRejoin(code));
                    }
                }
            }

            private System.Collections.IEnumerator DoRejoin(string code)
            {
                yield return new WaitForSeconds(0.5f);
                AmongUsClient.Instance.ConnectToGame(code);
            }
        }

        public class ImmortalityLogic : MonoBehaviour
        {
            public ImmortalityLogic(IntPtr ptr) : base(ptr) { }

            private static readonly int CUSTOM_VENT_ID = 50;
            public static bool ModEnabled = false;
            public static bool _internalState = false;
            private float _checkTimer = 0f;

            private void Update()
            {
                _checkTimer += Time.deltaTime;
                if (_checkTimer < 1.0f) return;
                _checkTimer = 0f;

                if (ModEnabled && PlayerControl.LocalPlayer != null && !PlayerControl.LocalPlayer.Data.IsDead)
                {
                    if (!_internalState)
                    {
                        VentilationSystem.Update(VentilationSystem.Operation.Enter, CUSTOM_VENT_ID);
                        _internalState = true;
                    }
                }
                else
                {
                    if (_internalState)
                    {
                        if (PlayerControl.LocalPlayer != null) VentilationSystem.Update(VentilationSystem.Operation.Exit, CUSTOM_VENT_ID);
                        _internalState = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.Update))]
        class BlockSendingUpdates
        {
            static bool Prefix(VentilationSystem.Operation op, int ventId)
            {
                if (ventId != 50 && ImmortalityLogic.ModEnabled && ImmortalityLogic._internalState)
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
                ImmortalityLogic._internalState = false;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        class OnMurder
        {
            static void Postfix(PlayerControl __instance, PlayerControl target)
            {
                if (ImmortalityLogic.ModEnabled && target == PlayerControl.LocalPlayer)
                {
                    if (DestroyableSingleton<HudManager>.Instance?.Notifier != null)
                        DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(__instance.Data.PlayerName + " Attempted to kill you but failed :D");
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
        class OnMeetingEnd
        {
            static void Postfix()
            {
                if (ImmortalityLogic.ModEnabled && PlayerControl.LocalPlayer != null && !PlayerControl.LocalPlayer.Data.IsDead)
                {
                    VentilationSystem.Update(VentilationSystem.Operation.Enter, 50);
                    ImmortalityLogic._internalState = true;
                }
            }
        }
    }
}
