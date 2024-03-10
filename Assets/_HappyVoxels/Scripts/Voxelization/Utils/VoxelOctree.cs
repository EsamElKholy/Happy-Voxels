using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class VoxelOctree 
{
    public TreeNode[] Nodes;
    public Vector3[] VoxelUVs;
    public List<TreeNode> FilledNodes = new List<TreeNode>();
    public int MaxDepth;
    public float MaxSize;
    public int NodeCount;

    public VoxelOctree(Vector3 position, float size, int maxDepth)
    {
        MaxDepth = maxDepth;
        int s = 0;
        for (int k = 0; k <= MaxDepth; k++)
        {
            s += (int)Mathf.Pow(8, k);
        }
        NodeCount = s;
        Nodes = new TreeNode[NodeCount];
        VoxelUVs = Enumerable.Repeat(new Vector3(-999, -999, -1), NodeCount).ToArray();

        TreeNode root = new TreeNode()
        {
            position = position,
            size = size,
            Value = 0,
            index = 0,
            currentDepth = 0,
            firstChild = -1,
            parent = -1,
            firstLeaf = 0,
            lastLeaf = 0
        };

        MaxSize = size;
        if (maxDepth > 0)
        {
            int sum = 0;
            for (int k = 0; k <= MaxDepth - 1; k++)
            {
                sum += (int)Mathf.Pow(8, k);
            }
            root.firstLeaf = sum;
            int stride = (int)Mathf.Pow(8, MaxDepth - root.currentDepth);
            root.lastLeaf = root.firstLeaf + stride - 1;
            root.firstChild = 1;
        }
        else
        {
            root.firstLeaf = 0;
            root.lastLeaf = 0;
            root.firstChild = -1;
        }

        Nodes[0] = (root);
    }

    public void Dispose() 
    {
        VoxelUVs = null;
        Nodes = null;
    }

    public void BuildFullTree()
    {
        TreeNode root = Nodes[0];
        Nodes = new TreeNode[NodeCount];
        Nodes[0] = (root);

        for (int i = 1; i < Nodes.Length; i++)
        {
            int a = 1;
            int b = 0;
            int index = i;
            int currentDepth = 0;
            for (int j = 1; j <= MaxDepth; j++)
            {
                a += (int)Mathf.Pow(8, j);
                b += (int)Mathf.Pow(8, j - 1);

                if (index >= b && index < a)
                {
                    currentDepth = j;
                    break;
                }
            }

            int range = (int)Mathf.Pow(8, currentDepth);

            TreeNode node = new TreeNode();
            node.index = i;
            node.parent = (i - 1) / 8;
            node.currentDepth = currentDepth;

            if (currentDepth == MaxDepth)
            {
                node.firstChild = -1;
                node.firstLeaf = -1;
                node.lastLeaf = -1;
            }
            else
            {
                int previousIndex = 0;

                previousIndex = 0;
                int s = 0;
                for (int k = 0; k < node.currentDepth; k++)
                {
                    s += (int)Mathf.Pow(8, k);
                }
                previousIndex = s;

                node.firstChild = range + 1 + ((node.index - previousIndex) * 8);
                int stride = (int)Mathf.Pow(8, MaxDepth - node.currentDepth);
                node.firstLeaf = 0;
                int sum = 0;
                for (int k = 0; k <= MaxDepth - 1; k++)
                {
                    sum += (int)Mathf.Pow(8, k);
                }
                sum += (node.index - previousIndex) * (int)Mathf.Pow(8, MaxDepth - node.currentDepth);
                node.firstLeaf = sum;
                node.lastLeaf = node.firstLeaf + stride - 1;
            }

            node.size = MaxSize / Mathf.Pow(2, currentDepth);
            node.position = Nodes[node.parent].position;

            if (((i % 8) & 4) == 4)
            {
                node.position.y += node.size / 2;
            }
            else
            {
                node.position.y -= node.size / 2;
            }

            if (((i % 8) & 2) == 2)
            {
                node.position.x += node.size / 2;
            }
            else
            {
                node.position.x -= node.size / 2;
            }

            if (((i % 8) & 1) == 1)
            {
                node.position.z += node.size / 2;
            }
            else
            {
                node.position.z -= node.size / 2;
            }

            node.Value = 0;

            Nodes[i] = (node);
        }
    }

    public List<TreeNode> CheckRay(Ray ray, Transform transform)
    {
        List<TreeNode> hitNodes = new List<TreeNode>();
        int min = 0;

        for (int i = 0; i <= MaxDepth - 1; i++)
        {
            min += (int)Mathf.Pow(8, i);
        }

        for (int i = min; i < Nodes.Length;)
        {
            int parentIndex = Nodes[i].parent;
            int lastSkip = 0;
            while (parentIndex != -1)
            {
                var parent = Nodes[parentIndex];
                Bounds bounds = new Bounds(transform.TransformPoint(parent.position), Vector3.one * parent.size);
                int intersecting = bounds.IntersectRay(ray) ? 1 : 0;

                if (intersecting == 1)
                {
                    break;
                }
                else
                {
                    lastSkip = parent.lastLeaf;
                    parentIndex = parent.parent;
                }
            }

            if (lastSkip == 0)
            {
                for (int j = i; j < i + 8; j++)
                {
                    Bounds bounds = new Bounds(transform.TransformPoint(Nodes[j].position), Vector3.one * Nodes[j].size);
                    float distance = 0;
                    int intersecting = bounds.IntersectRay(ray, out distance) ? 1 : 0;
                    if (intersecting == 1)
                    {
                        var node = Nodes[j];

                        if (node.Value > 0 && distance > node.size * 1.5f)
                        {
                            hitNodes.Add(node);
                        }
                    }
                }

                i += 8;
            }
            else
            {
                i = lastSkip + 1;
            }
        }

        return hitNodes;
    }

    public List<TreeNode> CastSphere(Vector3 center, float raduis)
    {
        List<TreeNode> nodes = new List<TreeNode>();

        float maxDistance = raduis + FilledNodes[0].size / 2;

        for (int i = 0; i < FilledNodes.Count; i++)
        {
            float distance = Vector3.Distance(center, FilledNodes[i].position);

            if (distance <= maxDistance)
            {
                nodes.Add(FilledNodes[i]);
            }
        }

        return nodes;
    }

    public List<TreeNode> GetNode(Vector3 position)
    {
        List<TreeNode> result = new List<TreeNode>();

        int min = 0;

        for (int i = 0; i <= MaxDepth - 1; i++)
        {
            min += (int)Mathf.Pow(8, i);
        }

        for (int i = min; i < Nodes.Length;)
        {
            int parentIndex = Nodes[i].parent;
            int lastSkip = 0;
            while (parentIndex != -1)
            {
                var parent = Nodes[parentIndex];
                Bounds bounds = new Bounds(parent.position, Vector3.one * parent.size);
                int intersecting = bounds.Contains(position) ? 1 : 0;

                if (intersecting == 1)
                {
                    break;
                }
                else
                {
                    lastSkip = parent.lastLeaf;
                    parentIndex = parent.parent;
                }
            }

            if (lastSkip == 0)
            {
                for (int j = i; j < i + 8; j++)
                {
                    Bounds bounds = new Bounds(Nodes[j].position, Vector3.one * Nodes[j].size);

                    int intersecting = bounds.Contains(position) ? 1 : 0;
                    if (intersecting == 1)
                    {
                        var node = Nodes[j];

                        if (node.Value == 0)
                        {
                            {
                                result.Add(node);
                            }
                        }
                    }
                }

                i += 8;
            }
            else
            {
                i = lastSkip + 1;
            }
        }

        return result;
    }

    public void CheckTriangles(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        int min = 0;

        for (int i = 0; i <= MaxDepth - 1; i++)
        {
            min += (int)Mathf.Pow(8, i);
        }

        for (int i = min; i < Nodes.Length;)
        {
            int parentIndex = Nodes[i].parent;
            int lastSkip = 0;
            while (parentIndex != -1)
            {
                var parent = Nodes[parentIndex];
                int intersecting = GeometryUtils.TriangleBoxIntersection(new Vector3[] { v0, v1, v2 }, parent.position, Vector3.one * parent.size);

                if (intersecting == 1)
                {
                    break;
                }
                else
                {
                    lastSkip = parent.lastLeaf;
                    parentIndex = parent.parent;
                }
            }

            if (lastSkip == 0)
            {
                for (int j = i; j < i + 8; j++)
                {
                    int intersecting = GeometryUtils.TriangleBoxIntersection(new Vector3[] { v0, v1, v2 }, Nodes[j].position, Vector3.one * Nodes[j].size);

                    if (intersecting == 1)
                    {
                        var node = Nodes[j];
                        if (node.Value == 0)
                        {
                            node.Value = 1;
                            FilledNodes.Add(node);
                        }

                        node.Value = 1;
                        Nodes[j] = node;
                    }
                }

                i += 8;
            }
            else
            {
                i = lastSkip + 1;
            }
        }
    }

    public List<TreeNode> GetFilledNodes(bool recalculate = false)
    {
        if (recalculate)
        {
            List<TreeNode> filledNodes = new List<TreeNode>();
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].Value == 1)
                {
                    filledNodes.Add(Nodes[i]);
                }
            }

            FilledNodes = filledNodes;

            return FilledNodes;
        }
        return FilledNodes;
    }

    public void DrawTree()
    {
        Color minColor = new Color(1, 1, 1, 1f);
        Color maxColor = new Color(0, 0.5f, 1, 0.25f);

        for (int i = 0; i < Nodes.Length; i++)
        {
            TreeNode node = Nodes[i];

            Gizmos.color = Color.Lerp(minColor, maxColor, node.currentDepth / (float)MaxDepth);
            Gizmos.DrawWireCube(node.position, Vector3.one * node.size);
        }
    }

    public int GetNodeSize()
    {
        int size = 0;
        size += sizeof(int) * 7;
        size += sizeof(float);
        size += sizeof(float) * 3;

        return size;
    }
}
