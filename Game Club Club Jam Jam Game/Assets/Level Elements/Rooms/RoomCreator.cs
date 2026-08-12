using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomCreator : MonoBehaviour
{
    public enum Side { North, South, East, West }

    [System.Serializable]
    public class Opening
    {
        public Side side;
        [Range(-1f, 1f)]
        [Tooltip("Position of the opening's center along the wall, as a percentage of the wall's interior span. -1 = wall start corner, 0 = wall center, 1 = wall end corner.")]
        public float centerOffset;
        [Tooltip("Width of the opening (gap size) in world units, measured along the wall.")]
        public float size = 2f;
        public GameObject doorPrefab;
    }

    [Header("Room Bounds")]
    [Tooltip("Interior min corner (bottom-left) in local space, relative to this transform.")]
    [SerializeField] private Vector2 minBounds = new Vector2(-5f, -5f);
    [Tooltip("Interior max corner (top-right) in local space, relative to this transform.")]
    [SerializeField] private Vector2 maxBounds = new Vector2(5f, 5f);

    [Header("Walls")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private float wallThickness = 1f;
    [Tooltip("Parent all generated walls under a child object to keep the hierarchy tidy.")]
    [SerializeField] private bool groupWallsUnderChild = true;

    [Header("Openings / Doors")]
    [SerializeField] private List<Opening> openings = new List<Opening>();
    [SerializeField] private float doorThickness = 1f;

    [Header("Camera Bounds")]
    [SerializeField] private bool createCameraBounds = true;

    private const float MinSegmentLength = 0.01f; // guards against degenerate zero-length wall segments

    [ContextMenu("Build Room")]
    public void BuildRoom()
    {
        ClearRoom();

        Transform wallParent = transform;
        if (groupWallsUnderChild)
        {
            GameObject wallsGO = new GameObject("Walls");
            wallsGO.transform.SetParent(transform, false);
            wallParent = wallsGO.transform;
        }

        BuildWallSide(Side.North, wallParent);
        BuildWallSide(Side.South, wallParent);
        BuildWallSide(Side.East, wallParent);
        BuildWallSide(Side.West, wallParent);

        if (createCameraBounds)
        {
            BuildCameraBounds();
        }
    }

    [ContextMenu("Clear Room")]
    public void ClearRoom()
    {
        // Destroy any previously generated children (walls group + camera bounds)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name != "Walls" && child.name != "CameraBounds")
                continue;
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

        private void BuildWallSide(Side side, Transform parent)
    {
        if (wallPrefab == null)
        {
            Debug.LogWarning($"[{name}: RoomCreator] No wallPrefab assigned; skipping wall generation.");
            return;
        }
 
        // wallLength = span of the wall along its own axis, in wall-local coords (thickness-extended, starts at 0)
        float wallLength = GetWallLength(side);
        float interiorSpan = GetInteriorSpan(side); // the un-extended interior span, e.g. maxBounds.x - minBounds.x
 
        List<Opening> sideOpenings = openings.Where(o => o.side == side).ToList();
 
        // Gather openings for this side. centerOffset is authored as -1..1 normalized along the wall,
        // so first convert to world-unit distance from the interior start corner, then shift by
        // wallThickness to land in wall-local coords (which start at the thickness-extended corner).
        // The doorPrefab is carried alongside (start, end) through this whole pipeline so each gap
        // stays paired with the SAME Opening it came from — never re-associated by list index later,
        // which breaks the moment gaps merge or openings from other sides sit earlier in the list.
        List<(float start, float end, GameObject doorPrefab)> gaps = sideOpenings
            .Select(o => (worldOffset: GetWorldCenterOffset(o), o.size, o.doorPrefab))
            .Select(o => (start: o.worldOffset - o.size * 0.5f, end: o.worldOffset + o.size * 0.5f, o.doorPrefab))
            // Clamp the INTERVAL as a whole against the interior span first, preserving size where possible
            // by sliding the whole gap back on-wall rather than chopping one side independently.
            .Select(g => (interval: ClampIntervalToRange(g.start, g.end, 0f, interiorSpan), g.doorPrefab))
            // Shift into wall-local (thickness-extended) coordinates for segment placement
            .Select(g => (start: g.interval.start + wallThickness, end: g.interval.end + wallThickness, g.doorPrefab))
            .Where(g => g.end - g.start > MinSegmentLength)
            .OrderBy(g => g.start)
            .ToList();
 
        // Doors are spawned per ORIGINAL opening (each keeps its own prefab), independent of wall-segment
        // merging below — merging only decides where solid wall goes, not which door prefab(s) appear.
        foreach (var gap in gaps)
        {
            if (gap.doorPrefab != null)
            {
                SpawnDoor(side, gap.start, gap.end, parent, gap.doorPrefab);
            }
        }
 
        // Merge overlapping gaps so overlapping doors don't create zero/negative-length wall segments between them
        List<(float start, float end)> mergedGaps = MergeIntervals(gaps.Select(g => (g.start, g.end)).ToList());
 
        // Walk along the wall, emitting a solid segment for every stretch NOT covered by a gap
        float cursor = 0f;
        foreach (var (start, end) in mergedGaps)
        {
            if (start - cursor > MinSegmentLength)
            {
                SpawnWallSegment(side, cursor, start, parent);
            }
            cursor = end;
        }
 
        if (wallLength - cursor > MinSegmentLength)
        {
            SpawnWallSegment(side, cursor, wallLength, parent);
        }
    }
 
    private List<(float start, float end)> MergeIntervals(List<(float start, float end)> sorted)
    {
        var merged = new List<(float start, float end)>();
        foreach (var interval in sorted)
        {
            if (merged.Count > 0 && interval.start <= merged[^1].end)
            {
                merged[^1] = (merged[^1].start, Mathf.Max(merged[^1].end, interval.end));
            }
            else
            {
                merged.Add(interval);
            }
        }
        return merged;
    }

    private float GetWallLength(Side side)
    {
        return GetInteriorSpan(side) + wallThickness * 2f;
    }

    /// <summary>The interior (un-extended) span of the room along this wall's axis; matches what centerOffset is authored against.</summary>
    private float GetInteriorSpan(Side side)
    {
        Vector2 size = maxBounds - minBounds;
        return (side == Side.North || side == Side.South) ? size.x : size.y;
    }

    /// <summary>Converts a -1..1 normalized centerOffset into world-unit distance from the wall's interior start corner.</summary>
    private float GetWorldCenterOffset(Opening opening)
    {
        float interiorSpan = GetInteriorSpan(opening.side);
        // -1 -> 0 (start corner), 0 -> half span (center), 1 -> full span (end corner)
        return (opening.centerOffset + 1f) * 0.5f * interiorSpan;
    }

    /// <summary>
    /// Clamps the interval [start, end] to fit within [rangeMin, rangeMax], preserving its length where
    /// possible by sliding the whole interval rather than truncating one side independently (which would
    /// silently shrink/distort the requested opening size instead of just repositioning it on-wall).
    /// </summary>
    private (float start, float end) ClampIntervalToRange(float start, float end, float rangeMin, float rangeMax)
    {
        float length = end - start;
        if (length >= rangeMax - rangeMin)
        {
            // Opening is wider than the wall itself; fill the whole wall
            return (rangeMin, rangeMax);
        }
        if (start < rangeMin)
        {
            return (rangeMin, rangeMin + length);
        }
        if (end > rangeMax)
        {
            return (rangeMax - length, rangeMax);
        }
        return (start, end);
    }

    /// <summary>
    /// Spawns one wall segment covering [offsetStart, offsetEnd] along the given side,
    /// where offsets are measured from that side's start corner including the thickness extension.
    /// </summary>
    private void SpawnWallSegment(Side side, float offsetStart, float offsetEnd, Transform parent)
    {
        float length = offsetEnd - offsetStart;
        float centerOffset = (offsetStart + offsetEnd) * 0.5f;

        Vector2 localPosition;
        Vector2 scale;

        switch (side)
        {
            case Side.North:
                // Runs along X at y = maxBounds.y, extended by thickness on both ends
                localPosition = new Vector2(minBounds.x - wallThickness + centerOffset, maxBounds.y + wallThickness * 0.5f);
                scale = new Vector2(length, wallThickness);
                break;
            case Side.South:
                localPosition = new Vector2(minBounds.x - wallThickness + centerOffset, minBounds.y - wallThickness * 0.5f);
                scale = new Vector2(length, wallThickness);
                break;
            case Side.East:
                // Runs along Y at x = maxBounds.x, extended by thickness on both ends
                localPosition = new Vector2(maxBounds.x + wallThickness * 0.5f, minBounds.y - wallThickness + centerOffset);
                scale = new Vector2(wallThickness, length);
                break;
            case Side.West:
                localPosition = new Vector2(minBounds.x - wallThickness * 0.5f, minBounds.y - wallThickness + centerOffset);
                scale = new Vector2(wallThickness, length);
                break;
            default:
                return;
        }

        GameObject wall = Instantiate(wallPrefab, parent);
        wall.name = $"Wall_{side}_{offsetStart:F1}-{offsetEnd:F1}";
        wall.transform.SetLocalPositionAndRotation(localPosition, Quaternion.identity);

        // Assumes wallPrefab is a 1x1 unit quad/sprite at its base scale; stretch it to fill the segment.
        wall.transform.localScale = new Vector3(scale.x, scale.y, 1f);
    }

    /// <summary>
    /// Spawns one door covering [offsetStart, offsetEnd] along the given side,
    /// where offsets are measured from that side's start corner including the thickness extension.
    /// </summary>
    private void SpawnDoor(Side side, float offsetStart, float offsetEnd, Transform parent, GameObject doorPrefab)
    {
        float length = offsetEnd - offsetStart;
        float centerOffset = (offsetStart + offsetEnd) * 0.5f;

        Vector2 localPosition;
        Vector2 scale;
        Quaternion localRotation = Quaternion.identity;

        switch (side)
        {
            case Side.North:
                // Runs along X at y = maxBounds.y, extended by thickness on both ends
                localPosition = new Vector2(minBounds.x - wallThickness + centerOffset, maxBounds.y + wallThickness * 0.5f);
                scale = new Vector2(doorThickness, length);
                localRotation = Quaternion.LookRotation(Vector3.forward, transform.right); // Assuming door opens along its local y axis
                break;
            case Side.South:
                localPosition = new Vector2(minBounds.x - wallThickness + centerOffset, minBounds.y - wallThickness * 0.5f);
                scale = new Vector2(doorThickness, length);
                localRotation = Quaternion.LookRotation(Vector3.forward, transform.right); // Assuming door opens along its local y axis
                break;
            case Side.East:
                // Runs along Y at x = maxBounds.x, extended by thickness on both ends
                localPosition = new Vector2(maxBounds.x + wallThickness * 0.5f, minBounds.y - wallThickness + centerOffset);
                scale = new Vector2(doorThickness, length);
                break;
            case Side.West:
                localPosition = new Vector2(minBounds.x - wallThickness * 0.5f, minBounds.y - wallThickness + centerOffset);
                scale = new Vector2(doorThickness, length);
                break;
            default:
                return;
        }

        GameObject door = Instantiate(doorPrefab, parent);
        door.name = $"Door_{side}_{offsetStart:F1}-{offsetEnd:F1}";
        door.transform.SetLocalPositionAndRotation(localPosition, localRotation);

        // Assumes doorPrefab is a 1x1 unit quad/sprite at its base scale; stretch it to fill the segment.
        door.transform.localScale = new Vector3(scale.x, scale.y, 1f);
    }

    private void BuildCameraBounds()
    {
        GameObject boundsGO = new GameObject("CameraBounds");
        boundsGO.transform.SetParent(transform, false);

        Vector2 size = maxBounds - minBounds;
        // Extend size by wallThickness to capture half the thickness of the walls
        // Not doing *2 since player can glitch into the wall when running into it and trigger the camera bounds
        size.x += wallThickness;
        size.y += wallThickness;
        Vector2 center = (minBounds + maxBounds) * 0.5f;
        boundsGO.transform.localPosition = center;

        // CameraBounds requires a Collider2D and (by default) reads min/max FROM that collider's
        // bounds via boundsMatchCollider; so we size a trigger BoxCollider2D to the room interior
        // and let CameraBounds compute min/max itself, exactly as it's designed to be used.
        BoxCollider2D trigger = boundsGO.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = size;

        CameraBounds cameraBounds = boundsGO.AddComponent<CameraBounds>();
        // triggerTag and boundsMatchCollider are private [SerializeField]s on CameraBounds with no
        // public setters, so we set them via SerializedObject in editor, or fall back to defaults
        // (boundsMatchCollider defaults to true, which is what we want) at runtime.
#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(cameraBounds);
        so.FindProperty("boundsMatchCollider").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 worldMin = transform.TransformPoint(minBounds);
        Vector2 worldMax = transform.TransformPoint(maxBounds);
        Vector2 center = (worldMin + worldMax) * 0.5f;
        Vector2 size = worldMax - worldMin;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = Color.cyan;
        foreach (var opening in openings)
        {
            DrawOpeningGizmo(opening);
        }
    }

    private void DrawOpeningGizmo(Opening opening)
    {
        float interiorSpan = GetInteriorSpan(opening.side);
        float worldOffset = GetWorldCenterOffset(opening);
        var (start, end) = ClampIntervalToRange(
            worldOffset - opening.size * 0.5f,
            worldOffset + opening.size * 0.5f,
            0f, interiorSpan);
        float centerOffset = (start + end) * 0.5f; // interior-relative, matches authoring space
        float length = end - start;
        if (length <= 0f)
            return;

        Vector2 localCenter;
        Vector2 size;
        switch (opening.side)
        {
            case Side.North:
                localCenter = new Vector2(minBounds.x + centerOffset, maxBounds.y + wallThickness * 0.5f);
                size = new Vector2(length, wallThickness);
                break;
            case Side.South:
                localCenter = new Vector2(minBounds.x + centerOffset, minBounds.y - wallThickness * 0.5f);
                size = new Vector2(length, wallThickness);
                break;
            case Side.East:
                localCenter = new Vector2(maxBounds.x + wallThickness * 0.5f, minBounds.y + centerOffset);
                size = new Vector2(wallThickness, length);
                break;
            case Side.West:
                localCenter = new Vector2(minBounds.x - wallThickness * 0.5f, minBounds.y + centerOffset);
                size = new Vector2(wallThickness, length);
                break;
            default:
                return;
        }

        Vector3 worldCenter = transform.TransformPoint(localCenter);
        Gizmos.DrawWireCube(worldCenter, size);
    }
}