using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [SerializeField][Range(1f, 20f)] private float sensitivity = 10f; // 마우스 감도 조절
    private float mouseX, mouseY;
    private Transform playerTransform;

    private void Start()
    {
        //원활한 Debugging을 위해 마우스 커서를 보이지 않도록 하였습니다, Play 중 Esc 키를 누르면 마우스를 볼 수 있습니다.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked; // 커서를 화면 중앙에 고정

        // transform: 현재 이 스크립트가 붙어있는 오브젝트(카메라)의 Transform
        // transform.parent: 현재 오브젝트의 부모 오브젝트(플레이어 몸체)의 Transform
        //  -> 좌우 회전은 플레이어 몸체 전체를 돌려야 함
        playerTransform = transform.parent;
    }

    /* 고정된 시간 간격으로 호출. 프레임률에 영향받지 않음.
       참고: 일반적으로 카메라는 Update()나 LateUpdate()를 사용 */
    private void FixedUpdate()
    {
        mouseX += Input.GetAxis("Mouse X") * sensitivity; // 회전: 좌우 움직임 (각도 누적)

        /* Quaternion.Euler(x, y, z)
           x: 상하 회전 (pitch)
           y: 좌우 회전 (yaw)
           z: 좌우 기울기 (roll) */
        playerTransform.rotation = Quaternion.Euler(new Vector3(0, mouseX, 0)); // 플레이어 몸체 회전 (좌우)

        mouseY += Input.GetAxis("Mouse Y") * sensitivity; // 회전: 상하 움직임 (각도 누적)
        mouseY = Mathf.Clamp(mouseY, -75f, 75f); // 값 제한 (-75도 ~ 75도)

        // localRotation: 부모를 기준으로 한 상대적인 회전
        // -mouseY: 상하 회전 값을 반대로 적용 (마우스 Y축 <-> Unity 좌표계는 방향이 반대)
        transform.localRotation = Quaternion.Euler(new Vector3(-mouseY, 0, 0)); // 카메라 회전 (상하)
    }
}