using System;
using HarmonyLib;

namespace Feather.Menu.patches
{
    [HarmonyPatch(typeof(MonkeAgent), "QuitDelay", MethodType.Enumerator)]
    public class QuitDelayPatch
    {
        private static bool Prefix()
        {
            return false;
        }
    }
}
