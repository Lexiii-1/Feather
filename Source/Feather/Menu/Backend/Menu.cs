using Feather.Menu.Backend;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace Feather.Menu.Backend
{
    public class Menu : MonoBehaviour
    {
        public static GameObject Collider;
        private static Dictionary<Transform, Color> buttonBaseColors = new Dictionary<Transform, Color>();
        private float animationTimer = 0f;
        private bool isClosing = false;
        private bool wasOpen = false;

        void Update()
        {
            MenuBackend.Tick();
            if (AssetBundleLoader.MenuInstance == null) return;
            Transform backContainer = AssetBundleLoader.MenuInstance.transform.Find("Back");

            float menuScale = MenuBackend.GetValue("Menu Size");
            if (menuScale == 0f) menuScale = 1f;

            if (InputPoller.Instance.GetYButton())
            {
                if (!wasOpen)
                {
                    isClosing = false;
                    animationTimer = 0f;
                    wasOpen = true;
                }

                AssetBundleLoader.MenuInstance.SetActive(true);
                AssetBundleLoader.MenuInstance.transform.SetParent(GTPlayer.Instance.LeftHand.controllerTransform, false);
                AssetBundleLoader.MenuInstance.transform.localPosition = new Vector3(0.08f, 0, 0);
                AssetBundleLoader.MenuInstance.transform.localRotation = Quaternion.identity;

                Vector3 localVelocity = GTPlayer.Instance.LeftHand.controllerTransform.InverseTransformDirection(GTPlayer.Instance.LeftHand.controllerTransform.GetComponent<GorillaVelocityTracker>().GetAverageVelocity(false));

                animationTimer = Mathf.MoveTowards(animationTimer, 1f, Time.deltaTime * 5f);
                float bounce = 1f + (Mathf.Sin(animationTimer * Mathf.PI) * 0.4f);

                float squashFactor = Mathf.Clamp(localVelocity.magnitude * 0.1f, -0.2f, 0.2f);
                AssetBundleLoader.MenuInstance.transform.localScale = Vector3.one * (menuScale * bounce) + new Vector3(0, 0, squashFactor);

                UpdateMenu(backContainer);
            }
            else
            {
                if (wasOpen)
                {
                    isClosing = true;
                    animationTimer = 1f;
                    wasOpen = false;
                }

                if (isClosing)
                {
                    animationTimer = Mathf.MoveTowards(animationTimer, 0f, Time.deltaTime * 8f);
                    float finalScale = animationTimer * menuScale;
                    AssetBundleLoader.MenuInstance.transform.localScale = new Vector3(finalScale, finalScale, finalScale);

                    if (animationTimer <= 0f)
                    {
                        AssetBundleLoader.MenuInstance.SetActive(false);
                        isClosing = false;
                    }
                }
            }

            if (AssetBundleLoader.MenuInstance.activeInHierarchy && Collider == null)
            {
                Collider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Collider.layer = 2;
                Collider.GetComponent<Collider>().isTrigger = true;
                Collider.AddComponent<ButtonPresser>();
                Collider.transform.localScale = Vector3.one * 0.01f;
                Collider.GetComponent<Renderer>().material.shader = Shader.Find("GUI/Text Shader");
                Collider.GetComponent<Renderer>().material.color = Color.white;
            }

            if (AssetBundleLoader.MenuInstance.activeInHierarchy && Collider != null)
            {
                bool isRigEnabled = VRRig.LocalRig.enabled;
                if (isRigEnabled)
                {
                    Collider.transform.SetParent(GorillaTagger.Instance.rightHandTriggerCollider.transform, false);
                    Collider.transform.localPosition = Vector3.zero;
                }
                else
                {
                    Transform rightHand = GTPlayer.Instance.RightHand.controllerTransform;
                    Collider.transform.SetParent(rightHand, false);
                    Collider.transform.localPosition = Vector3.down * 0.094f;
                }
            }
            else if (!AssetBundleLoader.MenuInstance.activeInHierarchy && Collider != null)
            {
                Destroy(Collider);
                Collider = null;
            }
        }

        void UpdateMenu(Transform backContainer)
        {
            var buttons = MenuBackend.GetCurrentButtons();
            List<Transform> btnTransforms = new List<Transform>();
            for (int i = 0; i < backContainer.childCount; i++)
            {
                Transform child = backContainer.GetChild(i);
                if (child.name.StartsWith("Btn")) btnTransforms.Add(child);
            }

            btnTransforms = btnTransforms.OrderBy(t => {
                var match = Regex.Match(t.name, @"\d+");
                return match.Success ? int.Parse(match.Value) : 0;
            }).ToList();

            for (int i = 0; i < btnTransforms.Count; i++)
            {
                Transform btnTrans = btnTransforms[i];
                if (i < buttons.Count)
                {
                    btnTrans.gameObject.SetActive(true);
                    var b = buttons[i];
                    SetupButton(btnTrans, b.Name, b.Press, b.IsToggle && b.Entry.Value);
                }
                else btnTrans.gameObject.SetActive(false);
            }

            Transform forwardBtn = backContainer.Find("Forward");
            if (forwardBtn != null) { forwardBtn.gameObject.SetActive(true); (forwardBtn.gameObject.GetComponent<ButtonCollider>() ?? forwardBtn.gameObject.AddComponent<ButtonCollider>()).Act = () => MenuBackend.HandleNav("Forward"); }

            Transform backwardBtn = backContainer.Find("Backward");
            if (backwardBtn != null) { backwardBtn.gameObject.SetActive(true); (backwardBtn.gameObject.GetComponent<ButtonCollider>() ?? backwardBtn.gameObject.AddComponent<ButtonCollider>()).Act = () => MenuBackend.HandleNav("Backward"); }

            Transform disconnectBtn = backContainer.Find("Disconnect");
            if (disconnectBtn != null) { disconnectBtn.gameObject.SetActive(true); (disconnectBtn.gameObject.GetComponent<ButtonCollider>() ?? disconnectBtn.gameObject.AddComponent<ButtonCollider>()).Act = () => MenuBackend.HandleNav("Disconnect"); }
        }

        void SetupButton(Transform trans, string text, System.Action action, bool isOn)
        {
            trans.Find("Text").GetComponent<TMP_Text>().text = text;
            var bc = trans.gameObject.GetComponent<ButtonCollider>() ?? trans.gameObject.AddComponent<ButtonCollider>();
            bc.Act = action;
            Renderer renderer = trans.GetComponent<Renderer>();
            if (!buttonBaseColors.ContainsKey(trans)) buttonBaseColors.Add(trans, renderer.material.color);
            renderer.material.color = isOn ? buttonBaseColors[trans] * 1.25f : buttonBaseColors[trans];
        }
    }
}