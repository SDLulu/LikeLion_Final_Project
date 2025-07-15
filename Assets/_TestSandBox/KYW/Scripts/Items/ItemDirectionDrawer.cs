#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// 🧭 ItemDirection을 위한 커스텀 프로퍼티 드로어 (룰타일 스타일)
[CustomPropertyDrawer(typeof(ItemDirection))]
public class ItemDirectionDrawer : PropertyDrawer
{
    private const float BUTTON_SIZE = 25f;
    private const float CENTER_SIZE = 60f;
    private const float TOTAL_HEIGHT = 80f;
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return TOTAL_HEIGHT;
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // 라벨 표시
        Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(labelRect, label);
        
        // 중앙 위치 계산
        float centerX = position.x + position.width / 2;
        float centerY = position.y + EditorGUIUtility.singleLineHeight + CENTER_SIZE / 2;
        
        // 현재 선택된 방향
        ItemDirection currentDirection = (ItemDirection)property.enumValueIndex;
        
        // 8방향 버튼 그리기
        for (int i = 0; i < 8; i++)
        {
            ItemDirection direction = (ItemDirection)i;
            Vector2 buttonPos = GetButtonPosition(centerX, centerY, i);
            
            Rect buttonRect = new Rect(buttonPos.x - BUTTON_SIZE / 2, buttonPos.y - BUTTON_SIZE / 2, BUTTON_SIZE, BUTTON_SIZE);
            
            // 현재 선택된 방향이면 다른 색상
            Color originalColor = GUI.backgroundColor;
            if (currentDirection == direction)
            {
                GUI.backgroundColor = Color.cyan;
            }
            
            // 방향 표시 텍스트
            string directionText = GetDirectionSymbol(direction);
            
            if (GUI.Button(buttonRect, directionText, EditorStyles.miniButton))
            {
                property.enumValueIndex = i;
                property.serializedObject.ApplyModifiedProperties();
            }
            
            GUI.backgroundColor = originalColor;
        }
        
        // 중앙에 현재 선택된 방향 표시
        Rect centerRect = new Rect(centerX - 30, centerY - 10, 60, 20);
        EditorGUI.LabelField(centerRect, GetDirectionName(currentDirection), EditorStyles.centeredGreyMiniLabel);
        
        EditorGUI.EndProperty();
    }
    
    // 8방향 버튼 위치 계산 (원형 배치)
    private Vector2 GetButtonPosition(float centerX, float centerY, int directionIndex)
    {
        float angle = directionIndex * 45f; // 45도씩 8방향
        float radian = angle * Mathf.Deg2Rad;
        float radius = 30f;
        
        float x = centerX + Mathf.Cos(radian) * radius;
        float y = centerY - Mathf.Sin(radian) * radius; // Y축 반전 (Unity GUI 좌표계)
        
        return new Vector2(x, y);
    }
    
    // 방향 심볼 반환
    private string GetDirectionSymbol(ItemDirection direction)
    {
        switch (direction)
        {
            case ItemDirection.Right:     return "→";
            case ItemDirection.RightUp:   return "↗";
            case ItemDirection.Up:        return "↑";
            case ItemDirection.LeftUp:    return "↖";
            case ItemDirection.Left:      return "←";
            case ItemDirection.LeftDown:  return "↙";
            case ItemDirection.Down:      return "↓";
            case ItemDirection.RightDown: return "↘";
            default: return "→";
        }
    }
    
    // 방향 이름 반환
    private string GetDirectionName(ItemDirection direction)
    {
        switch (direction)
        {
            case ItemDirection.Right:     return "오른쪽";
            case ItemDirection.RightUp:   return "오른쪽 위";
            case ItemDirection.Up:        return "위";
            case ItemDirection.LeftUp:    return "왼쪽 위";
            case ItemDirection.Left:      return "왼쪽";
            case ItemDirection.LeftDown:  return "왼쪽 아래";
            case ItemDirection.Down:      return "아래";
            case ItemDirection.RightDown: return "오른쪽 아래";
            default: return "오른쪽";
        }
    }
}
#endif 