using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Basic WASD + scroll + right-drag rotation camera tool.
    /// </summary>
    public class CameraTool : MonoBehaviour, ITool
    {
        [Header("Movement")]
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private float scrollSpeed = 20f;
        [SerializeField] private float minY = 20f;
        [SerializeField] private float maxY = 120f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 20f;

        private bool _rotating;

        public string ToolName => "Camera";

        private void Update()
        {
            HandleMove();
            HandleRotate();
        }

        private void HandleMove()
        {
            Vector3 forward = transform.forward; forward.y = 0f; forward.Normalize();
            Vector3 right = transform.right; right.y = 0f; right.Normalize();
            Vector3 pos = transform.position;

            if (Input.GetKey(KeyCode.W)) pos += forward * panSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) pos -= forward * panSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) pos += right * panSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.A)) pos -= right * panSpeed * Time.deltaTime;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            pos.y -= scroll * scrollSpeed * 100f * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            transform.position = pos;
        }

        private void HandleRotate()
        {
            if (Input.GetMouseButtonDown(1))
            {
                _rotating = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (Input.GetMouseButtonUp(1))
            {
                _rotating = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (_rotating)
            {
                Vector3 delta = new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"), 0f) * rotationSpeed;
                transform.Rotate(Vector3.up, delta.x, Space.World);
                transform.Rotate(transform.right, -delta.y, Space.World);
            }
        }

        public void OnToolActivated()
        {
            enabled = true;
        }

        public void OnToolDeactivated()
        {
            enabled = false;
            _rotating = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
