using System;
using HarmonyLib;

namespace Feather.Menu.patches
{
    [HarmonyPatch(typeof(MonkeAgent), "DispatchReport")]
    public class DispatchReportsPatch
    {
        private static bool Prefix()
        {
            return false;
        }
    }
}
