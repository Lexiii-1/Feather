using System;
using System.IO;
using BepInEx;
using Feather.Menu.Backend;
using Feather.Menu.Extra;
using HarmonyLib;
using UnityEngine;

namespace Feather.Menu.Plugin
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;
        void Awake()
        {
            Harmony harmony = new Harmony(PluginInfo.GUID);
            harmony.PatchAll();
            instance = this;
            gameObject.AddComponent<AssetBundleLoader>();
            gameObject.AddComponent<Backend.Menu>();
            gameObject.AddComponent<Backend.Gui>();
            gameObject.AddComponent<InfoNotifs>();
            gameObject.AddComponent<RPCHelper>();
            gameObject.AddComponent<NotiLib>();
            gameObject.AddComponent<Config>();
            gameObject.AddComponent<GunLib>();
            gameObject.AddComponent<ColoredBoards>();
            gameObject.AddComponent<FileUtils>();
            gameObject.AddComponent<InputPoller>();
            try
            {
                string folderPath = Path.Combine(Paths.GameRootPath, "Feather");
                string filePath = Path.Combine(folderPath, "Settings.txt");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                if (!File.Exists(filePath))
                {
                    string content = "CustomBundle = false";
                    File.WriteAllText(filePath, content);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
        public static void Log(string message)
        {
            Plugin.instance.Logger.LogInfo(message);
        }
    }
}