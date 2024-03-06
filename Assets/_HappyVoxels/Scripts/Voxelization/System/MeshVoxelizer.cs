using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Experimental.GraphView;
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
    private const string constructMeshKernel = "ConstructMesh";
    private const string constructMeshKernel1 = "ConstructMesh1";
    private const string buildTreeKernel = "BuildTree";
    private const string sphereOrigin = "SpherecastOrigin";
    private const string sphereRaduis = "SphereRadius";
    private const string enableVoxelsInSphereKernel = "EnableVoxelsInSphere";
    private const string disableVoxelsInSphereKernel = "DisableVoxelsInSphere";

    [Range(1, 6)]
    [SerializeField]
    private int treeDepth = 1;

    [SerializeField]
    private ComputeShader voxelComputeShader;
    [SerializeField]
    private Material geometryMaterial;

    private MeshFilter meshFilter;
    private VoxelOctree voxelOctree;
    private int currentDepth = 0;
    private float maxBoundsSize = 0;

    [Button]
    private void Voxelize() 
    {
        if (!meshFilter)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshFilter)
        {
            maxBoundsSize = Mathf.Max(meshFilter.mesh.bounds.size.x, meshFilter.mesh.bounds.size.y, meshFilter.mesh.bounds.size.z);

            voxelOctree = new VoxelOctree(meshFilter.mesh.bounds.center, maxBoundsSize, treeDepth);

            BuildTree();
            FillTree();
            UpdateFilledNodes();
            ConstructMesh();
        }
        else
        {
            Debug.LogError($"{nameof(MeshVoxelizer)}: {nameof(Voxelize)}: Failed to find mesh filter!");
        }
    }


    private void BuildTree()
    {
        var voxelBuffer = new ComputeBuffer(voxelOctree.Nodes.Length, Marshal.SizeOf(typeof(Node)));
        voxelBuffer.SetData(voxelOctree.Nodes);

        voxelComputeShader.SetFloat(maxSize, voxelOctree.MaxSize);
        voxelComputeShader.SetInt(maxDepth, voxelOctree.MaxDepth);
        voxelComputeShader.SetInt(voxelCount, voxelOctree.Nodes.Length);

        int kernel = voxelComputeShader.FindKernel(buildTreeKernel);

        voxelComputeShader.SetBuffer(kernel, voxelOctreeBuffer, voxelBuffer);

        voxelComputeShader.Dispatch(kernel, voxelOctree.NodeCount / 64 + 1, 1, 1);
        voxelBuffer.GetData(voxelOctree.Nodes);

        voxelBuffer.Dispose();
    }

    private void FillTree()
    {
        var voxelBuffer = new ComputeBuffer(voxelOctree.Nodes.Length, Marshal.SizeOf(typeof(Node)));
        voxelBuffer.SetData(voxelOctree.Nodes);

        MeshFilter filter = GetComponent<MeshFilter>();

        var verts = filter.mesh.vertices;
        int vCount = verts.Length;
        int vSize = Marshal.SizeOf(typeof(Vector3));
        var vertBuffer = new ComputeBuffer(vCount, vSize);
        vertBuffer.SetData(verts);

        var inds = filter.mesh.triangles;
        int indCount = inds.Length;
        int indSize = sizeof(int);
        var indBuffer = new ComputeBuffer(indCount, indSize);
        indBuffer.SetData(inds);

        voxelComputeShader.SetInt(indexCount, indCount);

        int kernel = voxelComputeShader.FindKernel(fillTreeKernel);

        voxelComputeShader.SetBuffer(kernel, vertexBuffer, vertBuffer);
        voxelComputeShader.SetBuffer(kernel, indexBuffer, indBuffer);
        voxelComputeShader.SetBuffer(kernel, voxelOctreeBuffer, voxelBuffer);

        voxelComputeShader.Dispatch( kernel, (indCount / (3 * 64)) + 1, 1, 1);

        voxelBuffer.GetData(voxelOctree.Nodes);

        voxelBuffer.Dispose();
        indBuffer.Dispose();
        vertBuffer.Dispose();
    }

    public void UpdateFilledNodes()
    {
        var voxelBuffer = new ComputeBuffer(voxelOctree.Nodes.Length, Marshal.SizeOf(typeof(Node)));
        voxelBuffer.SetData(voxelOctree.Nodes);

        var filledVoxelsBuffer = new ComputeBuffer(voxelOctree.NodeCount, Marshal.SizeOf(typeof(Node)), ComputeBufferType.Append);
        filledVoxelsBuffer.SetCounterValue(0);
        
        int kernel = voxelComputeShader.FindKernel(getFilledVoxelsKernel);

        voxelComputeShader.SetBuffer(kernel, voxelOctreeBuffer, voxelBuffer);
        voxelComputeShader.SetBuffer(kernel, filledVoxelPositionsBuffer, filledVoxelsBuffer);

        voxelComputeShader.Dispatch(kernel, voxelOctree.NodeCount / 64 + 1, 1, 1);

        int[] counter = new int[1] { 0 };
        ComputeBuffer appendBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        appendBuffer.SetData(counter);
        ComputeBuffer.CopyCount(filledVoxelsBuffer, appendBuffer, 0);

        appendBuffer.GetData(counter);

        TreeNode[] nodes = new TreeNode[counter[0]];
        filledVoxelsBuffer.GetData(nodes);

        voxelOctree.FilledNodes = new List<TreeNode>(nodes);

        filledVoxelsBuffer.Dispose();
        appendBuffer.Dispose();
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
        var kernel = voxelComputeShader.FindKernel(constructMeshKernel1);
        voxelComputeShader.SetBuffer(kernel, outVertexBuffer, vertBuffer);
        voxelComputeShader.SetBuffer(kernel, outIndexBuffer, indBuffer);
        voxelComputeShader.SetBuffer(kernel, _filledVoxelPositionsBuffer, filledVoxels);
        voxelComputeShader.Dispatch(kernel, voxelOctree.FilledNodes.Count / 64 + 1, 1, 1);

        var renderer = GetComponent<MeshRenderer>();
        renderer.material = geometryMaterial;
        geometryMaterial.SetFloat("_VoxelSize", voxelOctree.MaxSize / Mathf.Pow(2, voxelOctree.MaxDepth));
        meshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.mesh.Clear();
        vertBuffer.GetData(v);
        indBuffer.GetData(ind);
        meshFilter.mesh.vertices = v;
        meshFilter.mesh.triangles = ind;
        meshFilter.GetComponent<Renderer>().bounds.Expand(1000);

        filledVoxels.Dispose();
        indBuffer.Dispose();
        vertBuffer.Dispose();
    }
}
