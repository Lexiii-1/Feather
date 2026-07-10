using System.Collections.Generic;
using Feather.Menu.Extra;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace Feather.Menu.patches
{
    [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.SendReport))]
    public static class AntiCheat
    {
        private const float PlayerReportLogCooldown = 0f;
        private static readonly Dictionary<string, float> LastLoggedReport = new Dictionary<string, float>();

        private static bool Prefix(string susReason, string susId, string susNick)
        {
            NotiLib.SendNotification($"Monke Agent reported {susNick} ({susId}) for: {susReason}", 2000);

            if (LastLoggedReport.ContainsKey(susId) && LastLoggedReport[susId] > Time.time)
            {
                return true;
            }

            LastLoggedReport[susId] = Time.time + PlayerReportLogCooldown;

            return true;
        }
    }
}