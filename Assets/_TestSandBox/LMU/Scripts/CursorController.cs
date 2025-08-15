using UnityEngine;

public class CursorController : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private Texture2D _cursorTexture;

    [Header("설정")]
    [SerializeField] private bool _isActive = false;

    private void Awake()
    {
        if (_isActive == false)
            return;

        Cursor.SetCursor(_cursorTexture, Vector2.zero, CursorMode.Auto);
    }
}
