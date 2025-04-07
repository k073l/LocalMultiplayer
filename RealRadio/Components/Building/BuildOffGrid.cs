using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using RealRadio;
using ScheduleOne;
using ScheduleOne.Building;
using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.Interaction;
using ScheduleOne.ItemFramework;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Property;
using ScheduleOne.Tiles;
using ScheduleOne.UI;
using UnityEngine;

public class BuildStartOffGrid : BuildStart_Base
{
    public LayerMask DetectionMask = (Layers.Default | Layers.Tile | Layers.Terrain).ToLayerMask();

    /// <summary>
    /// The optimal placement angle in degrees, for the X and Z axes.
    /// </summary>
    public float? OptimalRotationXZ = 0f;

    /// <summary>
    /// Max allowed rotation offset in degrees.
    /// </summary>
    public float AllowedRotationLenience = 15f;

    /// <summary>
    /// Rotation speed in increments per keypress
    /// </summary>
    public float RotationIncrement = 360f / 16f;

    /// <summary>
    /// Whether or not to restrict placement to owned properties
    /// </summary>
    public bool RestrictToProperties = true;

    public GameObject? GhostObject { get; private set; }
    public BuildableItem? BuildableItem { get; private set; }
    public ItemInstance? ItemInstance { get; private set; }
    public BuildableItemDefinition? ItemDefinition
    {
        get
        {
            if (ItemInstance == null)
                return null;

            return ItemInstance.Definition as BuildableItemDefinition ?? throw new InvalidOperationException("ItemInstance.Definition is not BuildableItemDefinition");
        }
    }

    public override void StartBuilding(ItemInstance item)
    {
        Plugin.Logger.LogInfo($"BuildStartOffGrid.StartBuilding {item}");

        ItemInstance = item;

        if (item.Definition is not BuildableItemDefinition itemDef)
        {
            Plugin.Logger.LogError($"item.Definition is not BuildableItemDefinition: {item.Definition}");
            return;
        }

        if (GhostObject != null)
            throw new InvalidOperationException("StartBuilding was called while a ghost object already exists");

        HUD hud = Singleton<HUD>.Instance;
        hud.SetCrosshairVisible(false);

        InputPromptsCanvas inputPrompts = Singleton<InputPromptsCanvas>.Instance;
        inputPrompts.LoadModule("building");

        GhostObject = CreateGhostModel(itemDef);
        BuildableItem = GhostObject.GetComponent<BuildableItem>() ?? throw new InvalidOperationException("BuildableItem component not found on BuiltItem prefab");
    }

    public void DestroyGhostObject()
    {
        if (GhostObject == null)
            return;

        Destroy(GhostObject);
        GhostObject = null;
        BuildableItem = null;
    }

    private GameObject CreateGhostModel(BuildableItemDefinition itemDef)
    {
        itemDef.BuiltItem.isGhost = true;
        var prefab = itemDef.BuiltItem.gameObject;
        var result = Instantiate(prefab, parent: transform);
        itemDef.BuiltItem.isGhost = false;

        var buildManager = BuildManager.Instance;
        buildManager.DisableColliders(result);
        buildManager.DisableNavigation(result);
        buildManager.DisableNetworking(result);
        buildManager.DisableCanvases(result);

        foreach (var component in gameObject.GetComponentsInChildren<ActivateDuringBuild>())
        {
            component.gameObject.SetActive(true);
        }

        return result;
    }
}

public class BuildUpdateOffGrid : BuildUpdate_Base
{
    private BuildStartOffGrid buildStart = null!;

    private GameObject GhostObject => buildStart?.GhostObject ?? throw new InvalidOperationException("buildStart or buildStart.GhostObject is null");

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastValidPosition;
    private Quaternion lastValidRotation;
    private float desiredRotation;
    private bool positionIsValid; // also implies rotation is valid
    private float sqrDistanceDiff;
    private Collider[] intersections = new Collider[8];
    private Material? currentGhostMaterial;

    private const float MaxSnapDistanceSqr = 0.1f * 0.1f;

    public override void Stop()
    {
        Plugin.Logger.LogInfo("BuildUpdateOffGrid.Stop");
        buildStart.DestroyGhostObject();
    }

    private void Awake()
    {
        buildStart = GetComponent<BuildStartOffGrid>() ?? throw new InvalidOperationException("No BuildStartOffGrid component found on game object");
    }

    private void Update()
    {
        if (buildStart.GhostObject == null)
            return;

        UpdateDesiredRotationFromInput();
        UpdatePositions();

        sqrDistanceDiff = (lastPosition - lastValidPosition).sqrMagnitude;

        if (sqrDistanceDiff > MaxSnapDistanceSqr)
        {
            lastValidPosition = Vector3.zero;
        }

        UpdateGhostTransform();
        UpdateGhostMaterial();
        SpawnItemIfValid();
    }

    private void UpdateDesiredRotationFromInput()
    {
        if (GameInput.GetButtonDown(GameInput.ButtonCode.RotateLeft))
        {
            desiredRotation -= buildStart.RotationIncrement;
        }

        if (GameInput.GetButtonDown(GameInput.ButtonCode.RotateRight))
        {
            desiredRotation += buildStart.RotationIncrement;
        }
    }

    private void UpdatePositions()
    {
        if (buildStart.BuildableItem == null)
            throw new InvalidOperationException("buildStart.BuildableItem is null");

        float range = buildStart.BuildableItem.HoldDistance;
        PlayerCamera playerCamera = PlayerSingleton<PlayerCamera>.Instance;

        if (!playerCamera.LookRaycast(range, out var hit, buildStart.DetectionMask, includeTriggers: false))
        {
            lastPosition = playerCamera.transform.position + playerCamera.transform.forward * range;
            lastPosition -= buildStart.BuildableItem.MidAirCenterPoint.localPosition;
            lastRotation = playerCamera.transform.rotation * Quaternion.Euler(0, 180, 0);
            positionIsValid = false;
            return;
        }

        lastRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0, desiredRotation, 0);
        lastPosition = hit.point - buildStart.BuildableItem.BuildPoint.localPosition;

        if (TestForObstructions())
        {
            positionIsValid = false;
            return;
        }

        if (!TestForValidAngle(lastRotation))
        {
            positionIsValid = false;
            return;
        }

        if (!TestForValidSurface(hit.collider))
        {
            positionIsValid = false;
            return;
        }

        if (!TestForValidLocation(lastPosition))
        {
            positionIsValid = false;
            return;
        }

        positionIsValid = true;
        lastValidPosition = lastPosition;
        lastValidRotation = lastRotation;
    }

    private bool TestForObstructions()
    {
        if (buildStart.BuildableItem == null)
            throw new InvalidOperationException("buildStart.BuildableItem is null");

        var collider = buildStart.BuildableItem.BoundingCollider;
        collider.enabled = true;

        // Backup ghost position
        var ghostPosition = GhostObject.transform.position;
        var ghostRotation = GhostObject.transform.rotation;

        // Move and rotate ghost to new positions
        GhostObject.transform.position = lastPosition;
        GhostObject.transform.rotation = lastRotation;

        try
        {
            bool hitObstruction = false;
            int numColliders = Physics.OverlapSphereNonAlloc(position: lastPosition, radius: collider.bounds.extents.magnitude, layerMask: buildStart.DetectionMask, results: intersections);

            for (int i = 0; i < numColliders; ++i)
            {
                var testCollider = intersections[i];

                if (testCollider == collider || testCollider.isTrigger)
                    continue;

                bool hasCollided = Physics.ComputePenetration(
                    collider,
                    collider.transform.position,
                    collider.transform.rotation,
                    testCollider,
                    testCollider.transform.position,
                    testCollider.transform.rotation,
                    out Vector3 _,
                    out float distance
                );

                if (hasCollided && distance > 0.001f)
                {
                    hitObstruction = true;
                    break;
                }
            }

            return hitObstruction;
        }
        finally
        {
            // Restore ghost position
            GhostObject.transform.position = ghostPosition;
            GhostObject.transform.rotation = ghostRotation;

            // Disable collider
            collider.enabled = false;
        }
    }

    private bool TestForValidAngle(Quaternion rotation)
    {
        float? optimalXZ = buildStart.OptimalRotationXZ;
        float lenience = buildStart.AllowedRotationLenience;

        if (optimalXZ == null)
            return true;

        bool validAngle = false;

        Vector3 eulerAngles = rotation.eulerAngles;

        if (eulerAngles.x >= 180f)
        {
            eulerAngles.x = Math.Abs(eulerAngles.x - 360f);
        }

        if (eulerAngles.y >= 180f)
        {
            eulerAngles.y = Math.Abs(eulerAngles.y - 360f);
        }

        if (eulerAngles.z >= 180f)
        {
            eulerAngles.z = Math.Abs(eulerAngles.z - 360f);
        }

        var diffX = Math.Abs(eulerAngles.x - optimalXZ.Value);
        var diffZ = Math.Abs(eulerAngles.z - optimalXZ.Value);

        if (diffX < lenience && diffZ < lenience)
        {
            validAngle = true;
        }

        return validAngle;
    }

    private bool TestForValidSurface(Collider surface)
    {
        // Check for interactables in gameobject or parents
        var interactable = surface.GetComponent<InteractableObject>() ?? surface.GetComponentInParent<InteractableObject>();

        if (interactable != null)
            return false;

        return true;
    }

    private bool TestForValidLocation(Vector3 position)
    {
        if (buildStart.RestrictToProperties)
        {
            var property = Property.OwnedProperties.FirstOrDefault(property =>
            {
                return property.DoBoundsContainPoint(position);
            });

            return property != null;
        }

        return true;
    }

    private void UpdateGhostTransform()
    {
        if (buildStart.GhostObject == null)
            return;

        Vector3 newGhostPosition;
        Quaternion newGhostRotation;

        if (sqrDistanceDiff > MaxSnapDistanceSqr)
        {
            newGhostPosition = lastPosition;
            newGhostRotation = lastRotation;
        }
        else
        {
            newGhostPosition = lastValidPosition;
            newGhostRotation = lastValidRotation;
        }

        buildStart.GhostObject.transform.position = newGhostPosition;
        buildStart.GhostObject.transform.rotation = newGhostRotation;
    }

    private void UpdateGhostMaterial()
    {
        if (buildStart.GhostObject == null)
            return;

        var buildManager = Singleton<BuildManager>.Instance;
        Material material;

        if (positionIsValid || sqrDistanceDiff <= MaxSnapDistanceSqr)
        {
            material = buildManager.ghostMaterial_White;
        }
        else
        {
            material = buildManager.ghostMaterial_Red;
        }

        if (currentGhostMaterial == material)
            return;

        currentGhostMaterial = material;
        buildManager.ApplyMaterial(GhostObject, material);
    }

    private void SpawnItemIfValid()
    {
        if (buildStart.ItemDefinition == null || buildStart.ItemInstance == null)
            return;

        if (!positionIsValid && sqrDistanceDiff > MaxSnapDistanceSqr)
            return;

        if (!GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick))
            return;

        Vector3 position = lastValidPosition;
        Quaternion rotation = lastValidRotation;

        // todo: spawn off grid item
        var item = Instantiate(buildStart.ItemDefinition.BuiltItem);
        item.transform.position = position;
        item.transform.rotation = rotation;
        InstanceFinder.ServerManager.Spawn(item.gameObject);

        //PlayerSingleton<PlayerInventory>.Instance.equippedSlot.ChangeQuantity(-1);
        Singleton<BuildManager>.Instance.PlayBuildSound(buildStart.ItemDefinition.BuildSoundType, position);
    }
}

public class BuildStopOffGrid : BuildStop_Base
{
    // nothing to override, but the class exists and is used with the other components in case something needs to be done in the future
}
