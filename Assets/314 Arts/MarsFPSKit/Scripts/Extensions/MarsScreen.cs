using UnityEngine;

namespace MarsFPSKit
{
    /// <summary>
    /// Keeps the cursor always active and visible, 
    /// no matter if you click inside the game.
    /// </summary>
    public class MarsScreen
    {
        public static bool lockCursor
        {
            get
            {
                // Cursor is always "active" → return true if it's visible
                return Cursor.visible;
            }
            set
            {
                if (value)
                {
                    Cursor.lockState = CursorLockMode.None; // ❌ No lock
                    Cursor.visible = true;                 // ✅ Always visible
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None; // ❌ Still no lock
                    Cursor.visible = true;                 // ✅ Keep it visible
                }
            }
        }
    }
}
