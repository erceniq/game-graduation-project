using UnityEditor;
using MCPForUnity.Editor.Services;

namespace MCPForUnity.Editor
{
    [InitializeOnLoad]
    public static class McpAutoStarter
    {
        static McpAutoStarter()
        {
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            EditorApplication.delayCall += () =>
            {
                if (MCPServiceLocator.Bridge != null)
                {
                    _ = MCPServiceLocator.Bridge.StartAsync();
                }
            };
        }
    }
}
