using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Feather.Menu.Extra
{
    public class FileUtils : MonoBehaviour
    {
        public static bool DoesExist(string path)
        {
            return File.Exists(path);
        }

        public static void Create(string path, string content = "")
        {
            File.WriteAllText(path, content);
        }

        public static void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static void Edit(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        public static void Truncate(string path)
        {
            File.WriteAllText(path, string.Empty);
        }

        public static void Remove(string path)
        {
            File.WriteAllText(path, string.Empty);
        }

        public static void Add(string path, string content)
        {
            File.AppendAllText(path, content);
        }
    }
}