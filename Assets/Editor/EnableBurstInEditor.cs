#if UNITY_EDITOR
using UnityEditor;
using Unity.Burst;

[InitializeOnLoad]
static class EnableBurstInEditor
{
    static EnableBurstInEditor()
    {
        BurstCompiler.Options.EnableBurstCompilation = true;
    }
}
#endif