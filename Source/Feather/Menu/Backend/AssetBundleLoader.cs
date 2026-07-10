using System.IO;
using System.Reflection;
using UnityEngine;
using BepInEx;
using System.Collections.Generic;

namespace Feather.Menu.Backend
{
    public class AssetBundleLoader : MonoBehaviour
    {
        public static GameObject MenuInstance { get; private set; }
        public static GameObject NotificationPrefab { get; private set; }
        private static AssetBundle _activeBundle;

        private void Awake()
        {
            LoadBundles();
        }

        public static void ReloadCustomBundle()
        {
            if (MenuInstance != null)
            {
                Destroy(MenuInstance);
                MenuInstance = null;
            }

            if (_activeBundle != null)
            {
                _activeBundle.Unload(true);
                _activeBundle = null;
            }

            LoadBundles();
        }

        private static void LoadBundles()
        {
            string settingsPath = Path.Combine(Paths.GameRootPath, "Feather", "Settings.txt");
            bool loadCustom = false;

            if (File.Exists(settingsPath))
            {
                foreach (string line in File.ReadAllLines(settingsPath))
                {
                    if (line.Trim().StartsWith("CustomBundle"))
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length > 1 && parts[1].Trim().ToLower() == "true")
                        {
                            loadCustom = true;
                            break;
                        }
                    }
                }
            }

            bool customLoaded = false;

            if (loadCustom)
            {
                string bundlePath = Path.Combine(Paths.GameRootPath, "Feather", "custom.bundle");
                if (File.Exists(bundlePath))
                {
                    _activeBundle = AssetBundle.LoadFromFile(bundlePath);
                    if (_activeBundle != null)
                    {
                        GameObject prefab = _activeBundle.LoadAsset<GameObject>("Menu");
                        if (prefab != null)
                        {
                            MenuInstance = Instantiate(prefab);
                            MenuInstance.SetActive(false);
                            DontDestroyOnLoad(MenuInstance);
                            NotificationPrefab = _activeBundle.LoadAsset<GameObject>("Notification");
                            customLoaded = true;
                        }
                        else
                        {
                            _activeBundle.Unload(true);
                            _activeBundle = null;
                        }
                    }
                }
            }

            if (!customLoaded)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Menu.Resources.feather"))
                {
                    if (stream != null)
                    {
                        byte[] bundleData = new byte[stream.Length];
                        stream.Read(bundleData, 0, bundleData.Length);
                        _activeBundle = AssetBundle.LoadFromMemory(bundleData);
                        if (_activeBundle != null)
                        {
                            GameObject prefab = _activeBundle.LoadAsset<GameObject>("Menu");
                            if (prefab != null)
                            {
                                MenuInstance = Instantiate(prefab);
                                MenuInstance.SetActive(false);
                                DontDestroyOnLoad(MenuInstance);
                            }
                            NotificationPrefab = _activeBundle.LoadAsset<GameObject>("Notification");
                        }
                    }
                }
            }
        }
    }

    public class MaterialOffsetAnimator : MonoBehaviour
    {
        private List<Material> _materials = new List<Material>();
        private float _speed = 0.4f;

        private void Awake()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                _materials.AddRange(renderer.materials);
            }
        }

        private void Update()
        {
            float offset = Time.time * _speed;
            foreach (Material mat in _materials)
            {
                mat.mainTextureOffset = new Vector2(offset, offset);
            }
        }

        private void OnDestroy()
        {
            foreach (Material mat in _materials)
            {
                Destroy(mat);
            }
        }
    }
}