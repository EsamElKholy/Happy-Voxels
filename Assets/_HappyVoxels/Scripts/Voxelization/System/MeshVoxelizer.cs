using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class MeshVoxelizer : MonoBehaviour
{
    private const string voxelCount = "VoxelCount";
    private const string maxDepth = "MaxDepth";
    private const string maxSize = "MaxSize";
    private const string vertexBuffer = "VertexBuffer";
    private const string indexBuffer = "IndexBuffer";
    private const string indexCount = "IndexCount";
    private const string voxelOctreeBuffer = "VoxelOctreeBuffer";
    private const string fillTreeKernel = "FillTree";
    private const string filledVoxelPositionsBuffer = "FilledVoxelPositionsBuffer";
    private const string filledVoxelsCount = "FilledVoxelsCount";
    private const string getFilledVoxelsKernel = "GetFilledVoxels";
    private const string outVertexBuffer = "OutVerticesBuffer";
    private const string outIndexBuffer = "OutIndicesBuffer";
    private const string _filledVoxelPositionsBuffer = "_FilledVoxelPositionsBuffer";
    private const string constructCubeMeshKernel = "ConstructCubeMesh";
    private const string constructPointMeshKernel = "ConstructPointMesh";
    private const string buildTreeKernel = "BuildTree";
    private const string sphereOrigin = "SpherecastOrigin";
    private const string sphereRadius = "SphereRadius";
    private const string enableVoxelsInSphereKernel = "EnableVoxelsInSphere";
    private const string disableVoxelsInSphereKernel = "DisableVoxelsInSphere";

    [Range(1, 10)]
    [SerializeField]
    private int treeDepth = 1;

    [SerializeField]
    private ComputeShader voxelComputeShader;
    [SerializeField]
    private List<Material> geometryMaterials = new();
    [SerializeField]
    private bool skinnedMeshRendererMode;
    [SerializeField]
    private float voxelizedScale = 1;

    private Material[] defaultMaterials;
    private new MeshRenderer renderer;
    private Mesh originalMesh;
    private Mesh voxelMesh;
    private MeshFilter voxelMeshFilter;
    private VoxelOctree voxelOctree;
    private float maxBoundsSize = 0;

    private bool isVoxelized = false;
    private int currentDepth = 0;
    private Deform deform;

    private void Awake()
    {
        if (skinnedMeshRendererMode)
        {
            var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            
            GameObject meshCopy = new GameObject(gameObject.name + "_MeshCopy");
            meshCopy.transform.SetParent(transform);
            meshCopy.transform.localPosition = Vector3.zero;
            meshCopy.transform.localRotation = Quaternion.identity;
            meshCopy.transform.localScale = Vector3.one;

            voxelMeshFilter = meshCopy.AddComponent<MeshFilter>();
            voxelMeshFilter.sharedMesh = skinnedMeshRenderer.sharedMesh;

            renderer = meshCopy.AddComponent<MeshRenderer>();
            renderer.materials = skinnedMeshRenderer.materials;

            defaultMaterials = renderer.materials;
            skinnedMeshRenderer.enabled = false;
        }
    }

    public void SetTreeDepth(int depth) 
    {
        treeDepth = depth;
    }

    public void Voxelize(int depth) 
    {
        SetTreeDepth(depth);
        Voxelize();
    }

    [Button]
    public void Voxelize() 
    {
        if (isVoxelized && currentDepth == treeDepth)
        {
            return;
        }       

        isVoxelized = true;
        currentDepth = treeDepth;
        transform.GetChild(0).gameObject.SetActive(true);
        if (voxelMesh)
        {
            voxelMesh.Clear();
        }
        
        voxelMesh = new Mesh();

        if (!voxelMeshFilter)
        {
            voxelMeshFilter = GetComponent<MeshFilter>();      
        }

        if (!originalMesh)
        {
            originalMesh = voxelMeshFilter.mesh;
        }

        if (!renderer)
        {
            renderer = GetComponent<MeshRenderer>();
        }

        if (!deform)
        {
            deform = GetComponentInChildren<Deform>();
        }

        voxelMeshFilter.mesh = voxelMesh;        

        Material[] copyMaterials = new Material[geometryMaterials.Count];
        for (int i = 0; i < copyMaterials.Length; i++) 
        {
            copyMaterials[i] = new Material(geometryMaterials[i]);
        }

        renderer.materials = copyMaterials;

        var slicer = GetComponent<Slicer>();

        if (slicer != null)
        {
            slicer.OnFinishedSlicing += FinishedSlicing;
            slicer.ResetPlanePosition(false);
        }

        if (originalMesh)
        {
            maxBoundsSize = Mathf.Max(originalMesh.bounds.size.x, originalMesh.bounds.size.y, originalMesh.bounds.size.z);

            voxelOctree = new VoxelOctree(originalMesh.bounds.center, maxBoundsSize, treeDepth);

            BuildTree();
            FillTree();
            UpdateFilledNodes();
            ConstructMesh();

            if (slicer != null)
            {
                slicer.StartSlicing();
            }
        }
        else
        {
            Debug.LogError($"{nameof(MeshVoxelizer)}: {nameof(Voxelize)}: Failed to find original mesh!");
        }
    }

    private void FinishedSlicing() 
    {
        var slicer = GetComponent<Slicer>();

        if (slicer != null)
        {
            slicer.OnFinishedSlicing -= FinishedSlicing;
        }

        transform.GetChild(0).gameObject.SetActive(false);

        if (deform)
        {
            deform.StartAnimation().Forget();
        }
    }

    [Button]
    public void ResetToOriginal() 
    {
        if (originalMesh)
        {
            if (voxelOctree != null && voxelOctree.NodeCount > 0)
            {
                voxelMeshFilter.mesh.Clear();
                voxelMeshFilter.mesh = originalMesh;

                renderer.materials = defaultMaterials;
                isVoxelized = false;
            }
        }
    }

    private void BuildTree()
    {
        var voxelBuffer = new ComputeBuffer(voxelOctree.Nodes.Length, Marshal.SizeOf(typeof(TreeNode)));
        voxelBuffer.SetData(voxelOctree.Nodes);

        voxelComputeShader.SetFloat(maxSize, voxelOctree.MaxSize);
        voxelComputeShader.SetInt(maxDepth, voxelOctree.MaxDepth);
        voxelComputeShader.SetInt(voxelCount, voxelOctree.Nodes.Length);

        int kernel = voxelComputeShader.FindKernel(buildTreeKernel);
        uint threadGroupX, threadGroupY, threadGroupZ;
        voxelComputeShader.GetKernelThreadGroupSizes(kernel, out threadGroupX, out threadGroupY, out threadGroupZ);

        voxelComputeShader.SetBuffer(kernel, voxelOctreeBuffer, voxelBuffer);

        voxelComputeShader.Dispatch(kernel, voxelOctree.NodeCount / (int)threadGroupX + 1, 1, 1);
        voxelBuffer.GetData(voxelOctree.Nodes);

        voxelBuffer.Dispose();
    }

    private void FillTree()
    {
        var voxelBuffer = new ComputeBuffer(voxelOctree.Nodes.Length, Marshal.SizeOf(typeof(TreeNode)));
        voxelBuffer.SetData(voxelOctree.Nodes);

        var verts = originalMesh.vertices;
        int vCount = verts.Length;
        int vSize = Marshal.SizeOf(typeof(Vector3));
        
        var vertBuffer = new ComputeBuffer(vCount, vSize);
        vertBuffer.SetData(verts);

        var inds = originalMesh.triangles;
        int indCount = inds.Length;
        int indSize = sizeof(int);

        var indBuffer = new ComputeBuffer(indCount, indSize);
        indBuffer.SetData(inds);

        voxelComputeShader.SetInt(indexCount, indCount);

        int kernel = voxelComputeShader.FindKernel(fillTreeKernel);

        voxelComputeShader.SetBuffer(kernel, vertexBuffer, vertBuffer);
        voxelComputeShader.SetBuffer(kernel, indexBuffer, indBuffer);
        voxelComputeShader.SetBuffer(kernel, voxelOctreeBuffer, voxelBuffer);

        uint threadGroupX, threadGroupY, threadGroupZ;
        voxelComputeShader.GetKernelThreadGroupSizes(kernel, out threadGroupX, out threadGroupY, out threadGroupZ);

        voxelComputeShader.Dispatch( kernel, (indCount / (3 * (int)threadGroupX)) + 1, 1, 1);

        voxelBuffer.GetData(voxelOctree.Nodes);

        voxelBuffer.Dispose();
        indBuffer.Dispose();
        vertBuffer.Dispose();
    }

    public void UpdateFilledNodes()
    {
        var voxelBuffer = new ComputeBuffer(voxelOctree.Nodes.Length, Marshal.SizeOf(typeof(TreeNode)));
        voxelBuffer.SetData(voxelOctree.Nodes);

        var filledVoxelsBuffer = new ComputeBuffer(voxelOctree.Nodes.Length, Marshal.SizeOf(typeof(TreeNode)), ComputeBufferType.Append);
        filledVoxelsBuffer.SetCounterValue(0);        
        
        int kernel = voxelComputeShader.FindKernel(getFilledVoxelsKernel);

        voxelComputeShader.SetBuffer(kernel, voxelOctreeBuffer, voxelBuffer);
        voxelComputeShader.SetBuffer(kernel, filledVoxelPositionsBuffer, filledVoxelsBuffer);

        uint threadGroupX, threadGroupY, threadGroupZ;
        voxelComputeShader.GetKernelThreadGroupSizes(kernel, out threadGroupX, out threadGroupY, out threadGroupZ);

        voxelComputeShader.Dispatch(kernel, voxelOctree.NodeCount / (int)threadGroupX + 1, 1, 1);

        int[] counter = new int[1] { 0 };
        ComputeBuffer appendBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        appendBuffer.SetData(counter);
        ComputeBuffer.CopyCount(filledVoxelsBuffer, appendBuffer, 0);

        appendBuffer.GetData(counter);

        TreeNode[] nodes = new TreeNode[counter[0]];
        filledVoxelsBuffer.GetData(nodes);
        if (nodes.Length > 0) 
        {
            voxelOctree.FilledNodes = new List<TreeNode>(nodes);        
        }
        appendBuffer.Dispose();
        filledVoxelsBuffer.Dispose();
        voxelBuffer.Dispose();
    }

    private void ConstructMesh()
    {
        var filledVoxels = new ComputeBuffer(voxelOctree.FilledNodes.Count, Marshal.SizeOf(typeof(TreeNode)));
        filledVoxels.SetData(voxelOctree.FilledNodes);

        var indBuffer = new ComputeBuffer(voxelOctree.FilledNodes.Count * 3, sizeof(int));
        var ind = new int[voxelOctree.FilledNodes.Count * 3];
        
        var vertBuffer = new ComputeBuffer(voxelOctree.FilledNodes.Count, Marshal.SizeOf(typeof(Vector3)));
        var v = new Vector3[voxelOctree.FilledNodes.Count];

        voxelComputeShader.SetInt(filledVoxelsCount, voxelOctree.FilledNodes.Count);

        var kernel = voxelComputeShader.FindKernel(constructPointMeshKernel);

        voxelComputeShader.SetBuffer(kernel, outVertexBuffer, vertBuffer);
        voxelComputeShader.SetBuffer(kernel, outIndexBuffer, indBuffer);
        voxelComputeShader.SetBuffer(kernel, _filledVoxelPositionsBuffer, filledVoxels);

        uint threadGroupX, threadGroupY, threadGroupZ;
        voxelComputeShader.GetKernelThreadGroupSizes(kernel, out threadGroupX, out threadGroupY, out threadGroupZ);

        voxelComputeShader.Dispatch(kernel, voxelOctree.FilledNodes.Count / (int)threadGroupX + 1, 1, 1);

        foreach (var material in renderer.materials)
        {
            material.SetFloat("_VoxelSize", voxelOctree.MaxSize / Mathf.Pow(2, voxelOctree.MaxDepth));
        }

        voxelMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        voxelMesh.Clear();

        vertBuffer.GetData(v);
        indBuffer.GetData(ind);

        voxelMesh.vertices = v;
        voxelMesh.triangles = ind;

        filledVoxels.Dispose();
        indBuffer.Dispose();
        vertBuffer.Dispose();
    }

    public void UpdateMesh()
    {
        ComputeBuffer filledVoxels = new ComputeBuffer(voxelOctree.FilledNodes.Count, Marshal.SizeOf(typeof(TreeNode)));
        filledVoxels.SetData(voxelOctree.FilledNodes);

        var indBuffer = new ComputeBuffer(voxelOctree.FilledNodes.Count * 3, sizeof(int));
        var ind = new int[voxelOctree.FilledNodes.Count * 3];

        var vertBuffer = new ComputeBuffer(voxelOctree.FilledNodes.Count, Marshal.SizeOf(typeof(Vector3)));
        var v = new Vector3[voxelOctree.FilledNodes.Count];

        var kernel = voxelComputeShader.FindKernel(constructPointMeshKernel);

        voxelComputeShader.SetInt(filledVoxelsCount, voxelOctree.FilledNodes.Count);

        voxelComputeShader.SetBuffer(kernel, outVertexBuffer, vertBuffer);
        voxelComputeShader.SetBuffer(kernel, outIndexBuffer, indBuffer);

        voxelComputeShader.SetBuffer(kernel, _filledVoxelPositionsBuffer, filledVoxels);

        voxelComputeShader.Dispatch(kernel, voxelOctree.FilledNodes.Count / 64 + 1, 1, 1);

        voxelMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        voxelMesh.Clear();

        vertBuffer.GetData(v);
        indBuffer.GetData(ind);

        voxelMesh.vertices = v;
        voxelMesh.triangles = ind;

        filledVoxels.Dispose();
        indBuffer.Dispose();
        vertBuffer.Dispose();
    }

    public int GetFilledNodeCount() 
    {
        if (voxelOctree != null)
        {
            return voxelOctree.FilledNodes.Count;
        }

        return 0;
    }

    public int GetTotalVoxelCount() 
    {
        if (voxelOctree != null)
        {
            return voxelOctree.NodeCount;
        }

        return 0;
    }

    public void EnableNodesInSphere(TreeNode closestNode, float radius)
    {
        var voxelBuffer = new ComputeBuffer(voxelOctree.Nodes.Length, Marshal.SizeOf(typeof(TreeNode)));
        voxelBuffer.SetData(voxelOctree.Nodes);
        voxelComputeShader.SetVector(sphereOrigin, closestNode.position);
        voxelComputeShader.SetFloat(sphereRadius, radius / voxelizedScale);

        voxelComputeShader.SetBuffer(voxelComputeShader.FindKernel(enableVoxelsInSphereKernel), voxelOctreeBuffer, voxelBuffer);
        voxelComputeShader.Dispatch(voxelComputeShader.FindKernel(enableVoxelsInSphereKernel), voxelOctree.NodeCount / 64 + 1, 1, 1);
        voxelBuffer.GetData(voxelOctree.Nodes);

        voxelBuffer.Dispose();

        UpdateFilledNodes();
        UpdateMesh();
    }

    public void DisableNodesInSphere(TreeNode closestNode, float radius)
    {
        var nodesInSphere = CastSphere(closestNode.position, radius);
        if (nodesInSphere.Count > 0)
        {
            foreach (var node in nodesInSphere)
            {
                voxelOctree.Nodes[node.index].Value = -1;
                voxelOctree.FilledNodes.Remove(voxelOctree.FilledNodes.Find(x => x.index == node.index));
            }
            voxelOctree.Nodes[closestNode.index].Value = -1;
            voxelOctree.FilledNodes.Remove(voxelOctree.FilledNodes.Find(x => x.index == closestNode.index));

            UpdateMesh();
        }
    }

    public List<TreeNode> CheckRay(Ray ray) 
    {
        List<TreeNode> result = new List<TreeNode>();
        if (voxelOctree != null && voxelOctree.Nodes.Length > 0)
        {
            result = voxelOctree.CheckRay(ray, transform, voxelizedScale);
        }

        return result;
    }

    public List<TreeNode> CastSphere(Vector3 position, float radius) 
    {
        List<TreeNode> result = new List<TreeNode>();
        if (voxelOctree != null && voxelOctree.Nodes.Length > 0)
        {
            result = voxelOctree.CastSphere(position, radius / voxelizedScale);
        }

        return result;
    }

    public TreeNode GetNodeAt(int index)
    {
        return voxelOctree.GetNodeAt(index);
    }
}
