using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// 발사 시 햅틱 피드백 (VR 디바이스) 또는 로그 출력 (시뮬레이터)
/// </summary>
public class HapticFeedbackManager : MonoBehaviour
{
    private static HapticFeedbackManager instance;
    public static HapticFeedbackManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<HapticFeedbackManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("HapticFeedbackManager");
                    instance = obj.AddComponent<HapticFeedbackManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 발사 시 햅틱 피드백 (VR 컨트롤러 진동 또는 로그 출력)
    /// </summary>
    public void TriggerFireHaptic()
    {
        // XR 디바이스 찾기
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);

        bool sentToDevice = false;

        // VR 컨트롤러가 있으면 햅틱 전송
        foreach (var device in devices)
        {
            if (device.TryGetHapticCapabilities(out HapticCapabilities capabilities))
            {
                if (capabilities.supportsImpulse)
                {
                    device.SendHapticImpulse(0, 0.8f, 0.15f); // 강도 0.8, 지속시간 0.15초
                    sentToDevice = true;
                }
            }
        }

        // 시뮬레이터 모드 (VR 디바이스 없음)
        if (!sentToDevice)
        {
            Debug.Log("[Haptic] 햅틱 피드백 (시뮬레이터)");
        }
    }
}

