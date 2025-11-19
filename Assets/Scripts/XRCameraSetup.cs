using UnityEngine;

/// <summary>
/// XR Origin 카메라를 1인칭 FPS 스타일로 설정
/// </summary>
public class XRCameraSetup : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float eyeHeight = 1.6f; // 플레이어 눈 높이
    [SerializeField] private bool hidePlayerMesh = true; // 플레이어 메시 숨기기
    [SerializeField] private LayerMask playerLayer; // 플레이어 레이어 (카메라에서 제외)

    private Transform xrOrigin;
    private Transform cameraOffset;
    private Camera mainCamera;
    private MeshRenderer playerMesh;

    private void Start()
    {
        SetupXRCamera();
        SetupPlayerLayer();
    }

    private void SetupXRCamera()
    {
        // XR Origin 찾기
        xrOrigin = transform.Find("XR Origin (VR)");
        if (xrOrigin == null)
        {
            xrOrigin = transform.Find("XR Origin (XR Rig)");
        }

        if (xrOrigin == null)
        {
            Debug.LogWarning("[XRCameraSetup] XR Origin을 찾을 수 없습니다. Player 하위에 있는지 확인하세요.");
            return;
        }

        // XR Origin 위치 초기화 (Player 중심)
        xrOrigin.localPosition = Vector3.zero;
        xrOrigin.localRotation = Quaternion.identity;

        // Camera Offset 찾기 및 높이 설정
        cameraOffset = xrOrigin.Find("Camera Offset");
        if (cameraOffset != null)
        {
            // 눈 높이 설정
            Vector3 pos = cameraOffset.localPosition;
            pos.y = eyeHeight;
            cameraOffset.localPosition = pos;

            Debug.Log($"[XRCameraSetup] Camera Offset 높이 설정: {eyeHeight}m");
        }

        // 메인 카메라 찾기
        mainCamera = GetComponentInChildren<Camera>();
        if (mainCamera != null)
        {
            // 플레이어 레이어를 카메라 Culling Mask에서 제외
            if (hidePlayerMesh && gameObject.layer != 0)
            {
                int layerMask = mainCamera.cullingMask;
                layerMask &= ~(1 << gameObject.layer); // 플레이어 레이어 제외
                mainCamera.cullingMask = layerMask;

                Debug.Log($"[XRCameraSetup] 플레이어 레이어 {LayerMask.LayerToName(gameObject.layer)} 카메라에서 제외");
            }
        }

        // 플레이어 메시 숨기기 (선택)
        if (hidePlayerMesh)
        {
            playerMesh = GetComponent<MeshRenderer>();
            if (playerMesh != null)
            {
                playerMesh.enabled = false;
                Debug.Log("[XRCameraSetup] 플레이어 메시 숨김");
            }
        }

        // VR 컨트롤러 라인 숨기기 (FPS 스타일 - 화면 중앙 조준)
        HideControllerLines();

        Debug.Log("[XRCameraSetup] 1인칭 FPS 카메라 설정 완료");
    }

    /// <summary>
    /// VR 컨트롤러의 레이저 라인을 비활성화 (Head-Aimed FPS용)
    /// </summary>
    private void HideControllerLines()
    {
        if (cameraOffset == null) return;

        // Left Controller와 Right Controller의 Line Renderer 비활성화
        foreach (Transform child in cameraOffset)
        {
            if (child.name.Contains("Controller"))
            {
                // XR Interactor Line Visual 컴포넌트 비활성화
                var lineVisual = child.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>();
                if (lineVisual != null)
                {
                    lineVisual.enabled = false;
                    Debug.Log($"[XRCameraSetup] {child.name} 라인 비활성화");
                }

                // Line Renderer 비활성화 (대체)
                var lineRenderer = child.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = false;
                    Debug.Log($"[XRCameraSetup] {child.name} LineRenderer 비활성화");
                }
            }
        }
    }

    /// <summary>
    /// Player GameObject를 Player 레이어로 설정 (발사체가 자신을 쏘지 않도록)
    /// </summary>
    private void SetupPlayerLayer()
    {
        // Player 레이어 확인 및 설정
        int playerLayerIndex = LayerMask.NameToLayer("Player");

        if (playerLayerIndex == -1)
        {
            Debug.LogWarning("[XRCameraSetup] 'Player' 레이어가 없습니다. Edit → Project Settings → Tags and Layers에서 'Player' 레이어를 추가하세요.");
            return;
        }

        // Player GameObject의 레이어 설정 (자식은 제외)
        if (gameObject.layer != playerLayerIndex)
        {
            gameObject.layer = playerLayerIndex;
            Debug.Log($"[XRCameraSetup] Player GameObject를 'Player' 레이어로 설정 (Layer {playerLayerIndex})");
        }

        // XR Origin도 Player 레이어로 설정
        if (xrOrigin != null && xrOrigin.gameObject.layer != playerLayerIndex)
        {
            xrOrigin.gameObject.layer = playerLayerIndex;
            Debug.Log($"[XRCameraSetup] XR Origin을 'Player' 레이어로 설정");
        }
    }

    // Inspector에서 높이를 변경하면 실시간으로 적용
    private void OnValidate()
    {
        if (Application.isPlaying && cameraOffset != null)
        {
            Vector3 pos = cameraOffset.localPosition;
            pos.y = eyeHeight;
            cameraOffset.localPosition = pos;
        }
    }
}

