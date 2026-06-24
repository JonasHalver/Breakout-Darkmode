using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickLayoutGenerator : MonoBehaviour
{
    public static BrickLayoutGenerator Instance;

    [Header("References")]
    [SerializeField] List<string> _brickConfigKeys = new();

    [Header("Level")] 
    [SerializeField] List<string> _levelKeys = new();
    BrickLayoutConfigSO _layoutConfig;
    public static string LastLoadedLevel;
    
    [Header("Editor Preview")]
    [SerializeField] BrickLayoutConfigSO _testLevelConfig;
    [SerializeField] bool _forceLoadTestLevel;
    [SerializeField] bool _drawLayoutGizmos = true;
    [SerializeField] bool _drawEmptyCells = true;
    [SerializeField] float _gizmoDepth = 0.02f;
    
    readonly Dictionary<char, BrickConfigSO> _brickConfigs = new();
    readonly List<GameObject> _spawnedBricks = new();

    bool _queueRebuild;
    Coroutine _spawnRoutine;
    struct GridMetrics
    {
        public int Rows;
        public int Columns;

        public Vector2 CellSize;
        public Vector2 Spacing;
        public Vector2 FirstCellCenter;

        public Vector2 GetCellCenter(int row, int column)
        {
            return new Vector2(
                FirstCellCenter.x + column * (CellSize.x + Spacing.x),
                FirstCellCenter.y - row * (CellSize.y + Spacing.y)
            );
        }

        public Rect GetCellRect(int row, int column)
        {
            Vector2 center = GetCellCenter(row, column);

            return new Rect(
                center - CellSize * 0.5f,
                CellSize
            );
        }
    }
    void Awake()
    {
        Instance = this;
    }

    public static void Initialize(Action<bool> onInitialized)
    {
        if (Instance == null)
        {
            Debug.LogError("BrickLayoutGenerator has no active instance.");
            onInitialized?.Invoke(false);
            return;
        }

        
        if (Instance._forceLoadTestLevel)
        {
            Instance._layoutConfig = Instance._testLevelConfig;
        }
        else
        {
            var level = Instance.RandomLevel();
            if (!ConfigRepository.TryGetConfig(level, out Instance._layoutConfig))
            {
                Debug.LogError("Failed to initialize BrickLayoutGenerator. Layout config was missing.");
                onInitialized?.Invoke(false);
                return;
            }
        }

        if (!Instance.BuildBrickConfigLookup())
        {
            onInitialized?.Invoke(false);
            return;
        }

        onInitialized?.Invoke(Instance.TryRebuild());
    }

    void OnEnable()
    {
        DynamicPlayfieldBounds.BoundsChanged += OnPlayfieldBoundsChanged;
    }

    void OnDisable()
    {
        DynamicPlayfieldBounds.BoundsChanged -= OnPlayfieldBoundsChanged;
        StopSpawnRoutine();
    }

    void OnPlayfieldBoundsChanged(Rect _)
    {
        _queueRebuild = true;
    }

    void Update()
    {
        if (!_queueRebuild)
        {
            return;
        }

        TryRebuild();
        _queueRebuild = false;
    }

    string RandomLevel()
    {
        var levelKeys = new List<string>(_levelKeys);
        levelKeys.Remove(LastLoadedLevel);
        var level = levelKeys[UnityEngine.Random.Range(0, levelKeys.Count)];
        LastLoadedLevel = level;
        Debug.Log($"Loading level: {level}");
        return level;
    }
    bool BuildBrickConfigLookup()
    {
        _brickConfigs.Clear();

        if (_layoutConfig == null)
        {
            Debug.LogError("Cannot build brick config lookup without a layout config.");
            return false;
        }

        if (_layoutConfig.CsvLayout == null)
        {
            Debug.LogError("BrickLayoutConfigSO has no CSV assigned.");
            return false;
        }

        if (_brickConfigKeys == null ||
            _brickConfigKeys.Count == 0)
        {
            Debug.LogError("No brick config keys assigned.");
            return false;
        }

        foreach (string configKey in _brickConfigKeys)
        {
            if (string.IsNullOrWhiteSpace(configKey))
            {
                Debug.LogError("BrickLayoutConfigSO contains an empty brick config key.");
                return false;
            }

            if (!ConfigRepository.TryGetConfig(configKey, out BrickConfigSO brickConfig))
            {
                Debug.LogError($"Could not find BrickConfigSO '{configKey}'.");
                return false;
            }

            var typeCode = brickConfig.CsvTypeCode;

            if (typeCode == '\0')
            {
                Debug.LogError($"Brick config '{brickConfig.name}' has no valid CSV type code.");
                return false;
            }

            if (_brickConfigs.TryAdd(typeCode, brickConfig))
            {
                continue;
            }
            Debug.LogError($"More than one BrickConfigSO uses CSV type code '{typeCode}'.");
            return false;
        }

        return true;
    }
    
    bool TryCalculateGridMetrics(BrickCsvLayout csvLayout, Rect brickFieldRect, out GridMetrics metrics, BrickLayoutConfigSO config)
    {
        metrics = default;

        var columns = csvLayout.ColumnCount;
        var rows = csvLayout.RowCount;

        if (columns <= 0 || rows <= 0)
        {
            return false;
        }

        var spacing = config.Spacing;
        var availableWidth = brickFieldRect.width - spacing.x * (columns - 1);
        var availableHeight = brickFieldRect.height - spacing.y * (rows - 1);

        if (availableWidth <= 0f || availableHeight <= 0f)
        {
            return false;
        }

        var cellSize = new Vector2(availableWidth / columns, availableHeight / rows);

        metrics = new GridMetrics
        {
            Rows = rows,
            Columns = columns,
            CellSize = cellSize,
            Spacing = spacing,
            FirstCellCenter = new Vector2(
                brickFieldRect.xMin + cellSize.x * 0.5f,
                brickFieldRect.yMax - cellSize.y * 0.5f
            )
        };

        return true;
    }
    
    public bool TryRebuild()
    {
        if (_layoutConfig == null)
        {
            return false;
        }

        var playfieldRect = DynamicPlayfieldBounds.PlayfieldRect;

        if (playfieldRect.width <= 0f || playfieldRect.height <= 0f)
        {
            return false;
        }

        if (!BrickCsvParser.TryParse(_layoutConfig.CsvLayout, out BrickCsvLayout csvLayout, out string parseError))
        {
            Debug.LogError($"Could not parse brick CSV: {parseError}");
            return false;
        }

        var brickFieldRect = CalculateBrickFieldRect(playfieldRect, _layoutConfig);

        if (brickFieldRect.width <= 0f || brickFieldRect.height <= 0f)
        {
            Debug.LogWarning("Brick field rect is too small to generate bricks.");
            return false;
        }

        if (!TryBuildSpawnRequests(csvLayout, brickFieldRect, out List<BrickSpawnRequest> spawnRequests))
        {
            return false;
        }

        StopSpawnRoutine();
        ClearExistingBricks();

        _spawnRoutine = StartCoroutine(SpawnBricksAsync(spawnRequests));

        return true;
    }

    Rect CalculateBrickFieldRect(Rect playfieldRect, BrickLayoutConfigSO config)
    {
        float xMin = playfieldRect.xMin + playfieldRect.width * config.LeftPercent;
        float xMax = playfieldRect.xMax - playfieldRect.width * config.RightPercent;

        float yMax = playfieldRect.yMax - playfieldRect.height * config.TopPercent;
        float yMin = playfieldRect.yMin + playfieldRect.height * config.BottomPercent;

        xMin += config.FieldPadding.x;
        xMax -= config.FieldPadding.x;

        yMin += config.FieldPadding.y;
        yMax -= config.FieldPadding.y;

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    bool TryBuildSpawnRequests(BrickCsvLayout csvLayout, Rect brickFieldRect, out List<BrickSpawnRequest> spawnRequests)
    {
        spawnRequests = new List<BrickSpawnRequest>();

        var columns = csvLayout.ColumnCount;
        var rows = csvLayout.RowCount;

        if (columns <= 0 || rows <= 0)
        {
            Debug.LogError("CSV layout has no usable rows or columns.");
            return false;
        }

        var spacing = _layoutConfig.Spacing;
        var availableWidth = brickFieldRect.width - spacing.x * (columns - 1);
        var availableHeight = brickFieldRect.height - spacing.y * (rows - 1);

        if (availableWidth <= 0f || availableHeight <= 0f)
        {
            Debug.LogError(
                "Spacing is too large for this CSV layout and brick field."
            );
            return false;
        }

        if (!TryCalculateGridMetrics(csvLayout, brickFieldRect, out GridMetrics grid, _layoutConfig))
        {
            Debug.LogError("Spacing is too large for this CSV layout and brick field.");
            return false;
        }

        Vector3 brickSize = new Vector3(
            grid.CellSize.x,
            grid.CellSize.y,
            1
        );

        for (int row = 0; row < grid.Rows; row++)
        {
            for (int column = 0; column < grid.Columns; column++)
            {
                BrickCellData cell = csvLayout.GetCell(row, column);

                if (cell.IsEmpty)
                {
                    continue;
                }

                if (!_brickConfigs.TryGetValue(cell.TypeCode, out BrickConfigSO brickConfig))
                {
                    Debug.LogError(
                        $"CSV uses brick type '{cell.TypeCode}' at row {row + 1}, " +
                        $"column {column + 1}, but no matching config exists."
                    );

                    return false;
                }

                Vector2 cellCenter = grid.GetCellCenter(row, column);

                spawnRequests.Add(new BrickSpawnRequest
                {
                    Config = brickConfig,
                    Position = new Vector3(cellCenter.x, cellCenter.y, 0f),
                    Size = brickSize,
                    Rotation = GetRotation(cell.DirectionIndex),
                    CollisionNote = cell.CollisionNote
                });
            }
        }

        return true;
    }

    static Quaternion GetRotation(int directionIndex)
    {
        // 0 = North, then clockwise in 45 degree increments.
        // Assumes the brick prefab's local up direction is "north".
        return Quaternion.Euler(0f, 0f, -directionIndex * 45f);
    }

    IEnumerator SpawnBricksAsync(List<BrickSpawnRequest> spawnRequests)
    {
        foreach (BrickSpawnRequest request in spawnRequests)
        {
            Brick newBrick = BrickFactory.Create(request, transform);

            if (newBrick != null)
            {
                _spawnedBricks.Add(newBrick.gameObject);
            }

            yield return null;
        }

        _spawnRoutine = null;
    }

    void StopSpawnRoutine()
    {
        if (_spawnRoutine == null)
        {
            return;
        }

        StopCoroutine(_spawnRoutine);
        _spawnRoutine = null;
    }

    void ClearExistingBricks()
    {
        for (int i = _spawnedBricks.Count - 1; i >= 0; i--)
        {
            GameObject brick = _spawnedBricks[i];

            if (brick == null)
            {
                continue;
            }

            // Removes colliders immediately, before Destroy completes.
            brick.SetActive(false);

            if (Application.isPlaying)
            {
                Destroy(brick);
            }
            else
            {
                DestroyImmediate(brick);
            }
        }

        _spawnedBricks.Clear();
    }

    void OnDrawGizmos()
    {
        if (!_drawLayoutGizmos || _testLevelConfig == null)
        {
            return;
        }

        var playfieldRect = DynamicPlayfieldBounds.PlayfieldRect;

        if (playfieldRect.width <= 0f || playfieldRect.height <= 0f)
        {
            Debug.LogWarning("Playfield rect is too small to draw layout gizmos.");
            return;
        }

        var brickFieldRect = CalculateBrickFieldRect(playfieldRect, _testLevelConfig);

        DrawPreviewRect(playfieldRect, new Color(1f, 0.8f, 0.15f), 0f);

        DrawPreviewRect(brickFieldRect, new Color(0.15f, 0.85f, 1f), 0.025f);

        if (_testLevelConfig.CsvLayout == null)
        {
            return;
        }

        if (!BrickCsvParser.TryParse(_testLevelConfig.CsvLayout, out BrickCsvLayout csvLayout, out _)
            || !TryCalculateGridMetrics(csvLayout, brickFieldRect, out GridMetrics grid, _testLevelConfig))
        {
            DrawPreviewRect(brickFieldRect, Color.red, 0.08f);
            return;
        }

        for (var row = 0; row < grid.Rows; row++)
        {
            for (var column = 0; column < grid.Columns; column++)
            {
                var cell = csvLayout.GetCell(row, column);
                var cellRect = grid.GetCellRect(row, column);

                if (cell.IsEmpty)
                {
                    if (_drawEmptyCells)
                    {
                        DrawPreviewRect(cellRect, new Color(0.45f, 0.45f, 0.45f), 0.015f);
                    }

                    continue;
                }

                DrawPreviewRect(cellRect, new Color(0.2f, 1f, 0.55f), 0.08f);
            }
        }
    }
    void DrawPreviewRect(Rect rect, Color outlineColor, float fillAlpha)
    {
        var depth = Mathf.Max(0.001f, _gizmoDepth);

        var center = new Vector3(rect.center.x, rect.center.y, 0f);

        var size = new Vector3(rect.width, rect.height, depth);

        var previousColor = Gizmos.color;

        if (fillAlpha > 0f)
        {
            var fillColor = outlineColor;
            fillColor.a = fillAlpha;

            Gizmos.color = fillColor;
            Gizmos.DrawCube(center, size);
        }

        outlineColor.a = 1f;
        Gizmos.color = outlineColor;
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = previousColor;
    }
}