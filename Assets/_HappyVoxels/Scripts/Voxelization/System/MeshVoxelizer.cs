using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class MeshVoxelizer : MonoBehaviour
{
    private const string voxelCount = "VoxelCount";
    private const string maxDepth = "MaxDepth";
    private const string maxSize = "MaxSize";
    private const string vertexBuffer = "VertexBuffer";
    private const string UVsBuffer = "UVsBuffer";
    private const string voxelUVsBuffer = "voxelUVsBuffer";
    private const string indexBuffer = "IndexBuffer";
    private const string indexCount = "IndexCount";
    private const string voxelOctreeBuffer = "VoxelOctreeBuffer";
    private const string fillTreeKernel = "FillTree";
    private const string filledVoxelPositionsBuffer = "FilledVoxelPositionsBuffer";
    private const string filledVoxelUVsBuffer = "FilledVoxelUVsBuffer";
    private const string filledVoxelsCount = "FilledVoxelsCount";
    private const string getFilledVoxelsKernel = "GetFilledVoxels";
    private const string getFilledVoxelsUVsKernel = "GetFilledVoxelsUVs";
    private const string outVertexBuffer = "OutVerticesBuffer";
    private const string outIndexBuffer = "OutIndicesBuffer";
    private const string outUVsBuffer = "OutUVsBuffer";
    private const string _filledVoxelPositionsBuffer = "_FilledVoxelPositionsBuffer";
    private const string constructMeshKernel = "ConstructMesh";
    private const string constructMeshKernel1 = "ConstructMesh1";
    private const string buildTreeKernel = "BuildTree";
    private const string sphereOrigin = "SpherecastOrigin";
    private const string sphereRaduis = "SphereRadius";
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
    private Vector2[] UVCache;

    private bool isVoxelized = false;
    private int currentDepth = 0;

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

        voxelMeshFilter.mesh = voxelMesh;

        Material[] copyMaterials = new Material[geometryMaterials.Count];
        for (int i = 0; i < copyMaterials.Length; i++) 
        {
            copyMaterials[i] = new Material(geometryMaterials[i]);
        }

        renderer.materials = copyMaterials;       

        if (originalMesh)
        {
            maxBoundsSize = Mathf.Max(originalMesh.bounds.size.x, originalMesh.bounds.size.y, originalMesh.bounds.size.z);

            voxelOctree = new VoxelOctree(originalMesh.bounds.center, maxBoundsSize, treeDepth);

            BuildTree();
            FillTree();
            UpdateFilledNodes();
            ConstructMesh();
        }
        else
        {
            Debug.LogError($"{nameof(MeshVoxelizer)}: {nameof(Voxelize)}: Failed to find original mesh!");
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

        var voxelUVBuffer = new ComputeBuffer(voxelOctree.VoxelUVs.Length, Marshal.SizeOf(typeof(Vector3)));
        voxelUVBuffer.SetData(voxelOctree.VoxelUVs);

        var uvsBuffer = new ComputeBuffer(vCount, Marshal.SizeOf(typeof(Vector2)));
        uvsBuffer.SetData(originalMesh.uv);

        var inds = originalMesh.triangles;
        int indCount = inds.Length;
        int indSize = sizeof(int);

        var indBuffer = new ComputeBuffer(indCount, indSize);
        indBuffer.SetData(inds);

        voxelComputeShader.SetInt(indexCount, indCount);

        int kernel = voxelComputeShader.FindKernel(fillTreeKernel);

        voxelComputeShader.SetBuffer(kernel, vertexBuffer, vertBuffer);

        voxelComputeShader.SetBuffer(kernel, UVsBuffer, uvsBuffer);
        voxelComputeShader.SetBuffer(kernel, voxelUVsBuffer, voxelUVBuffer);

        voxelComputeShader.SetBuffer(kernel, indexBuffer, indBuffer);
        voxelComputeShader.SetBuffer(kernel, voxelOctreeBuffer, voxelBuffer);

        uint threadGroupX, threadGroupY, threadGroupZ;
        voxelComputeShader.GetKernelThreadGroupSizes(kernel, out threadGroupX, out threadGroupY, out threadGroupZ);

        voxelComputeShader.Dispatch( kernel, (indCount / (3 * (int)threadGroupX)) + 1, 1, 1);

        voxelBuffer.GetData(voxelOctree.Nodes);
        voxelUVBuffer.GetData(voxelOctree.VoxelUVs);

        voxelBuffer.Dispose();
        indBuffer.Dispose();
        vertBuffer.Dispose();
        uvsBuffer.Dispose();
        voxelUVBuffer.Dispose();
    }

    public void UpdateFilledNodes()
    {
        var voxelBuffer = new ComputeBuffer(voxelOctree.Nodes.Length, Marshal.SizeOf(typeof(TreeNode)));
        voxelBuffer.SetData(voxelOctree.Nodes);

        var filledVoxelsBuffer = new ComputeBuffer(voxelOctree.NodeCount, Marshal.SizeOf(typeof(TreeNode)), ComputeBufferType.Append);
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

        voxelOctree.FilledNodes = new List<TreeNode>(nodes);

        appendBuffer.Dispose();

        filledVoxelsBuffer.Dispose();
        appendBuffer.Dispose();
        voxelBuffer.Dispose();
        
        GenerateUVs();
    }

    private void GenerateUVs() 
    {
        var voxelUVBuffer = new ComputeBuffer(voxelOctree.VoxelUVs.Length, Marshal.SizeOf(typeof(Vector3)));
        voxelUVBuffer.SetData(voxelOctree.VoxelUVs);

        var filledVoxelUVs = new ComputeBuffer(voxelOctree.VoxelUVs.Length, Marshal.SizeOf(typeof(Vector2)), ComputeBufferType.Append);
        filledVoxelUVs.SetCounterValue(0);

        var kernel = voxelComputeShader.FindKernel(getFilledVoxelsUVsKernel);

        voxelComputeShader.SetBuffer(kernel, voxelUVsBuffer, voxelUVBuffer);
        voxelComputeShader.SetBuffer(kernel, filledVoxelUVsBuffer, filledVoxelUVs);

        uint threadGroupX, threadGroupY, threadGroupZ;
        voxelComputeShader.GetKernelThreadGroupSizes(kernel, out threadGroupX, out threadGroupY, out threadGroupZ); 

        voxelComputeShader.Dispatch(kernel, voxelOctree.VoxelUVs.Length / (int)threadGroupX + 1, 1, 1);

        int[] counter = new int[1] { 0 };
        ComputeBuffer appendBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        appendBuffer.SetData(counter);
        ComputeBuffer.CopyCount(filledVoxelUVs, appendBuffer, 0);

        appendBuffer.GetData(counter);

        UVCache = new Vector2[counter[0]];
        filledVoxelUVs.GetData(UVCache);

        voxelOctree.FilledNodesUVs = new List<Vector2>(UVCache);

        appendBuffer.Dispose();
        filledVoxelUVs.Dispose();
        voxelUVBuffer.Dispose();
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

        var kernel = voxelComputeShader.FindKernel(constructMeshKernel1);

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

        voxelMesh.uv = UVCache;
        UVCache = null;

        filledVoxels.Dispose();
        indBuffer.Dispose();
        vertBuffer.Dispose();

        //voxelOctree.Dispose();
    }

    public void UpdateMesh()
    {
        ComputeBuffer filledVoxels = new ComputeBuffer(voxelOctree.FilledNodes.Count, Marshal.SizeOf(typeof(TreeNode)));
        filledVoxels.SetData(voxelOctree.FilledNodes);
        var indBuffer = new ComputeBuffer(voxelOctree.FilledNodes.Count * 3, sizeof(int));
        var ind = new int[voxelOctree.FilledNodes.Count * 3];
        //indBuffer1.SetData(ind);
        var vertBuffer = new ComputeBuffer(voxelOctree.FilledNodes.Count, Marshal.SizeOf(typeof(Vector3)));
        var v = new Vector3[voxelOctree.FilledNodes.Count];
        //vertBuffer1.SetData(v);
        var kernel = voxelComputeShader.FindKernel(constructMeshKernel1);
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

        voxelMesh.uv = UVCache;

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

    public void EnableNodesInSphere(TreeNode closestNode, float raduis, bool useGPU)
    {
        var position = transform.TransformPoint(closestNode.position);
        if (useGPU)
        {
            var voxelBuffer = new ComputeBuffer(voxelOctree.Nodes.Length, Marshal.SizeOf(typeof(TreeNode)));
            voxelBuffer.SetData(voxelOctree.Nodes);
            voxelComputeShader.SetVector(sphereOrigin, position);
            voxelComputeShader.SetFloat(sphereRaduis, raduis);

            voxelComputeShader.SetBuffer(voxelComputeShader.FindKernel(enableVoxelsInSphereKernel), voxelOctreeBuffer, voxelBuffer);
            voxelComputeShader.Dispatch(voxelComputeShader.FindKernel(enableVoxelsInSphereKernel), voxelOctree.NodeCount / 64 + 1, 1, 1);
            voxelBuffer.GetData(voxelOctree.Nodes);

            voxelBuffer.Dispose();
        }
        else
        {
            var nodesInSphere = CastSphere(position, raduis);
            if (nodesInSphere.Count > 0)
            {
                foreach (var node in nodesInSphere)
                {
                    voxelOctree.Nodes[node.index].Value = 1;
                }

                voxelOctree.Nodes[closestNode.index].Value = 1;
                var index = nodesInSphere.Select(x => x.index).ToList();
                index.Add(closestNode.index);
                voxelOctree.ActivateUVAt(index.ToArray(), true);


                UpdateFilledNodes();
                UpdateMesh();
            }           
        }
    }

    public void DisableNodesInSphere(TreeNode closestNode, float raduis, bool useGPU)
    {
        var position = transform.TransformPoint(closestNode.position);
        if (useGPU)
        {
            var voxelBuffer = new ComputeBuffer(voxelOctree.Nodes.Length, Marshal.SizeOf(typeof(TreeNode)));
            voxelBuffer.SetData(voxelOctree.Nodes);
            ComputeBuffer filledVoxels = new ComputeBuffer(voxelOctree.FilledNodes.Count, Marshal.SizeOf(typeof(TreeNode)));
            filledVoxels.SetData(voxelOctree.FilledNodes);

            voxelComputeShader.SetInt(filledVoxelsCount, voxelOctree.FilledNodes.Count);
            voxelComputeShader.SetVector(sphereOrigin, position);
            voxelComputeShader.SetFloat(sphereRaduis, raduis);

            voxelComputeShader.SetBuffer(voxelComputeShader.FindKernel(disableVoxelsInSphereKernel), _filledVoxelPositionsBuffer, filledVoxels);
            voxelComputeShader.SetBuffer(voxelComputeShader.FindKernel(disableVoxelsInSphereKernel), voxelOctreeBuffer, voxelBuffer);
            voxelComputeShader.Dispatch(voxelComputeShader.FindKernel(disableVoxelsInSphereKernel), voxelOctree.FilledNodes.Count / 64 + 1, 1, 1);
            voxelBuffer.GetData(voxelOctree.Nodes);

            filledVoxels.Dispose();
            voxelBuffer.Dispose();
        }
        else
        {
            var nodesInSphere = CastSphere(closestNode.position, raduis);
            if (nodesInSphere.Count > 0)
            {
                foreach (var node in nodesInSphere) 
                {
                    voxelOctree.Nodes[node.index].Value = 0;
                }
                voxelOctree.Nodes[closestNode.index].Value = 0;
                var index = nodesInSphere.Select(x => x.index).ToList();
                index.Add(closestNode.index);
                voxelOctree.ActivateUVAt(index.ToArray(), false);


                UpdateFilledNodes();
                UpdateMesh();
            }
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
