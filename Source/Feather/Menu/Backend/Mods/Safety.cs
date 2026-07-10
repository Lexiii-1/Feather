using Feather.Menu.Extra;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Feather.Menu.Backend.Mods
{
    public class Safety
    {
        public static void AntiReport()
        {
            if (NetworkSystem.Instance == null || !NetworkSystem.Instance.InRoom) return;

            NetPlayer? localPlayer = NetworkSystem.Instance.LocalPlayer;
            List<GorillaPlayerScoreboardLine>? lines = GorillaScoreboardTotalUpdater.allScoreboardLines;
            IReadOnlyList<VRRig>? rigs = VRRigCache.ActiveRigs;

            for (int i = 0; i < lines.Count; i++)
            {
                GorillaPlayerScoreboardLine? line = lines[i];

                if (line.linePlayer != localPlayer) continue;
                Vector3 reportBtnPos = line.reportButton.transform.position;

                for (int j = 0; j < rigs.Count; j++)
                {
                    VRRig? vrrig = rigs[j];

                    if (vrrig == null || vrrig.isLocal || vrrig.isOfflineVRRig) continue;

                    if (Vector3.Distance(vrrig.rightHandTransform.position, reportBtnPos) < 0.4f ||
                        Vector3.Distance(vrrig.leftHandTransform.position, reportBtnPos) < 0.4f)
                    {
                        PhotonNetwork.Disconnect();
                        NotiLib.SendNotification(vrrig.Creator.NickName + " Tried to report you!", 2000);

                        return;
                    }
                }
            }
        }
    }
}
