using UnityEngine;

public class TreeNode
{
    public int index;
    public int currentDepth;
    public int firstChild;
    public int parent;
    public int firstLeaf;
    public int lastLeaf;

    public Vector3 position;
    public float size;
    private int value;

    public TreeNode() { }

    public TreeNode(int index, int currentDepth, int firstChild, int parent, int firstLeaf, int lastLeaf, Vector3 position, float size, int value)
    {
        this.index = index;
        this.currentDepth = currentDepth;
        this.firstChild = firstChild;
        this.parent = parent;
        this.firstLeaf = firstLeaf;
        this.lastLeaf = lastLeaf;
        this.position = position;
        this.size = size;
        this.value = value;
    }

    public int Value { get => value; set => this.value = value; }
}
