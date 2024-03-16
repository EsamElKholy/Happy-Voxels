using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class FusionVoxelMeshController : NetworkBehaviour
{
    [SerializeField]
    private float sphereCastRaduis = 2f;
    [SerializeField]
    private GameObject sphereAimPrefab;

    [Networked, Capacity(10), OnChangedRender(nameof(OnVoxelStateChanged))]
    private NetworkDictionary<string, bool> VoxelStates { get; } = MakeInitializer(new Dictionary<string, bool>());

    private VoxelMeshScene voxelMeshScene;

    private List<Vector2> addedVoxels = new List<Vector2>();
    private List<Vector2> removedVoxels = new List<Vector2>();
    private GameObject sphereAim;

    private bool isInitialized = false;
    private FusionPlayer player;
    private Ray cameraRay;
    private float editCooldown = 0.5f;
    private float editCooldownCounter = 0;
    private bool isEditing = false;

    public override void Spawned()
    {
        base.Spawned();

        voxelMeshScene = FindAnyObjectByType<VoxelMeshScene>();

        if (HasStateAuthority)
        {
            if (VoxelStates.Count == 0)
            {
                var names = voxelMeshScene.GetMeshVoxelizerControllerNames();
                foreach (var name in names) 
                {
                    VoxelStates.Add(name, voxelMeshScene.StartVoxelized);                
                }
            }
        }
    }

    public void Initialize(FusionPlayer player) 
    {
        this.player = player;
        sphereAim = Instantiate(sphereAimPrefab);
        sphereAim.SetActive(false);

        isInitialized = true;
    }

    private void OnVoxelStateChanged() 
    {
        if (voxelMeshScene)
        {
            foreach (var state in VoxelStates) 
            {
                var controller = voxelMeshScene.GetMeshVoxelizerController(state.Key);            
                if (state.Value)
                {
                    controller.Voxelize(); 
                }
                else
                {
                    controller.ResetToOriginal();
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!isInitialized)
        {
            return;
        }

        UpdateCameraRay(player.LocalCamera);

        if (Physics.Raycast(cameraRay, out RaycastHit hitInfo, layerMask: 1 << LayerMask.NameToLayer("VoxelMesh"), maxDistance: Mathf.Infinity))
        {
            var meshVoxelizer = hitInfo.collider.GetComponent<MeshVoxelizer>();

            if (meshVoxelizer != null) 
            {
                var treeNodes = meshVoxelizer.CheckRay(cameraRay);

                if (treeNodes.Count > 0)
                {
                    sphereAim.SetActive(true);

                    var closestNode = GetClosestNode(ref treeNodes, transform.position, meshVoxelizer.transform);

                    if (closestNode.index >= 0)
                    {
                        var pos = meshVoxelizer.transform.TransformPoint(closestNode.position);

                        sphereAim.transform.position = pos;
                        sphereAim.transform.localScale = Vector3.one * sphereCastRaduis * 2;
                        sphereAim.gameObject.SetActive(!isEditing);

                        if (GetInput(out NetworkInputData networkInputData))
                        {
                            if (!isEditing)
                            {
                                if (networkInputData.isSphereDisabling)
                                {
                                    meshVoxelizer.DisableNodesInSphere(closestNode, sphereCastRaduis, false);
                                    isEditing = true;
                                }

                                //if (networkInputData.isSphereDisabling)
                                //{
                                //    meshVoxelizer.EnableNodesInSphere(closestNode, sphereCastRaduis, false);
                                //    isEditing = true;
                                //}
                            }                           
                        }
                    }
                    else
                    {
                        sphereAim.SetActive(false);
                        Debug.LogError($"No closest node on {meshVoxelizer}");
                    }
                }
                else
                {
                    sphereAim.SetActive(false);
                    Debug.LogError($"No tree nodes found on {meshVoxelizer}");
                }
            }
            else
            {
                sphereAim.SetActive(false);
                Debug.LogError($"No mesh voxelizer on {hitInfo.collider.gameObject}");
            }
        }
        else
        {
            sphereAim.SetActive(false);
        }

        if (isEditing)
        {
            editCooldownCounter += Runner.DeltaTime;

            if (editCooldownCounter >= editCooldown)
            {
                editCooldownCounter = 0;
                isEditing = false;
            }
        }
    }

    private TreeNode GetClosestNode(ref List<TreeNode> nodes, Vector3 targetPosition, Transform target)
    {
        if (nodes.Count == 1)
        {
            return nodes[0];
        }
        else if (nodes.Count > 1)
        {
            float distance = 9999999999;
            int index = -1;

            for (int i = 0; i < nodes.Count; i++)
            {
                float dist = Vector3.Distance(targetPosition, target.TransformPoint(nodes[i].position));

                if (dist < distance)
                {
                    distance = dist;
                    index = i;
                }
            }

            if (index >= 0)
            {
                return nodes[index];
            }
        }

        TreeNode nullNode = new TreeNode();
        nullNode.index = -1;

        return nullNode;
    }

    private void UpdateCameraRay(Camera camera) 
    {
        cameraRay.origin = camera.transform.position;
        cameraRay.direction = camera.transform.forward;
    }
}
