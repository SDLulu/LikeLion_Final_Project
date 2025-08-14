using UnityEngine;

public class CursorController : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private Texture2D _cursorTexture;

    private void Awake()
    {
        Cursor.SetCursor(_cursorTexture, Vector2.zero, CursorMode.Auto);
    }
}
