using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
    {
        public float moveSpeed = 6f;
        public Camera playerCamera;
       // public KeyCode rotateCameraLeftKey = KeyCode.Q;
        //public KeyCode rotateCameraRightKey = KeyCode.E;
       // public float cameraRotateSpeed = 420f;
       
        public float mouseSensitivity = 120f;

        private CharacterController controller;
        private float targetCameraYaw;
        private float currentCameraYaw;
        

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        

        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
                playerCamera = Camera.main;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // if (!IsOwner) return;
        //HandleCameraRotationInput();
        float mouseX = Input.GetAxis("Mouse X");
        currentCameraYaw += mouseX * mouseSensitivity * Time.deltaTime;

        float h = Input.GetAxis("Horizontal"); //A/D
        float v = Input.GetAxis("Vertical"); // W/S

        Vector3 cameraForward = Quaternion.Euler(0f, currentCameraYaw, 0f) * Vector3.back;
        Vector3 cameraRight = Quaternion.Euler(0f, currentCameraYaw, 0f) * Vector3.left;
        Vector3 moveDir = (cameraRight * h + cameraForward * v);
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        controller.Move(moveDir * moveSpeed * Time.deltaTime);

        controller.Move(Vector3.down * 9.8f * Time.deltaTime);

        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if(groundPlane.Raycast(ray, out float distance))
        {
            Vector3 lookPoint = ray.GetPoint(distance);
            lookPoint.y = transform.position.y;
            transform.LookAt(lookPoint);
        }
    }

   // void HandleCameraRotationInput()
   // {
    //    if (Input.GetKeyDown(rotateCameraLeftKey))
    //        targetCameraYaw -= 90f;

   //     if (Input.GetKeyDown(rotateCameraRightKey))
           // targetCameraYaw += 90f;
  //  }

    void LateUpdate()
    {
        if (playerCamera == null) return;

       // currentCameraYaw = Mathf.MoveTowardsAngle(
        //    currentCameraYaw,
         //   targetCameraYaw,
         //   cameraRotateSpeed * Time.deltaTime);

     //   Vector3 cameraOffset = Quaternion.Euler(0f, currentCameraYaw, 0f) * new Vector3(0f, 30f, 35f);
       // playerCamera.transform.position = transform.position + cameraOffset;
       // playerCamera.transform.LookAt(transform.position);
        Vector3 cameraOffset = Quaternion.Euler(0f, currentCameraYaw, 0f) * new Vector3(0f, 30f, 35f);
    playerCamera.transform.position = transform.position + cameraOffset;
    playerCamera.transform.LookAt(transform.position);
    }
}
