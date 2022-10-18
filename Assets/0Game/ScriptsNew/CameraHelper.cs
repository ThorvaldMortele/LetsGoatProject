using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CameraHelper
{
    public static void HideMousePointer(bool seemouse, CursorLockMode state)
    {
        Cursor.visible = !seemouse;
        Cursor.lockState = state;
    }
}
