using UnityEngine;

namespace Feather.Menu.Backend
{
    public class Config : MonoBehaviour
    {
        public static Config instance;

        void Awake()
        {
            instance = this;
            MenuBackend.Initialize();
        }
    }
}