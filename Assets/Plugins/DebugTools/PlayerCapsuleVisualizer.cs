using UnityEngine;

/// <summary>
/// Attached to Player. Uses dynamic LineRenderer loops to guarantee collider wireframe
/// is 100% visible in Game View under Unity 6 Universal Render Pipeline (URP).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(CharacterController))]
public class PlayerCapsuleVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color wireColor = Color.green;
    [Range(0.01f, 0.1f)] public float lineWidth = 0.035f;

    private CharacterController _cc;
    private LineRenderer _lineRenderer;
    private Material _lineMat;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        SetupLineRenderer();
    }

    void OnEnable()
    {
        if (_lineRenderer != null) _lineRenderer.enabled = true;
    }

    void OnDisable()
    {
        if (_lineRenderer != null) _lineRenderer.enabled = false;
    }

    private void SetupLineRenderer()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");

        _lineMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        if (_lineMat.HasProperty("_BaseColor")) _lineMat.SetColor("_BaseColor", wireColor);
        if (_lineMat.HasProperty("_Color")) _lineMat.SetColor("_Color", wireColor);

        _lineRenderer.material = _lineMat;
        _lineRenderer.startColor = wireColor;
        _lineRenderer.endColor = wireColor;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        _lineRenderer.useWorldSpace = false;
        _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;
    }

    void LateUpdate()
    {
        if (_cc == null) _cc = GetComponent<CharacterController>();
        if (_cc == null || _lineRenderer == null) return;

        UpdateWireframePoints();
    }

    private void UpdateWireframePoints()
    {
        float radius = _cc.radius;
        float height = _cc.height;
        Vector3 center = _cc.center;

        float halfHeight = Mathf.Max(0f, (height * 0.5f) - radius);
        Vector3 top = center + Vector3.up * halfHeight;
        Vector3 bottom = center - Vector3.up * halfHeight;

        int segments = 16;
        // Total points:
        // Top ring: segments + 1
        // Down line: 1
        // Bottom ring: segments + 1
        // Side loops: 4 vertical lines
        var points = new System.Collections.Generic.List<Vector3>();

        // 1. Top Ring
        for (int i = 0; i <= segments; i++)
        {
            float rad = (i % segments) * (Mathf.PI * 2f / segments);
            points.Add(top + new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius));
        }

        // 2. Down to bottom ring
        points.Add(bottom + new Vector3(radius, 0, 0));

        // 3. Bottom Ring
        for (int i = 0; i <= segments; i++)
        {
            float rad = (i % segments) * (Mathf.PI * 2f / segments);
            points.Add(bottom + new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius));
        }

        // 4. Vertical Front line up
        points.Add(bottom + new Vector3(0, 0, radius));
        points.Add(top + new Vector3(0, 0, radius));

        // 5. Vertical Back line down
        points.Add(top + new Vector3(0, 0, -radius));
        points.Add(bottom + new Vector3(0, 0, -radius));

        // 6. Vertical Left line up
        points.Add(bottom + new Vector3(-radius, 0, 0));
        points.Add(top + new Vector3(-radius, 0, 0));

        _lineRenderer.positionCount = points.Count;
        _lineRenderer.SetPositions(points.ToArray());
    }
}
