using UnityEngine;
using UnityEngine.Rendering;

[AddComponentMenu("Debug/Runtime Collider Visualizer")]
[ExecuteAlways]
public class RuntimeColliderVisualizer : MonoBehaviour
{
    [Header("Display Settings")]
    public bool showInGameView = true;
    public KeyCode toggleKey = KeyCode.F1;

    [Header("Colors")]
    public Color characterControllerColor = new Color(0f, 1f, 0.2f, 0.9f);
    public Color staticColliderColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    public Color triggerColor = new Color(1f, 0.8f, 0f, 0.8f);

    private static Material _lineMaterial;

    private void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showInGameView = !showInGameView;
        }
    }

    private static void CreateLineMaterial()
    {
        if (!_lineMaterial)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            _lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 0);
            _lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }
    }

    // URP Hook: Guarantees rendering on top of Game View in Unity 6 Universal Render Pipeline
    private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!showInGameView) return;

        CreateLineMaterial();
        _lineMaterial.SetPass(0);

        GL.PushMatrix();

        // 1. Draw all CharacterControllers
        CharacterController[] controllers = FindObjectsByType<CharacterController>(FindObjectsSortMode.None);
        foreach (var cc in controllers)
        {
            if (!cc.enabled || !cc.gameObject.activeInHierarchy) continue;
            DrawWireCapsule(cc.transform, cc.center, cc.radius, cc.height, characterControllerColor);
        }

        // 2. Draw all BoxColliders
        BoxCollider[] boxes = FindObjectsByType<BoxCollider>(FindObjectsSortMode.None);
        foreach (var b in boxes)
        {
            if (!b.enabled || !b.gameObject.activeInHierarchy) continue;
            Color col = b.isTrigger ? triggerColor : staticColliderColor;
            DrawWireBox(b.transform, b.center, b.size, col);
        }

        // 3. Draw all SphereColliders
        SphereCollider[] spheres = FindObjectsByType<SphereCollider>(FindObjectsSortMode.None);
        foreach (var s in spheres)
        {
            if (!s.enabled || !s.gameObject.activeInHierarchy) continue;
            Color col = s.isTrigger ? triggerColor : staticColliderColor;
            DrawWireSphere(s.transform, s.center, s.radius, col);
        }

        // 4. Draw all CapsuleColliders
        CapsuleCollider[] capsules = FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None);
        foreach (var cap in capsules)
        {
            if (!cap.enabled || !cap.gameObject.activeInHierarchy) continue;
            Color col = cap.isTrigger ? triggerColor : staticColliderColor;
            DrawWireCapsule(cap.transform, cap.center, cap.radius, cap.height, col);
        }

        GL.PopMatrix();
    }

    // Built-in fallback
    private void OnRenderObject()
    {
        if (GraphicsSettings.currentRenderPipeline == null)
        {
            OnEndCameraRendering(default, Camera.current);
        }
    }

    private void DrawWireBox(Transform t, Vector3 center, Vector3 size, Color color)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);

        Vector3 half = size * 0.5f;
        Vector3[] p = new Vector3[]
        {
            t.TransformPoint(center + new Vector3(-half.x, -half.y, -half.z)),
            t.TransformPoint(center + new Vector3( half.x, -half.y, -half.z)),
            t.TransformPoint(center + new Vector3( half.x, -half.y,  half.z)),
            t.TransformPoint(center + new Vector3(-half.x, -half.y,  half.z)),
            t.TransformPoint(center + new Vector3(-half.x,  half.y, -half.z)),
            t.TransformPoint(center + new Vector3( half.x,  half.y, -half.z)),
            t.TransformPoint(center + new Vector3( half.x,  half.y,  half.z)),
            t.TransformPoint(center + new Vector3(-half.x,  half.y,  half.z))
        };

        // Bottom
        Line(p[0], p[1]); Line(p[1], p[2]); Line(p[2], p[3]); Line(p[3], p[0]);
        // Top
        Line(p[4], p[5]); Line(p[5], p[6]); Line(p[6], p[7]); Line(p[7], p[4]);
        // Vertical
        Line(p[0], p[4]); Line(p[1], p[5]); Line(p[2], p[6]); Line(p[3], p[7]);

        GL.End();
    }

    private void DrawWireSphere(Transform t, Vector3 center, float radius, Color color)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);

        int segments = 24;
        float step = Mathf.PI * 2f / segments;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * step;
            float a2 = (i + 1) * step;
            Line(t.TransformPoint(center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * radius),
                 t.TransformPoint(center + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2), 0) * radius));
            Line(t.TransformPoint(center + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius),
                 t.TransformPoint(center + new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * radius));
        }

        GL.End();
    }

    private void DrawWireCapsule(Transform t, Vector3 center, float radius, float height, Color color)
    {
        GL.Begin(GL.LINES);
        GL.Color(color);

        float halfHeight = Mathf.Max(0f, (height * 0.5f) - radius);
        Vector3 topCenter = center + Vector3.up * halfHeight;
        Vector3 bottomCenter = center - Vector3.up * halfHeight;

        int segments = 16;
        float step = Mathf.PI * 2f / segments;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * step;
            float a2 = (i + 1) * step;
            Line(t.TransformPoint(topCenter + new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius)),
                 t.TransformPoint(topCenter + new Vector3(Mathf.Cos(a2) * radius, 0, Mathf.Sin(a2) * radius)));
            Line(t.TransformPoint(bottomCenter + new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius)),
                 t.TransformPoint(bottomCenter + new Vector3(Mathf.Cos(a2) * radius, 0, Mathf.Sin(a2) * radius)));
        }

        Line(t.TransformPoint(topCenter + Vector3.right * radius), t.TransformPoint(bottomCenter + Vector3.right * radius));
        Line(t.TransformPoint(topCenter - Vector3.right * radius), t.TransformPoint(bottomCenter - Vector3.right * radius));
        Line(t.TransformPoint(topCenter + Vector3.forward * radius), t.TransformPoint(bottomCenter + Vector3.forward * radius));
        Line(t.TransformPoint(topCenter - Vector3.forward * radius), t.TransformPoint(bottomCenter - Vector3.forward * radius));

        for (int i = 0; i < segments / 2; i++)
        {
            float a1 = i * step;
            float a2 = (i + 1) * step;
            Line(t.TransformPoint(topCenter + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * radius),
                 t.TransformPoint(topCenter + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2), 0) * radius));
            Line(t.TransformPoint(bottomCenter + new Vector3(Mathf.Cos(a1), -Mathf.Sin(a1), 0) * radius),
                 t.TransformPoint(bottomCenter + new Vector3(Mathf.Cos(a2), -Mathf.Sin(a2), 0) * radius));
        }

        GL.End();
    }

    private static void Line(Vector3 a, Vector3 b)
    {
        GL.Vertex(a);
        GL.Vertex(b);
    }
}
