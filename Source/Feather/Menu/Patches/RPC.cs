using System;
using HarmonyLib;

namespace Feather.Menu.patches
{
    [HarmonyPatch(typeof(MonkeAgent), "GetRPCCallTracker")]
    internal class RPC
    {
        private static bool Prefix()
        {
            return false;
        }
    }
}
