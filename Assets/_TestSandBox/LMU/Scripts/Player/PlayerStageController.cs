using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스테이지에 의해 통제되는 컴포넌트
/// </summary>
public class PlayerStageController : NetworkBehaviour
{
    private ILogHandler originalLogHandler;
    private HashSet<string> playerComponentTypes = new HashSet<string>();

    private void Awake()
    {
        ApplyLoggerFilter();
    }

    private void OnDestroy()
    {
        RestoreOriginalLogger();
    }

    public override void Spawned()
    {
        base.Spawned();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
    }

    /// <summary>
    /// Unity Logger 필터링 적용 (플레이어 컴포넌트의 Debug.Log 차단)
    /// </summary>
    private void ApplyLoggerFilter()
    {
        originalLogHandler = Debug.unityLogger.logHandler;
        Debug.unityLogger.logHandler = new PlayerDebugLogFilter(originalLogHandler, playerComponentTypes);
    }

    /// <summary>
    /// 원래 로그 핸들러 복원
    /// </summary>
    private void RestoreOriginalLogger()
    {
        if (originalLogHandler != null)
        {
            Debug.unityLogger.logHandler = originalLogHandler;
        }
    }

    /// <summary>
    /// Note : 매프레임 호출하니 갑자기 위치가 튀는 문제 발생
    /// 내부적으로 RPC 쓰는것 같긴한데 그래서 문제가 발생하는듯?
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        var rigid = GetComponent<NetworkRigidbody2D>();
        rigid.Teleport(position);
    }

    public Vector2 GetPosition()
    {
        var rigid = GetComponent<NetworkRigidbody2D>();
        var ret = rigid.RBPosition;
        return ret;
    }

    public Vector2 GetScreenPosition()
    {
        var worldPos = GetPosition();
        var screenPos = Camera.main.WorldToScreenPoint(worldPos);
        return screenPos;
    }

    public bool IsAlive()
    {
        // Todo
        return true;
    }
}

/// <summary>
/// 플레이어 컴포넌트에서 오는 Debug 로그를 필터링하는 커스텀 로그 핸들러
/// </summary>
public class PlayerDebugLogFilter : ILogHandler
{
    private ILogHandler originalHandler;
    private HashSet<string> playerComponentTypes;
    
    public PlayerDebugLogFilter(ILogHandler originalHandler, HashSet<string> playerComponentTypes)
    {
        this.originalHandler = originalHandler;
        this.playerComponentTypes = playerComponentTypes;
    }
    
    public void LogFormat(UnityEngine.LogType logType, Object context, string format, params object[] args)
    {
        // 실제 메시지 구성 (args가 있으면 포맷팅)
        string actualMessage = args != null && args.Length > 0 ? string.Format(format, args) : format;
        
        // 플레이어 관련 로그인지 확인하여 차단
        if (ShouldFilterLog(context, actualMessage))
        {
            return; // 로그 차단 - 아무것도 출력하지 않음
        }
        
        // 차단되지 않은 로그는 원래 핸들러로 전달
        if (originalHandler != null)
        {
            originalHandler.LogFormat(logType, context, format, args);
        }
    }
    
    public void LogException(System.Exception exception, Object context)
    {
        // Exception은 항상 표시 (중요하므로)
        if (originalHandler != null)
        {
            originalHandler.LogException(exception, context);
        }
    }
    
    /// <summary>
    /// 로그를 필터링할지 결정
    /// </summary>
    private bool ShouldFilterLog(Object context, string message)
    {
        // 메시지 내용으로 플레이어 관련 로그 판별 (우선 확인)
        if (!string.IsNullOrEmpty(message))
        {
            string lowerMessage = message.ToLower();
            
            // 핵심 플레이어 관련 키워드들
            if (lowerMessage.Contains("🎮") || // 플레이어 컨트롤러
                lowerMessage.Contains("🌐") || // 네트워크 설정
                lowerMessage.Contains("🏠") || // 로컬 플레이어
                lowerMessage.Contains("🌍") || // 원격 플레이어
                lowerMessage.Contains("🎯") || // 물리 설정
                lowerMessage.Contains("🪜") || // 사다리
                lowerMessage.Contains("🎒") || // 아이템 픽업
                lowerMessage.Contains("🎨") || // UI 초기화
                lowerMessage.Contains("⚠️") || // 설정 확인
                lowerMessage.Contains("🦘") || // 점프
                lowerMessage.Contains("점프") ||
                lowerMessage.Contains("플레이어") ||
                lowerMessage.Contains("player") ||
                lowerMessage.Contains("network") ||
                lowerMessage.Contains("input authority") ||
                lowerMessage.Contains("rigidbody") ||
                lowerMessage.Contains("interpolated") ||
                lowerMessage.Contains("interpolate") ||
                lowerMessage.Contains("떨림 방지") ||
                lowerMessage.Contains("master client") ||
                lowerMessage.Contains("interpolation target") ||
                lowerMessage.Contains("interpolation data source"))
            {
                return true; // 차단
            }
        }
        
        // Context가 플레이어 컴포넌트인 경우
        if (context != null)
        {
            string contextTypeName = context.GetType().Name;
            if (playerComponentTypes.Contains(contextTypeName))
            {
                return true; // 차단
            }
        }
        
        return false; // 통과
    }
}
