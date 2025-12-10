using UnityEngine;

/// <summary>
/// Global world boundaries to keep entities on land (not in ocean)
/// Set by WorldLogic, used by animals to avoid walking into water
/// </summary>
public static class WorldBounds
{
    public static Vector3 areaCenter;
    public static Vector2 areaHalfExtents;
    public static float oceanTopY;  // Y-coordinate where ocean ends (don't go below this)
    
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
        float minY = oceanTopY + 0.2f;  // 0.2f buffer above water
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
        if (!IsInitialized) return true;  // Allow if not initialized yet
        
        // Check if above ocean
        if (position.y < oceanTopY + 0.2f) return false;
        
        // Check if within map bounds
        float minX = areaCenter.x - areaHalfExtents.x;
        float maxX = areaCenter.x + areaHalfExtents.x;
        float maxY = areaCenter.y + areaHalfExtents.y;
        
        return position.x >= minX && position.x <= maxX && position.y <= maxY;
    }
    
    /// <summary>
    /// CRITICAL FIX: Returns a random valid position on land.
    /// This function was missing and caused the error in HumanAgent/ControlPanelUI.
    /// </summary>
    public static Vector3 GetRandomLandPosition()
    {
        if (!IsInitialized)
        {
            Debug.LogError("[WorldBounds] Attempted to get random position before initialization!");
            return Vector3.zero;
        }

        // Try a few times to find a position above the ocean floor
        for (int i = 0; i < 10; i++)
        {
            float x = Random.Range(areaCenter.x - areaHalfExtents.x, areaCenter.x + areaHalfExtents.x);
            
            // Ensure the spawn point is above the ocean's surface plus a buffer (0.2f)
            float minY = oceanTopY + 0.2f; 
            float maxY = areaCenter.y + areaHalfExtents.y;
            float y = Random.Range(minY, maxY);
            
            Vector3 candidate = new Vector3(x, y, 0f);
            
            // Use the existing helper to check if it's a valid land position
            if (IsOnLand(candidate))
            {
                return candidate;
            }
        }
        
        // Fallback: Return the center of the land area if ten attempts fail
        return ClampToLand(areaCenter); 
    }
}