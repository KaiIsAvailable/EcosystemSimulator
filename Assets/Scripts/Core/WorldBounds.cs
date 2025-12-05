using UnityEngine;

/// <summary>
/// Global world boundaries to keep entities on land (not in ocean)
/// Set by WorldLogic, used by animals to avoid walking into water
/// </summary>
public static class WorldBounds
{
    public static Vector3 areaCenter;
    public static Vector2 areaHalfExtents;
    public static float oceanTopY;  // Y-coordinate where ocean ends (don't go below this)
    
    public static bool IsInitialized { get; private set; } = false;
    
    /// <summary>
    /// Initialize world boundaries (called by WorldLogic on startup)
    /// </summary>
    public static void Initialize(Vector3 center, Vector2 halfExtents, float oceanTop)
    {
        areaCenter = center;
        areaHalfExtents = halfExtents;
        oceanTopY = oceanTop;
        IsInitialized = true;
        
        Debug.Log($"[WorldBounds] Initialized: center={center}, extents={halfExtents}, oceanTopY={oceanTopY:F2}");
    }
    
    /// <summary>
    /// Clamp a position to stay on land (above ocean, within map bounds)
    /// </summary>
    public static Vector3 ClampToLand(Vector3 position)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[WorldBounds] Not initialized! Position unclamped.");
            return position;
        }
        
        float minX = areaCenter.x - areaHalfExtents.x;
        float maxX = areaCenter.x + areaHalfExtents.x;
        float minY = oceanTopY + 0.2f;  // 0.2f buffer above water
        float maxY = areaCenter.y + areaHalfExtents.y;
        
        return new Vector3(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY),
            position.z
        );
    }
    
    /// <summary>
    /// Check if a position is valid (on land, not in ocean)
    /// </summary>
    public static bool IsOnLand(Vector3 position)
    {
        if (!IsInitialized) return true;  // Allow if not initialized yet
        
        // Check if above ocean
        if (position.y < oceanTopY + 0.2f) return false;
        
        // Check if within map bounds
        float minX = areaCenter.x - areaHalfExtents.x;
        float maxX = areaCenter.x + areaHalfExtents.x;
        float maxY = areaCenter.y + areaHalfExtents.y;
        
        return position.x >= minX && position.x <= maxX && position.y <= maxY;
    }
}
