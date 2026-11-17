using UnityEngine;
using UnityEngine.InputSystem;

public class RTSCamera : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("How fast the camera moves (units per second) when zoomed to the start distance")]
    public float panSpeed = 30f;

    [Tooltip("Holding Left Shift multiplies the speed by this")]
    public float fastMultiplier = 2f;

    [Tooltip("Move the camera by pushing the mouse against the edge of the screen")]
    public bool edgeScrolling = true;

    [Tooltip("How many pixels from the screen edge count as 'the edge'")]
    public float edgeSize = 15f;

    [Tooltip("How strongly middle mouse dragging moves the map")]
    public float dragSpeed = 0.05f;

    [Header("Zoom")]
    public float minDistance = 12f;
    public float maxDistance = 70f;
    public float startDistance = 35f;

    [Tooltip("How much one scroll wheel notch zooms")]
    public float zoomSpeed = 4f;

    [Header("Angle")]
    [Range(30f, 80f)]
    [Tooltip("How far the camera looks down. Age of Empires is roughly 50-60")]
    public float pitch = 55f;

    public bool allowRotation = true;
    public float rotateSpeed = 90f;

    [Header("Feel")]
    [Tooltip("Higher = snappier, lower = smoother")]
    public float smoothing = 10f;

    [Tooltip("Extra room the camera is allowed past the edge of the map")]
    public float boundsPadding = 0f;

    // The point on the ground the camera is looking at
    private Vector3 focusPoint;
    private Vector3 targetFocus;
    private float distance;
    private float targetDistance;
    private float yaw;

    private Bounds mapBounds;
    private bool ready = false;
    private bool mouseHasMoved = false;

    private void Update()
    {
        //Wait untill the terrain is generated
        if (!ready)
        {
            if (!GenerationGrid.TerrainReady) return;
            SetupCamera();
        }

        if (Keyboard.current == null || Mouse.current == null) return;

        //So the camera still works if you pause the game later
        float dt = Time.unscaledDeltaTime;

        HandleMovement(dt);
        HandleZoom();
        HandleRotation(dt);
        ApplyCamera(dt);
    }

    //Runs once when the terrain is ready
    private void SetupCamera()
    {
        GenerationGrid grid = FindAnyObjectByType<GenerationGrid>();
        Renderer[] pieces = grid != null ? grid.GetComponentsInChildren<Renderer>() : new Renderer[0];

        if (pieces.Length > 0)
        {
            mapBounds = pieces[0].bounds;
            for (int i = 1; i < pieces.Length; i++)
            {
                mapBounds.Encapsulate(pieces[i].bounds);
            }
        }
        else
        {
            mapBounds = new Bounds(Vector3.zero, new Vector3(100f, 10f, 100f));
            Debug.LogWarning("RTSCamera could not measure the terrain - using a default size.");
        }

        //Start by looking at the tower, otherwise the middle of the map
        Tower tower = FindAnyObjectByType<Tower>();
        Vector3 start = tower != null ? tower.transform.position : mapBounds.center;
        start.y = mapBounds.center.y;

        focusPoint = start;
        targetFocus = start;
        distance = startDistance;
        targetDistance = startDistance;
        yaw = transform.eulerAngles.y;

        ready = true;
    }

    private void HandleMovement(float dt)
    {
        Vector2 input = Vector2.zero;

        //Keyboard
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;

        //Mouse at the edge of the screen
        Vector2 mousePos = Mouse.current.position.ReadValue();
        bool mouseInWindow = mousePos.x >= 0f && mousePos.x <= Screen.width &&
                     mousePos.y >= 0f && mousePos.y <= Screen.height;

        //Don't edge scroll until the player has actually moved the mouse
        if (!mouseHasMoved && Mouse.current.delta.ReadValue().sqrMagnitude > 0f)
        {
            mouseHasMoved = true;
        }

        if (edgeScrolling && mouseHasMoved && mouseInWindow && Application.isFocused)
        {
            if (mousePos.x <= edgeSize) input.x -= 1f;
            if (mousePos.x >= Screen.width - edgeSize) input.x += 1f;
            if (mousePos.y <= edgeSize) input.y -= 1f;
            if (mousePos.y >= Screen.height - edgeSize) input.y += 1f;
        }

        input = Vector2.ClampMagnitude(input, 1f);

        //Directions on the ground based on which way the camera is facing
        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 forward = yawRotation * Vector3.forward;
        Vector3 right = yawRotation * Vector3.right;

        //Move faster when zoomed out so it still feels the same
        float zoomScale = targetDistance / startDistance;
        float speed = panSpeed * zoomScale;
        if (Keyboard.current.leftShiftKey.isPressed) speed *= fastMultiplier;

        targetFocus += (right * input.x + forward * input.y) * speed * dt;

        //Middle mouse drag
        if (Mouse.current.middleButton.isPressed)
        {
            Vector2 drag = Mouse.current.delta.ReadValue();
            targetFocus -= (right * drag.x + forward * drag.y) * dragSpeed * zoomScale;
        }

        //Space = back to the tower
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Tower tower = FindAnyObjectByType<Tower>();
            if (tower != null)
            {
                targetFocus = new Vector3(tower.transform.position.x, mapBounds.center.y, tower.transform.position.z);
            }
        }

        //Keep the camera inside the map
        targetFocus.x = Mathf.Clamp(targetFocus.x, mapBounds.min.x - boundsPadding, mapBounds.max.x + boundsPadding);
        targetFocus.z = Mathf.Clamp(targetFocus.z, mapBounds.min.z - boundsPadding, mapBounds.max.z + boundsPadding);
    }

    private void HandleZoom()
    {
        //Scroll is about 120 per notch on Windows, so scale it down
        float scroll = Mouse.current.scroll.ReadValue().y * 0.01f;

        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
    }

    private void HandleRotation(float dt)
    {
        if (!allowRotation) return;

        if (Keyboard.current.qKey.isPressed) yaw -= rotateSpeed * dt;
        if (Keyboard.current.eKey.isPressed) yaw += rotateSpeed * dt;
    }

    //Moves the real camera to where it should be
    private void ApplyCamera(float dt)
    {
        float blend = 1f - Mathf.Exp(-smoothing * dt);

        focusPoint = Vector3.Lerp(focusPoint, targetFocus, blend);
        distance = Mathf.Lerp(distance, targetDistance, blend);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rotation;
        transform.position = focusPoint - rotation * Vector3.forward * distance;
    }
}
