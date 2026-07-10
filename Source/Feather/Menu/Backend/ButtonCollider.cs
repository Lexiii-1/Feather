using System;
using UnityEngine;

namespace Feather.Menu.Backend
{
    public class ButtonCollider : MonoBehaviour
    {
        public Action Act;

        private static float lastTriggerTime;

        void OnTriggerEnter(Collider other)
        {
            var hand = other.GetComponentInParent<ButtonPresser>();
            if (hand == null) return;

            if (Time.time - lastTriggerTime > 0.3f)
            {
                lastTriggerTime = Time.time;
                Act?.Invoke();

                VRRig.LocalRig.PlayHandTapLocal(66, false, 4 / 10f);
                GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tagHapticStrength / 2f, 0.05f);
            }
        }
    }
}