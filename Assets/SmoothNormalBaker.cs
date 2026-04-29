using UnityEngine;
using AquaSys.SmoothNormals;

public class SmoothNormalBaker : MonoBehaviour
{
    [SerializeField] private int uvChannel = 2; // Maps to UV2 in Shader Graph

    [ContextMenu("Bake Smooth Normals")]
    void Bake()
    {
        Mesh mesh = null;
        Renderer targetRenderer = null;

        // 1. Check for SkinnedMeshRenderer (Characters)
        if (TryGetComponent(out SkinnedMeshRenderer smr))
        {
            mesh = Instantiate(smr.sharedMesh);
            targetRenderer = smr;
            smr.sharedMesh = mesh;
        }
        // 2. Check for MeshFilter (Static Props)
        else if (TryGetComponent(out MeshFilter mf))
        {
            mesh = Instantiate(mf.sharedMesh);
            targetRenderer = GetComponent<MeshRenderer>();
            mf.mesh = mesh;
        }

        if (mesh == null)
        {
            Debug.LogError("No Mesh found on this GameObject!");
            return;
        }

        // 3. Compute smoothed normals as Vector3 (easier for Shader Graph)
        Vector3[] smoothedNormals = AquaSmoothNormals.ComputeSmoothedNormalsV3(mesh);

        if (smoothedNormals != null)
        {
            // SetUVs(2, ...) maps to the "UV2" selection in Shader Graph
            mesh.SetUVs(uvChannel, smoothedNormals);
            Debug.Log($"Successfully baked smoothed normals to UV{uvChannel} on {gameObject.name}");
        }
    }
}