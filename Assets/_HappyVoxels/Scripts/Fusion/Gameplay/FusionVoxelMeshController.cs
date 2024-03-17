using Cysharp.Threading.Tasks;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FusionVoxelMeshController : NetworkBehaviour
{
    [SerializeField]
    private float sphereCastRaduis = 2f;
    [SerializeField]
    private GameObject sphereAimPrefab;

    [Networked, Capacity(10), OnChangedRender(nameof(OnVoxelStateChanged))]
    private NetworkDictionary<string, bool> VoxelStates { get; } = MakeInitializer(new Dictionary<string, bool>());

    private VoxelMeshScene voxelMeshScene;

    private List<Vector4> editedVoxels = new();
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

            SingletonInterface.SingletonLocator.FusionPlayerManager.OnAnotherPlayerJoined += AnotherPlayerJoined;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        if (runner && HasStateAuthority)
        {
            SingletonInterface.SingletonLocator.FusionPlayerManager.OnAnotherPlayerJoined -= AnotherPlayerJoined;
        }
    }

    private void AnotherPlayerJoined(PlayerRef player)
    {
        SendEditData(player).Forget();
    }

    public void Initialize(FusionPlayer player) 
    {
        this.player = player;
        if (!sphereAim)
        {
            sphereAim = Instantiate(sphereAimPrefab);
        }
        sphereAim.SetActive(false);

        isInitialized = true;
    }

    private void OnVoxelStateChanged() 
    {
        UpdateVoxelState().Forget();
    }

    private async UniTask UpdateVoxelState() 
    {
        await UniTask.WaitUntil(() => { return voxelMeshScene && voxelMeshScene.IsInitialized && Object && Object.IsValid; });

        await UniTask.DelayFrame(1);
        if (voxelMeshScene)
        {
            foreach (var state in VoxelStates)
            {
                var controller = voxelMeshScene.GetMeshVoxelizerController(state.Key);
                if (state.Value)
                {
                    controller.Voxelize().Forget();
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
                                    Vector4 data = new Vector4(voxelMeshScene.GetMeshVoxelizerIndex(meshVoxelizer), closestNode.index, sphereCastRaduis, -1);
                                    editedVoxels.Add(data);
                                    RPC_EditVoxels(data);
                                    isEditing = true;
                                }

                                if (networkInputData.isSphereEnabling)
                                {
                                    Vector4 data = new Vector4(voxelMeshScene.GetMeshVoxelizerIndex(meshVoxelizer), closestNode.index, sphereCastRaduis, 1);
                                    editedVoxels.Add(data);
                                    RPC_EditVoxels(data);
                                    isEditing = true;
                                }
                            }                           
                        }
                    }
                    else
                    {
                        sphereAim.SetActive(false);
                        //Debug.LogError($"No closest node on {meshVoxelizer}");
                    }
                }
                else
                {
                    sphereAim.SetActive(false);
                    //Debug.LogError($"No tree nodes found on {meshVoxelizer}");
                }
            }
            else
            {
                sphereAim.SetActive(false);
                //Debug.LogError($"No mesh voxelizer on {hitInfo.collider.gameObject}");
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

    private async UniTask SendEditData(PlayerRef player) 
    {
        await UniTask.WaitForSeconds(3);
        List<float> data = new List<float>();
        for (int i = 0; i < editedVoxels.Count; i++)
        {
            data.Add(editedVoxels[i].x);
            data.Add(editedVoxels[i].y);
            data.Add(editedVoxels[i].z);
            data.Add(editedVoxels[i].w);
        }

        int counter = 0;
        NetworkBuffer networkBuffer = new NetworkBuffer();
        int index = 0;
        for (int i = 0; i < data.Count; i++)
        {
            if (counter >= 32)
            {
                RPC_SendEditData(player, networkBuffer, 32);
                await UniTask.DelayFrame(2);
                counter = 0;
                index = 0;
                networkBuffer = new NetworkBuffer();
            }
            index = networkBuffer.Record(data[i], index);
            counter++;
        }

        RPC_SendEditData(player, networkBuffer, counter);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SendEditData([RpcTarget] PlayerRef player, NetworkBuffer data, int dataCount) 
    {
        Vector4 editData = Vector4.zero;
        int counter = 0;
        for (int j = 0; j < dataCount; j++)
        {
            if (counter >= 4)
            {
                EditVoxels(editData);
                counter = 0;
                editData = Vector4.zero;
            }
            var f = data.GetFloat(j * sizeof(float));
            editData[counter++] = f;
        }

        if (!editedVoxels.Contains(editData))
        {
            editedVoxels.Add(editData);
            EditVoxels(editData);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EditVoxels(Vector4 data)
    {
        EditVoxels(data);
    }

    private void EditVoxels(Vector4 data) 
    {
        MeshVoxelizer meshVoxelizer = voxelMeshScene.GetMeshVoxelizerAtIndex((int)data.x);
        TreeNode closestNode = meshVoxelizer.GetNodeAt((int)data.y);
        bool disable = data.w < 0;
        if (disable) 
        {
            meshVoxelizer.DisableNodesInSphere(closestNode, (int)data.z);
        }
        else
        {
            meshVoxelizer.EnableNodesInSphere(closestNode, (int)data.z);
        }
    }
}
