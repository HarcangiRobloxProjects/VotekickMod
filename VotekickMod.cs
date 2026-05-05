using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using UnityEngine;
using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;

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
            AddComponent<VotekickMenu>();
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

                GUI.Box(new Rect(10, 10, 250, 500), "Votekick Mod");

                if (GUI.Button(new Rect(20, 40, 230, 30), "Votekick All"))
                {
                    VotekickAllOnce();
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

            private void VotekickAllOnce()
            {
                if (VoteBanSystem.Instance == null) return;

                var players = PlayerControl.AllPlayerControls;
                int count = 0;

                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (p != null && !p.AmOwner && p.Data != null)
                    {
                        SendKick(p.Data.ClientId);
                        count++;
                    }
                }
                VotekickPlugin.Logger.LogInfo("Executed kick on " + count + " players.");
            }

            private void SendKick(int clientId)
            {
                if (VoteBanSystem.Instance == null) return;

               
                VoteBanSystem.Instance.CmdAddVote(clientId);
                VoteBanSystem.Instance.CmdAddVote(clientId);
                VoteBanSystem.Instance.CmdAddVote(clientId);

                VotekickPlugin.Logger.LogInfo("Kick sent to: " + clientId);
                
                if (DestroyableSingleton<HudManager>.Instance?.Notifier != null)
                {
                    DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage("Kicking ID: " + clientId);
                }
            }
        }
    }
}
