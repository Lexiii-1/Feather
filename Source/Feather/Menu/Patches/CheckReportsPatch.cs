using System;
using HarmonyLib;

namespace Feather.Menu.patches
{
    [HarmonyPatch(typeof(MonkeAgent), "CheckReports")]
    public class CheckReportsPatch
    {
        private static bool Prefix()
        {
            return false;
        }
    }
}
