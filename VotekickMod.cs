using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnhollowerBaseLib;

namespace VotekickMod
{
    [BepInPlugin("com.votekick.mod", "Votekick Mod", "1.0.0")]
    public class VotekickPlugin : BasePlugin
    {
        public static ManualLogSource Logger;
        public static List<int> votekickedPlayerIds = new List<int>();
        public static int selectedVotekickTargetId = -1;
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
                    if (showGui)
                    {
                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.None;
                    }
                    else
                    {
                        Cursor.visible = false;
                        Cursor.lockState = CursorLockMode.Locked;
                    }
                }
            }

            private void OnGUI()
            {
                if (!showGui) return;

                GUI.Box(new Rect(10, 10, 250, 500), "Votekick Mod");

                if (GUI.Button(new Rect(20, 40, 230, 30), "Votekick All"))
                {
                    VotekickAll();
                }

                int yOffset = 80;
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        if (player == null || player.AmOwner || player.Data == null) continue;

                        if (GUI.Button(new Rect(20, yOffset, 230, 25), player.Data.PlayerName))
                        {
                            selectedVotekickTargetId = player.Data.ClientId;
                            VotekickTarget();
                        }
                        yOffset += 30;
                    }
                }
            }

            private void VotekickAll()
            {
                try
                {
                    if (VoteBanSystem.Instance != null)
                    {
                        int num = 0;
                        foreach (PlayerControl playerControl in PlayerControl.AllPlayerControls.ToArray())
                        {
                            if (playerControl != null && !playerControl.AmOwner)
                            {
                                int clientId = playerControl.Data.ClientId;
                                if (!votekickedPlayerIds.Contains(clientId))
                                {
                                    for (int i = 0; i < 3; i++)
                                    {
                                        VoteBanSystem.Instance.CmdAddVote(clientId);
                                    }
                                    votekickedPlayerIds.Add(clientId);
                                    num++;
                                }
                            }
                        }
                        VotekickPlugin.Logger.LogInfo("Votekick sent to " + num.ToString() + " players");
                    }
                }
                catch (Exception ex)
                {
                    VotekickPlugin.Logger.LogError("Failed to votekick all: " + ex.Message);
                }
            }

            private void VotekickTarget()
            {
                try
                {
                    if (selectedVotekickTargetId != -1 && VoteBanSystem.Instance != null)
                    {
                        VoteBanSystem.Instance.CmdAddVote(selectedVotekickTargetId);
                        VotekickPlugin.Logger.LogInfo("Votekick added to player with ClientId: " + selectedVotekickTargetId.ToString());
                        
                        if (DestroyableSingleton<HudManager>.Instance != null && DestroyableSingleton<HudManager>.Instance.Notifier != null)
                        {
                            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage("Votekick sent! Leave and rejoin 2 more times.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    VotekickPlugin.Logger.LogError("Failed to votekick target: " + ex.Message);
                }
            }
        }
    }
}
