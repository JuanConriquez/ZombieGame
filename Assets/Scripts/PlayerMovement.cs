using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
    {
        public float moveSpeed = 6f;
        public Camera playerCamera;

        private CharacterController controller;
        

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        
    }

    // Update is called once per frame
    void Update()
    {
        // if (!IsOwner) return;
        playerCamera.transform.position = new Vector3(transform.position.x, 30f, transform.position.z + 35f);
        playerCamera.transform.LookAt(transform.position);

        float h = Input.GetAxis("Horizontal"); //A/D
        float v = Input.GetAxis("Vertical"); // W/S

        Vector3 moveDir = new Vector3(-h, 0f, -v);

        controller.Move(moveDir * moveSpeed * Time.deltaTime);

        controller.Move(Vector3.down * 9.8f * Time.deltaTime);

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if(groundPlane.Raycast(ray, out float distance))
        {
            Vector3 lookPoint = ray.GetPoint(distance);
            lookPoint.y = transform.position.y;
            transform.LookAt(lookPoint);
        }
    }
}
