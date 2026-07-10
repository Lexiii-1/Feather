using GorillaLocomotion;
using System.Collections;
using TMPro;
using UnityEngine;
using Feather.Menu.Backend;

namespace Feather.Menu.Extra;

public class NotiLib : MonoBehaviour
{
    public static NotiLib Instance;
    private static GameObject _currentNotification;

    private void Awake()
    {
        Instance = this;
    }

    public static void SendNotification(string message, float time)
    {
        if (Instance == null || AssetBundleLoader.NotificationPrefab == null) return;

        if (_currentNotification != null)
        {
            Destroy(_currentNotification);
        }

        GameObject notificationInstance = Instantiate(AssetBundleLoader.NotificationPrefab);
        notificationInstance.transform.SetParent(GTPlayer.Instance.headCollider.transform, false);
        notificationInstance.transform.localPosition = Vector3.zero;
        notificationInstance.transform.localRotation = Quaternion.identity;

        Transform textTransform = notificationInstance.transform.Find("Notification/Text");
        if (textTransform != null)
        {
            TextMeshPro textComponent = textTransform.GetComponent<TextMeshPro>();
            if (textComponent != null)
            {
                textComponent.text = message;
            }
        }

        _currentNotification = notificationInstance;

        Instance.StartCoroutine(Instance.DestroyAfterTime(notificationInstance, time));
    }

    private IEnumerator DestroyAfterTime(GameObject notificationObj, float time)
    {
        yield return new WaitForSeconds(time / 1000.0f);

        if (notificationObj != null)
        {
            if (_currentNotification == notificationObj)
            {
                _currentNotification = null;
            }
            Destroy(notificationObj);
        }
    }
}