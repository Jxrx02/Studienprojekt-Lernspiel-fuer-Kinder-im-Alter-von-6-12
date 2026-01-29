using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;


public class CameraZoom : MonoBehaviour
{
    [SerializeField] CameraBounds2D bounds;
    Vector2 maxXPositions, maxYPositions;

    Vector3 touchStart;
    public float zoomOutMin = 1;
    public float zoomOutMax = 8;
    public float speed = 1;

    private PixelPerfectCamera pixelPerfectCamera;
    private float currentZoomLevel;

    void Awake()
    {
        // Initialisiere die Grenzen
        if (bounds != null)
        {
            bounds.Initialize(GetComponent<Camera>());
            maxXPositions = bounds.maxXlimit;
            maxYPositions = bounds.maxYlimit;

        }

        // Pixel Perfect Camera Setup
        pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
        currentZoomLevel = pixelPerfectCamera.assetsPPU; // Initialwert des Pixels pro Einheit
    }

    void Update()
    {
        if (IsOverUI()) return;

        // Touch-Eingabe: Pan
        if (Input.GetMouseButtonDown(0))
        {
            touchStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        else if (Input.touchCount == 2) // Zoom mit Touch
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            Zoom(difference * 0.01f);
        }
        else if (Input.GetMouseButton(0)) // Kamera verschieben
        {
            Vector3 direction = touchStart - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 currentPosition = transform.position;
            Vector3 targetPosition = new Vector3(
                Mathf.Clamp(currentPosition.x + direction.x, maxXPositions.x, maxXPositions.y),
                Mathf.Clamp(currentPosition.y + direction.y, maxYPositions.x, maxYPositions.y),
                currentPosition.z
            );
            transform.position = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * speed);
        }

        // Scrollen mit Maus
        Zoom(Input.GetAxis("Mouse ScrollWheel"));
    }

    bool IsOverUI()
    {
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        else
            return EventSystem.current.IsPointerOverGameObject();
    }

    void Zoom(float increment)
    {
        // Berechne neues Zoom-Level und passe die Grenzen an
        float newZoomLevel = Mathf.Clamp(
            currentZoomLevel + (increment * 10),
            zoomOutMin,
            zoomOutMax 
        );

        // Glattere Zoom-Bewegung
        currentZoomLevel = Mathf.MoveTowards(currentZoomLevel, newZoomLevel, Time.deltaTime * speed * 100);
        
        // Kamera-Zentrum bleibt Fokuspunkt
        Vector3 zoomCenter = transform.position;
        transform.position += (zoomCenter - transform.position) * (1 - (currentZoomLevel / newZoomLevel));

        // Aktualisiere Pixelgröße
        pixelPerfectCamera.assetsPPU = Mathf.RoundToInt(currentZoomLevel);

        // Begrenzungen neu berechnen
        if (bounds == null)
            return;
        bounds.CalculateBounds();
        maxXPositions = bounds.maxXlimit;
        maxYPositions = bounds.maxYlimit;
    }



}
