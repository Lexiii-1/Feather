using BepInEx.Configuration;
using Feather.Menu.Backend.Mods;
using Feather.Menu.Extra;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace Feather.Menu.Backend
{
    public static class MenuBackend
    {
        internal static readonly List<ModCategory> categories = new();
        public static Stack<ModCategory> CategoryStack = new();
        public static int Page = 1;
        public static int GetBtnCount()
        {
            if (AssetBundleLoader.MenuInstance == null) return 0;
            Transform back = AssetBundleLoader.MenuInstance.transform.Find("Back");
            if (back == null) return 0;
            int count = 0;
            for (int i = 0; i < back.childCount; i++) if (back.GetChild(i).name.StartsWith("Btn")) count++;
            return count;
        }

        public static void Initialize()
        {
            AddCategory(null, "Safety", false,
                Toggle("Anti Report", "Safety", "Anti Report", Safety.AntiReport),
                Button("Reauthenticate", () => MothershipAuthenticator.Instance.BeginLoginFlow())
            );
            AddCategory(null, "Settings", false);

            AddCategory("Settings", "Menu Settings", false,
                Float("Menu Size", "Settings", "Menu Size", 0.4f, 4.0f, 0.1f, true),
                Toggle("Match Asset Bundle Theme", "Settings", "Match Asset Bundle Theme"),
                Button("Reload Menu Bundle", () => AssetBundleLoader.ReloadCustomBundle())
            );

            AddCategory(null, "Admin", true);
        }

        public static void RegisterExternalCategory(string parentName, string name, bool adminOnly, params ModButton[] buttons)
        {
            AddCategory(parentName, name, adminOnly, buttons);
        }

        public static ModButton CreateExternalToggle(string name, string section, string key, ConfigFile config, System.Action updateAction = null, System.Action disableAction = null, string tip = null)
        {
            var entry = config.Bind(section, key, false, "");
            return new ModButton(name, entry, updateAction, disableAction, tip);
        }

        public static ModButton CreateExternalFloat(string name, string section, string key, float min, float max, float step, bool sync, ConfigFile config, string tip = null)
        {
            var entry = config.Bind(section, key, min, "");
            return new ModButton(name, entry, min, max, step, sync, tip);
        }

        public static ModButton CreateExternalButton(string name, System.Action action, string tip = null)
        {
            return new ModButton(name, action, tip);
        }

        public static ModButton CreateCustomButton(string name, Action onPress, Func<string> nameGetter, string tip = null)
        {
            return new ModButton(name, onPress, nameGetter, tip);
        }

        public static bool Enabled(string categoryName, string buttonName)
        {
            var category = FindCategory(categoryName);
            if (category == null) return false;

            var button = category.Buttons.FirstOrDefault(b => b.Name == buttonName);
            return button != null && button.IsToggle && button.Entry.Value;
        }

        public static float GetValue(string key)
        {
            foreach (var category in categories)
            {
                var button = category.Buttons.FirstOrDefault(b => b.IsFloat && b.FloatEntry.Definition.Key == key);
                if (button != null) return button.FloatEntry.Value;
            }
            return 0f;
        }

        public static void AddCategory(string parentName, string name, bool adminOnly, params ModButton[] buttons)
        {
            var parent = parentName != null ? FindCategory(parentName) : null;
            var newCat = new ModCategory(name, buttons.ToList(), parent, adminOnly);
            categories.Add(newCat);
            if (parent != null) parent.SubCategories.Add(newCat);
        }

        public static ModCategory FindCategory(string name) => categories.FirstOrDefault(c => c.Name == name);

        public static ModButton Toggle(string name, string section, string key, Action updateAction = null, Action disableAction = null, string tip = null)
        {
            var entry = Plugin.Plugin.instance.Config.Bind(section, key, false, "");
            return new ModButton(name, entry, updateAction, disableAction, tip);
        }

        public static ModButton Float(string name, string section, string key, float min, float max, float step, bool sync, string tip = null)
        {
            var entry = Plugin.Plugin.instance.Config.Bind(section, key, min, "");
            return new ModButton(name, entry, min, max, step, sync, tip);
        }

        public static ModButton Button(string name, Action action, string tip = null) =>
            new ModButton(name, action, tip);

        public static List<ModButton> GetCurrentButtons()
        {
            List<ModButton> all = new List<ModButton>();
            var current = CategoryStack.Count > 0 ? CategoryStack.Peek() : null;

            if (current != null)
            {
                all.Add(Button("Back", () => { CategoryStack.Pop(); Page = 1; }));
                foreach (var sub in current.SubCategories.Where(s => !s.AdminOnly || Console.ServerData.IsAdmin).OrderBy(s => s.AdminOnly))
                    all.Add(Button(sub.Name, () => { CategoryStack.Push(sub); Page = 1; }));
                all.AddRange(current.Buttons);
            }
            else
            {
                foreach (var cat in categories.Where(c => c.Parent == null && (!c.AdminOnly || Console.ServerData.IsAdmin)).OrderBy(c => c.AdminOnly))
                    all.Add(Button(cat.Name, () => { CategoryStack.Push(cat); Page = 1; }));
            }

            int btnCount = GetBtnCount();
            int maxPages = Mathf.CeilToInt((float)all.Count / btnCount);
            if (Page > maxPages) Page = maxPages;
            if (Page < 1) Page = 1;

            return all.Skip((Page - 1) * btnCount).Take(btnCount).ToList();
        }

        public static void HandleNav(string action)
        {
            if (action == "Forward") { Page++; }
            else if (action == "Backward") { if (Page > 1) Page--; }
            else if (action == "Disconnect") { PhotonNetwork.Disconnect(); }
        }

        public static void Tick()
        {
            foreach (var category in categories)
                foreach (var button in category.Buttons)
                    button.Tick();
        }
    }

    public sealed class ModCategory
    {
        public string Name { get; }
        public List<ModButton> Buttons { get; }
        public List<ModCategory> SubCategories { get; } = new();
        public ModCategory Parent { get; }
        public bool AdminOnly { get; }
        public ModCategory(string name, List<ModButton> buttons, ModCategory parent, bool adminOnly) { Name = name; Buttons = buttons; Parent = parent; AdminOnly = adminOnly; }
    }

    public sealed class ModButton
    {
        private string _baseName;
        private Func<string> _nameGetter;
        public string Name => _nameGetter != null ? _nameGetter() : (IsFloat ? $"{_baseName}: {FloatEntry.Value:F1}" : _baseName);
        public ConfigEntry<bool> Entry { get; }
        public ConfigEntry<float> FloatEntry { get; }
        private float MaxValue;
        private float MinValue;
        private float Step;
        private bool Sync;
        private string Tip;

        private Action Action { get; }
        private Action UpdateAction { get; }
        private Action DisableAction { get; }
        private bool _lastState;
        public bool IsToggle => Entry != null;
        public bool IsFloat => FloatEntry != null;

        public ModButton(string name, ConfigEntry<bool> entry, Action updateAction = null, Action disableAction = null, string tip = null)
        {
            _baseName = name;
            Entry = entry;
            UpdateAction = updateAction;
            DisableAction = disableAction;
            Tip = tip;
            _lastState = Entry.Value;
        }

        public ModButton(string name, ConfigEntry<float> entry, float min, float max, float step, bool sync, string tip = null)
        {
            _baseName = name;
            FloatEntry = entry;
            MinValue = min;
            MaxValue = max;
            Step = step;
            Sync = sync;
            Tip = tip;
        }

        public ModButton(string name, Action action, string tip = null)
        { _baseName = name; Action = action; Tip = tip; }

        public ModButton(string name, Action action, Func<string> nameGetter, string tip = null)
        { _baseName = name; Action = action; _nameGetter = nameGetter; Tip = tip; }

        public void Press()
        {
            if (!string.IsNullOrEmpty(Tip)) NotiLib.SendNotification(Tip, 2000);

            if (IsToggle) Entry.Value = !Entry.Value;
            else if (IsFloat)
            {
                bool subtract = InputPoller.Instance.GetLeftTrigger();
                float nextValue = subtract ? FloatEntry.Value - Step : FloatEntry.Value + Step;

                if (nextValue > MaxValue) nextValue = MinValue;
                else if (nextValue < MinValue) nextValue = MaxValue;

                FloatEntry.Value = nextValue;

                if (Sync) SyncAll(FloatEntry.Definition.Key, FloatEntry.Value);
            }
            else Action?.Invoke();
        }

        private void SyncAll(string key, float value)
        {
            foreach (var cat in MenuBackend.categories)
            {
                foreach (var btn in cat.Buttons)
                {
                    if (btn.IsFloat && btn.FloatEntry.Definition.Key == key)
                    {
                        btn.FloatEntry.Value = value;
                    }
                }
            }
        }

        public void Tick()
        {
            if (IsToggle)
            {
                if (Entry.Value) UpdateAction?.Invoke();
                else if (_lastState) DisableAction?.Invoke();
                _lastState = Entry.Value;
            }
        }
    }
}