using Shapes;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController2D : MonoBehaviour
{
    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 0.5f;
    public float maxZoom = 25f;

    [Header("Pan")]
    public float panSpeed = 1f;

    private Vector3 lastMousePos;
    private Camera cam;

	private float resetSize = 5f;
	private Vector3 resetPosition = new Vector3( 0, 0, 0 );

	// --- Double-click detection state ---
	private float lastLeftClickTime = 0f;
	private const float doubleClickThreshold = 0.25f; // tweak to taste

    void Awake()
    {
        cam = Camera.main;
		resetSize = cam.orthographicSize;
		resetPosition = cam.transform.position;
    }

    void Update()
    {
        HandleZoom();
        HandlePan();
		HandleDoubleClick();
    }

	void HandleZoom()
	{
		if (Mouse.current == null)
			return;

		float scroll = Mouse.current.scroll.ReadValue().y;
		if (Mathf.Approximately(scroll, 0f))
			return;

		Camera cam = Camera.main;

		// Zoom toward cursor: capture world pos before zoom
		Vector3 before = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

		// Scale zoom by current size
		float size = cam.orthographicSize;
		size -= scroll * zoomSpeed * size * Time.deltaTime;
		cam.orthographicSize = Mathf.Clamp(size, minZoom, maxZoom);

		// Maintain cursor focus
		Vector3 after = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		cam.transform.position += before - after;

		//ClampCameraToWorld();
	}

	/*void ClampCameraToWorld()
	{
		Camera cam = Camera.main;

		float vertExtent = cam.orthographicSize;
		float horzExtent = vertExtent * cam.aspect;

		float minX = worldBounds.xMin + horzExtent;
		float maxX = worldBounds.xMax - horzExtent;
		float minY = worldBounds.yMin + vertExtent;
		float maxY = worldBounds.yMax - vertExtent;

		Vector3 pos = cam.transform.position;
		pos.x = Mathf.Clamp(pos.x, minX, maxX);
		pos.y = Mathf.Clamp(pos.y, minY, maxY);
		cam.transform.position = pos;
	}*/

	private bool CheckDoubleClick()
	{
		if (Mouse.current == null)
			return false;

		if (Mouse.current.leftButton.wasPressedThisFrame)
		{
			float now = Time.time;

			if (now - lastLeftClickTime <= doubleClickThreshold)
			{
				lastLeftClickTime = 0f; // reset so triple-click doesn't count
				return true;
			}

			lastLeftClickTime = now;
		}

		return false;
	}

	void HandleDoubleClick() {
		if (Mouse.current == null)
			return;

		if( CheckDoubleClick() ) {
			cam.orthographicSize = resetSize;
			cam.transform.position = resetPosition;
		}
	}

    void HandlePan()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
            lastMousePos = Mouse.current.position.ReadValue();

        if (Mouse.current.rightButton.isPressed)
        {
            Vector3 currentPos = Mouse.current.position.ReadValue();
            Vector3 delta = currentPos - lastMousePos;
            lastMousePos = currentPos;

            Vector3 worldDelta =
                cam.ScreenToWorldPoint(delta) -
                cam.ScreenToWorldPoint(Vector3.zero);

            cam.transform.position -= worldDelta;
        }
    }
}
