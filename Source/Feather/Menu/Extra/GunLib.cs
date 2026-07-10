using Feather.Menu.Backend;
using GorillaLocomotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Feather.Menu.Extra
{
    public class GunLib : MonoBehaviour
    {
        public static GameObject GunObject;
        public static Transform GunPos;

        private static LineRenderer lineRenderer;
        private static GunLib instance;

        private static bool isHolding = false;
        private static bool allowThisFrame = false;
        private static Camera cachedShoulderCam;

        public static VRRig lockedRig = null;
        private static Vector3 lockedOffset;

        private const float unlockDistance = 1.35f;
        private static bool isRightHandActive = true;

        public static readonly string[] ignoreLayers =
        {
            "Gorilla Trigger",
            "Gorilla Boundary",
            "GorillaHand",
            "GorillaObject",
            "Zone",
            "Water",
            "GorillaCosmetics",
            "GorillaParticle",
        };

        public static bool IsOverVrrig => lockedRig != null;

        public static bool Triggering =>
            (InputPoller.Instance != null &&
            (isRightHandActive
                ? InputPoller.Instance.GetRightTrigger()
                : InputPoller.Instance.GetLeftTrigger())) ||
            (Mouse.current != null && Mouse.current.leftButton.isPressed);

        public static void LetGun()
        {
            allowThisFrame = true;
        }

        void Awake()
        {
            instance = this;
        }

        private Camera GetShoulderCamera()
        {
            if (cachedShoulderCam == null)
            {
                GameObject camObj = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera");
                if (camObj != null)
                {
                    cachedShoulderCam = camObj.GetComponent<Camera>();
                }
            }
            return cachedShoulderCam;
        }

        void Update()
        {
            if (!allowThisFrame)
            {
                if (isHolding)
                {
                    DestroyGun();
                    isHolding = false;
                }
                lockedRig = null;
                return;
            }

            allowThisFrame = false;

            if (GTPlayer.Instance == null)
                return;

            bool mouseRight = Mouse.current != null && Mouse.current.rightButton.isPressed;

            bool rightGrab = (InputPoller.Instance != null && InputPoller.Instance.GetRightGrip()) || mouseRight;
            bool leftGrab = (InputPoller.Instance != null && InputPoller.Instance.GetLeftGrip());
            bool holding = rightGrab || leftGrab;

            if (lockedRig == VRRig.LocalRig)
                lockedRig = null;

            if (holding && !isHolding)
            {
                isRightHandActive = rightGrab;
                SpawnGun();
                isHolding = true;
            }

            if (isHolding)
            {
                if (isRightHandActive && !rightGrab && leftGrab)
                    isRightHandActive = false;
                else if (!isRightHandActive && !leftGrab && rightGrab)
                    isRightHandActive = true;
            }

            if (holding && GunObject != null)
            {
                Vector3 origin;
                Vector3 direction;
                Ray ray;

                if (mouseRight)
                {
                    Camera cam = GetShoulderCamera();
                    if (cam != null)
                    {
                        origin = cam.transform.position + (cam.transform.right * 0.2f);
                        direction = cam.transform.forward;
                        ray = new Ray(origin, direction);
                    }
                    else
                    {
                        ray = new Ray(GTPlayer.Instance.headCollider.transform.position, GTPlayer.Instance.headCollider.transform.forward);
                    }
                }
                else
                {
                    Transform hand = isRightHandActive
                        ? GTPlayer.Instance.RightHand.controllerTransform
                        : GTPlayer.Instance.LeftHand.controllerTransform;

                    float downwardAngle = 50f;
                    direction = Quaternion.AngleAxis(downwardAngle, hand.right) * hand.forward;
                    origin = hand.position;
                    ray = new Ray(origin, direction);
                }

                int mask = ~LayerMask.GetMask(ignoreLayers);
                bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, 1000f, mask, QueryTriggerInteraction.Collide);

                Vector3 targetPoint = ray.origin + ray.direction * 1000f;

                if (hitSomething)
                {
                    VRRig hitRig = hit.collider.GetComponentInParent<VRRig>();
                    if (hitRig != null && hitRig != VRRig.LocalRig)
                    {
                        if (lockedRig != hitRig)
                        {
                            lockedRig = hitRig;
                            lockedOffset = Vector3.zero;
                        }
                    }
                }

                if (lockedRig != null)
                {
                    Vector3 rigCenter = lockedRig.transform.position + lockedOffset;
                    float distanceFromAim = Vector3.Cross(ray.direction, rigCenter - ray.origin).magnitude;

                    if (distanceFromAim > unlockDistance)
                    {
                        lockedRig = null;
                    }
                    else
                    {
                        targetPoint = rigCenter;
                        hitSomething = true;
                    }
                }
                else if (hitSomething)
                {
                    targetPoint = hit.point;
                }

                GunObject.transform.position = targetPoint;
                GunObject.transform.rotation = hitSomething ? Quaternion.LookRotation((targetPoint - ray.origin).normalized) : Quaternion.identity;

                DrawLine(ray.origin, targetPoint);
                GunPos = GunObject.transform;
            }

            if (!holding && isHolding)
            {
                DestroyGun();
                isHolding = false;
                lockedRig = null;
            }
        }

        private void DrawLine(Vector3 start, Vector3 end)
        {
            if (lineRenderer == null) return;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        private void SpawnGun()
        {
            GunObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GunObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            Destroy(GunObject.GetComponent<Rigidbody>());
            Destroy(GunObject.GetComponent<Collider>());

            Material mat = new Material(Shader.Find("GUI/Text Shader")) { color = ThemeManager.GetTheme() };
            GunObject.GetComponent<Renderer>().material = mat;

            lineRenderer = GunObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.material = mat;
        }

        private void DestroyGun()
        {
            if (GunObject != null)
            {
                Destroy(GunObject);
                GunObject = null;
                GunPos = null;
                lineRenderer = null;
            }
        }
    }
}