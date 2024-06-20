using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.EventSystems;

public class CityBuilderController : MonoBehaviour
{

    private Vector3 initialCursorPosition;
    private List<GameObject> instantiatedIcons = new List<GameObject>(); //some bullshit I'll need to replace eventually into it's own script
    public GameObject trackSelectorPrefab; // Assign in Unity Inspector

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);
    public float rotationSpeed = 20f;  // Speed of the camera rotation
    public Canvas mainCanvas;
    private bool isRotating;  // Flag to check if the camera is rotating
    private Vector3 lastMousePosition;  // Store the last mouse position
    public float panSpeed = 20f;
    public float panBorderThickness = 10f;
    public Vector2 panLimit;
    public float scrollSpeed = 20f;
    public float minY = 20f;
    public float maxY = 120f;
    private Vector3 lastPosition;
    public GameObject contextMenuPrefab;
    public ContextMenu buildMenu;
    private RectTransform canvasRectTransform;
    private DestinationTrainController trainTester;


    private bool isRightMouseDown = false;
    private float rightMouseDownTime = 0f;
    private const float LockThreshold = 0.25f;  // 1/4th of a second


    void Start(){
        canvasRectTransform = mainCanvas.GetComponent<RectTransform>();
        lastPosition = transform.position;
    }
    void Update()
    {
        if(lastPosition!= transform.position && buildMenu!= null){
            buildMenu.Hide();
            lastPosition = transform.position;
        }
        //change that 1 later when I can be assed
        Vector3 forwardMovement = transform.forward * 1;  // Calculate forward/backward movement based on camera's forward direction
        Vector3 rightMovement = transform.right * 1;  // Calculate left/right movement based on camera's right direction

        // Ensure that the movement doesn't affect the y-axis
        forwardMovement.y = 0;
        rightMovement.y = 0;
        Vector3 pos = gameObject.transform.position;

        // Now, use these vectors to adjust the position of your camera. 
        if (Input.GetKey("w"))
        {
            transform.position += forwardMovement * panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("s"))
        {
            transform.position -= forwardMovement * panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("d"))
        {
            transform.position += rightMovement * panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("a"))
        {
            transform.position -= rightMovement * panSpeed * Time.deltaTime;
        }
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        pos.y -= scroll * scrollSpeed * 100f * Time.deltaTime;


        // Clamp the camera position
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        pos.x = transform.position.x;
        pos.z = transform.position.z;
        transform.position = pos;
        if(isRotating)HandleCameraRotation();
        HandleInput();
    }

    private void captureTrain(DestinationTrainController train)
    {
        trainTester = train;
        Track[] tracks = FindObjectsOfType<Track>();
        foreach (Track track in tracks)
        {
            float totalLength = 0;
            for (int i = 0; i < track.waypoints.Length - 1; i++)
            {
                totalLength += Vector3.Distance(track.waypoints[i].position, track.waypoints[i + 1].position);
            }

            float halfwayLength = totalLength / 2;
            float currentLength = 0;
            Vector3 centerPoint = Vector3.zero;

            for (int i = 0; i < track.waypoints.Length - 1; i++)
            {
                float segmentLength = Vector3.Distance(track.waypoints[i].position, track.waypoints[i + 1].position);
                if (currentLength + segmentLength >= halfwayLength)
                {
                    float lerpFactor = (halfwayLength - currentLength) / segmentLength;
                    centerPoint = Vector3.Lerp(track.waypoints[i].position, track.waypoints[i + 1].position, lerpFactor);
                    break;
                }
                currentLength += segmentLength;
            }
            GameObject icon = Instantiate(trackSelectorPrefab, centerPoint, Quaternion.identity);
            icon.transform.GetComponentInChildren<FaceCamera>().playerTransform = this.transform;
            icon.GetComponent<Icon>().data = track;
            instantiatedIcons.Add(icon);
        }

    }
    void ClearTrackIcons() {
        foreach(GameObject icon in instantiatedIcons){
            Destroy(icon);
        }
        instantiatedIcons.Clear();
    }
    private void HandleCameraRotation()
    {
        // If the right mouse button is pressed down
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
            Cursor.visible = false; // Hide the cursor
            Cursor.lockState = CursorLockMode.Locked; // Lock the cursor to the center of the screen
        }

        // If the right mouse button is released
        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
            Cursor.visible = true; // Show the cursor
            Cursor.lockState = CursorLockMode.None; // Unlock the cursor
        }

        // Rotate the camera based on the mouse movement
        if (isRotating)
        {
            Vector3 deltaMousePos = new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"), 0) * rotationSpeed;
            transform.Rotate(Vector3.up, deltaMousePos.x, Space.World);
            transform.Rotate(transform.right, -deltaMousePos.y, Space.World);
        }
    }
    private void HandleInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        // Check for left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            // Create a ray from the camera to the mouse position
            
            
            if(EventSystem.current.IsPointerOverGameObject()){

            }
            else if (Physics.Raycast(ray, out hit))
            {
                // Here, hit.point will give you the world position of where the mouse clicked
                //DesignatePoint(hit.point);
                Icon hitTrack = hit.collider.gameObject.GetComponent<Icon>();
                if(hitTrack!= null && hitTrack.data is Track track){
                    Debug.Log("Clear Icons");
                    trainTester.NavigateToPosition(track,hitTrack.transform);
                    ClearTrackIcons();
                }
                else{
                    Debug.Log("Hit nothing");
                    Debug.Log(hitTrack);
                }
                buildMenu.Hide();
            }
            else {
                buildMenu.Hide();
            }
        }

        // Check for right mouse button click
        if (Input.GetMouseButtonDown(1))
        {
            isRightMouseDown = true;
            rightMouseDownTime = Time.time;
            StartCoroutine(CheckRightMouseHoldDuration());
            buildMenu.ClearOptions();
            if(Physics.Raycast(ray, out hit)){
                DestinationTrainController traincap = hit.collider.gameObject.GetComponent<DestinationTrainController>();
                if(traincap != null){
                    buildMenu.AddOption("Capture Train",()=> captureTrain(traincap));

                }
                Track track = hit.collider.gameObject.GetComponent<Track>();
                if(track != null){
                    buildMenu.AddOption("Send Train",()=>{traincap.NavigateToPosition(hit.collider.gameObject.GetComponent<Track>());});
                }
                Road road = hit.collider.gameObject.GetComponent<Road>();
                if(road != null){
                    buildMenu.AddOption("Split Road",()=>{road.SplitRoad(hit.point);});
                }
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
            isRightMouseDown = false;
            Cursor.visible = true; // Show the cursor
            Cursor.lockState = CursorLockMode.None; // Unlock the cursor
        }
    }

    private IEnumerator CheckRightMouseHoldDuration()
    {
        while (isRightMouseDown && Time.time - rightMouseDownTime < LockThreshold)
        {
            yield return null;
        }

        if (isRightMouseDown)  // If the right mouse button is still being held down after the threshold
        {
            isRotating = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isRightMouseDown = true;
            buildMenu.Hide();
        }
        else  // If the right mouse button was released before the threshold
        {
            // Open the context menu
            OpenContextMenu();
        }
    }

    private void DesignatePoint(Vector3 point)
    {
        // Here you can handle what happens when you designate a point. 
        // For example, you might place a marker or instantiate some object at the clicked location.

        Debug.Log($"Designated Point: {point}");
    }

    private void OpenContextMenu()
    {
        buildMenu.Show(Input.mousePosition);
    }

    void DestroyExistingContextMenu()
{
    ContextMenu existingMenu = FindObjectOfType<ContextMenu>();
    if(existingMenu != null)
    {
        Destroy(existingMenu.gameObject);
    }
}
}