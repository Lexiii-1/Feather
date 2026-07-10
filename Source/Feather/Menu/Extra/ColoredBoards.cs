using GorillaNetworking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Feather.Menu.Extra;

public class ColoredBoards : MonoBehaviour
{
    private const int StumpIndex = 3;
    private const int ForestIndex = 6;
    public static ColoredBoards? Instance;

    private static Material _boardMaterial;

    private static readonly Dictionary<string, MapBoardConfig> MapRegister = new()
    {
        ["Canyon2"] = new MapBoardConfig(
                    "Canyon/CanyonScoreboardAnchor/GorillaScoreBoard",
                    new Vector3(-24.5019f, -28.7746f, 0.1f),
                    new Vector3(270f, 0f, 0f),
                    new Vector3(21.5946f, 1f, 22.1782f)
            ),
        ["Skyjungle"] = new MapBoardConfig(
                    "skyjungle/UI/Scoreboard/GorillaScoreBoard",
                    new Vector3(-21.2764f, -32.1928f, 0f),
                    new Vector3(270.2987f, 0.2f, 359.9f),
                    new Vector3(21.6f, 0.1f, 20.4909f)
            ),
        ["Mountain"] = new MapBoardConfig(
                    "Mountain/MountainScoreboardAnchor/GorillaScoreBoard",
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.one
            ),
        ["Metropolis"] = new MapBoardConfig(
                    "MetroMain/ComputerArea/Scoreboard/GorillaScoreBoard",
                    new Vector3(-25.1f, -31f, 0.1502f),
                    new Vector3(270.1958f, 0.2086f, 0f),
                    new Vector3(21f, 102.9727f, 21.4f)
            ),
        ["Bayou"] = new MapBoardConfig(
                    "BayouMain/ComputerArea/GorillaScoreBoardPhysical",
                    new Vector3(-28.3419f, -26.851f, 0.3f),
                    new Vector3(270f, 0f, 0f),
                    new Vector3(21.3636f, 38f, 21f)
            ),
        ["Beach"] = new MapBoardConfig(
                    "BeachScoreboardAnchor/GorillaScoreBoard",
                    new Vector3(-22.1964f, -33.7126f, 0.1f),
                    new Vector3(270.056f, 0f, 0f),
                    new Vector3(21.2f, 2f, 21.6f)
            ),
        ["Cave"] = new MapBoardConfig(
                    "Cave_Main_Prefab/CrystalCaveScoreboardAnchor/GorillaScoreBoard",
                    new Vector3(-22.1964f, -33.7126f, 0.1f),
                    new Vector3(270.056f, 0f, 0f),
                    new Vector3(21.2f, 2f, 21.6f)
            ),
        ["Rotating"] = new MapBoardConfig(
                    "RotatingPermanentEntrance/UI (1)/RotatingScoreboard/RotatingScoreboardAnchor/GorillaScoreBoard",
                    new Vector3(-22.1964f, -33.7126f, 0.1f),
                    new Vector3(270.056f, 0f, 0f),
                    new Vector3(21.2f, 2f, 21.6f)
            ),
        ["MonkeBlocks"] = new MapBoardConfig(
                    "Environment Objects/MonkeBlocksRoomPersistent/AtticScoreBoard/AtticScoreboardAnchor/GorillaScoreBoard",
                    new Vector3(-22.1964f, -24.5091f, 0.57f),
                    new Vector3(270.1856f, 0.1f, 0f),
                    new Vector3(21.6f, 1.2f, 20.8f)
            ),
        ["Basement"] = new MapBoardConfig(
                    "Basement/BasementScoreboardAnchor/GorillaScoreBoard",
                    new Vector3(-22.1964f, -24.5091f, 0.57f),
                    new Vector3(270.1856f, 0.1f, 0f),
                    new Vector3(21.6f, 1.2f, 20.8f)
            ),
        ["City"] = new MapBoardConfig(
                    "City_Pretty/CosmeticsScoreboardAnchor/GorillaScoreBoard",
                    new Vector3(-22.1964f, -34.9f, 0.57f),
                    new Vector3(270f, 0f, 0f),
                    new Vector3(21.6f, 2.4f, 22f)
            ),
    };

    private readonly Dictionary<string, GameObject> activeCustomBoards = new();
    private readonly List<Renderer> networkTriggerScreens = [];
    private readonly Dictionary<string, GameObject> sceneHierarchyCache = new(StringComparer.OrdinalIgnoreCase);
    private GameObject? computerMonitor;

    private bool triggerScanComplete;

    public static Material BoardMaterial
    {
        get
        {
            if (_boardMaterial == null)
            {
                _boardMaterial = new Material(Shader.Find("GorillaTag/UberShader"));
                _boardMaterial.color = ThemeManager.GetTheme();
            }
            return _boardMaterial;
        }
        set => _boardMaterial = value;
    }

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnHierarchyChanged;
    }

    public void Start()
    {
        BuildInitialHierarchyCache();
    }

    public void Update()
    {
        BoardMaterial.color = ThemeManager.GetTheme();
        UpdateBaseMapLeaderboards();
        UpdateCustomSceneBoards();
        ProcessNetworkTriggers();
        UpdateComputerMonitorMaterial();
    }

    public void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnHierarchyChanged;
        Instance = null;
    }

    public void ReloadBoards()
    {
        triggerScanComplete = false;
        networkTriggerScreens.Clear();
    }

    private void UpdateBaseMapLeaderboards()
    {
        GameObject? treeRoom = FetchCachedElement("Environment Objects/LocalObjects_Prefab/TreeRoom");
        if (treeRoom != null && treeRoom.activeInHierarchy)
        {
            GameObject? stumpBoard = GetLeaderboardMesh(treeRoom.transform, StumpIndex);
            if (stumpBoard != null)
            {
                Renderer rend = stumpBoard.GetComponent<Renderer>();
                if (rend != null && rend.sharedMaterial != BoardMaterial)
                {
                    rend.sharedMaterial = BoardMaterial;
                }
            }
        }

        GameObject? forestRoom = FetchCachedElement("Environment Objects/LocalObjects_Prefab/Forest");

        if (forestRoom == null || !forestRoom.activeInHierarchy)
            return;

        {
            GameObject forestBoard = GetLeaderboardMesh(forestRoom.transform, ForestIndex);

            if (forestBoard == null)
                return;

            Renderer rend = forestBoard.GetComponent<Renderer>();

            if (rend != null && rend.sharedMaterial != BoardMaterial)
            {
                rend.sharedMaterial = BoardMaterial;
            }
        }
    }

    private void UpdateCustomSceneBoards()
    {
        foreach (KeyValuePair<string, MapBoardConfig> entry in MapRegister)
        {
            GameObject? anchor = FetchCachedElement(entry.Value.HierarchyPath);

            if (anchor == null || !anchor.activeInHierarchy)
                continue;

            if (!activeCustomBoards.ContainsKey(entry.Key))
                GenerateCustomSurface(entry.Key, entry.Value);
        }
    }

    private void ProcessNetworkTriggers()
    {
        if (!triggerScanComplete)
        {
            networkTriggerScreens.Clear();
            GorillaNetworkJoinTrigger[] triggers = Resources.FindObjectsOfTypeAll<GorillaNetworkJoinTrigger>();

            if (triggers != null)
                foreach (GorillaNetworkJoinTrigger trigger in triggers)
                {
                    Renderer[] childrenRenderers = trigger.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer rend in childrenRenderers)
                        if (rend != null &&
                            rend.gameObject.name.IndexOf("screen", StringComparison.OrdinalIgnoreCase) >= 0)
                            networkTriggerScreens.Add(rend);
                }

            triggerScanComplete = true;
        }

        foreach (Renderer screen in networkTriggerScreens.Where(screen => screen != null && screen.gameObject.activeInHierarchy && screen.sharedMaterial != BoardMaterial))
            screen.sharedMaterial = BoardMaterial;
    }

    private void UpdateComputerMonitorMaterial()
    {
        if (computerMonitor == null)
            computerMonitor =
                    FetchCachedElement(
                            "Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/GorillaComputerObject/ComputerUI/monitor/monitorScreen");

        if (computerMonitor == null || !computerMonitor.activeInHierarchy)
            return;

        Renderer monitorRenderer = computerMonitor.GetComponent<Renderer>();
        if (monitorRenderer != null && monitorRenderer.sharedMaterial != BoardMaterial)
            monitorRenderer.sharedMaterial = BoardMaterial;
    }

    private void OnHierarchyChanged(Scene scene, LoadSceneMode mode)
    {
        ParseSceneTree(scene);
        ReloadBoards();
    }

    public void GenerateCustomSurface(string sceneId, MapBoardConfig details)
    {
        try
        {
            if (activeCustomBoards.TryGetValue(sceneId, out GameObject oldBoard))
            {
                if (oldBoard != null) Destroy(oldBoard);
                activeCustomBoards.Remove(sceneId);
            }

            GameObject? hostObject = FetchCachedElement(details.HierarchyPath);

            if (hostObject == null) return;

            GameObject planeSurface = GameObject.CreatePrimitive(PrimitiveType.Plane);
            planeSurface.transform.parent = hostObject.transform;

            planeSurface.transform.localPosition = details.OffsetPosition ?? new Vector3(-22.1964f, -34.9f, 0.57f);
            planeSurface.transform.localRotation =
                    Quaternion.Euler(details.OffsetRotation ?? new Vector3(270f, 0f, 0f));

            planeSurface.transform.localScale = details.OffsetScale ?? new Vector3(21.6f, 2.4f, 22f);

            Collider surfaceCollider = planeSurface.GetComponent<Collider>();
            if (surfaceCollider != null) Destroy(surfaceCollider);

            Renderer surfaceRenderer = planeSurface.GetComponent<Renderer>();
            if (surfaceRenderer != null) surfaceRenderer.sharedMaterial = BoardMaterial;

            activeCustomBoards.Add(sceneId, planeSurface);
        }
        catch (Exception)
        {
        }
    }

    private void BuildInitialHierarchyCache()
    {
        sceneHierarchyCache.Clear();
        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.hideFlags != HideFlags.None && obj.hideFlags != HideFlags.DontSaveInBuild) continue;
            string keyPath = GenerateKeySignature(obj);
            if (!string.IsNullOrEmpty(keyPath)) sceneHierarchyCache[keyPath] = obj;
        }
    }

    private void ParseSceneTree(Scene targetScene)
    {
        if (!targetScene.isLoaded) return;

        GameObject[] roots = targetScene.GetRootGameObjects();
        foreach (GameObject root in roots)
            RunCacheIteration(root);
    }

    private void RunCacheIteration(GameObject nodes)
    {
        if (nodes == null) return;

        string verifiedPath = GenerateKeySignature(nodes);
        if (!string.IsNullOrEmpty(verifiedPath))
            sceneHierarchyCache[verifiedPath] = nodes;

        Transform structuralTransform = nodes.transform;
        int childrenCount = structuralTransform.childCount;
        for (int index = 0; index < childrenCount; index++)
            RunCacheIteration(structuralTransform.GetChild(index).gameObject);
    }

    private string GenerateKeySignature(GameObject target)
    {
        string buildingString = target.name;
        Transform stepPointer = target.transform;
        while (stepPointer.parent != null)
        {
            stepPointer = stepPointer.parent;
            buildingString = stepPointer.gameObject.name + "/" + buildingString;
        }

        return buildingString;
    }

    private GameObject? FetchCachedElement(string computedKey) =>
            sceneHierarchyCache.TryGetValue(computedKey, out GameObject discoveredObject) && discoveredObject != null
                    ? discoveredObject
                    : null;

    private GameObject? GetLeaderboardMesh(Transform parentRoom, int placementIndex)
    {
        int loopCounter = 0;
        int totalChildren = parentRoom.childCount;

        for (int i = 0; i < totalChildren; i++)
        {
            GameObject currentChild = parentRoom.GetChild(i).gameObject;

            if (currentChild.name.IndexOf("UnityTempFile", StringComparison.Ordinal) < 0)
                continue;

            if (loopCounter == placementIndex)
                return currentChild;

            loopCounter++;
        }

        return null;
    }

    public class MapBoardConfig
    {
        public MapBoardConfig(string path, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            HierarchyPath = path;
            OffsetPosition = pos;
            OffsetRotation = rot;
            OffsetScale = scale;
        }

        public MapBoardConfig(string path)
        {
            HierarchyPath = path;
            OffsetPosition = null;
            OffsetRotation = null;
            OffsetScale = null;
        }

        public string HierarchyPath { get; }
        public Vector3? OffsetPosition { get; }
        public Vector3? OffsetRotation { get; }
        public Vector3? OffsetScale { get; }
    }
}