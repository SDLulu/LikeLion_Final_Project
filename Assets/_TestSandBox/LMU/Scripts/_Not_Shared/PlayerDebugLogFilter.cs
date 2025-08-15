using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어 디버그 로그를 필터링하는 컴포넌트
/// </summary>
public class PlayerDebugLogFilter : MonoBehaviour
{
    private ILogHandler _originalLogHandler;
    private CustomLogHandler _customLogHandler;
    private HashSet<string> _playerComponentTypes = new HashSet<string>();
    [SerializeField] private bool _isFiltering = true;

    private void Awake()
    {
        if (_isFiltering)
        {
            ApplyLoggerFilter();
        }
    }

    private void OnDestroy()
    {
        RestoreOriginalLogger();
    }

    /// <summary>
    /// Unity Logger 필터링 적용 (플레이어 컴포넌트의 Debug.Log 차단)
    /// </summary>
    private void ApplyLoggerFilter()
    {
        _originalLogHandler = Debug.unityLogger.logHandler;
        _customLogHandler = new CustomLogHandler(_originalLogHandler, _playerComponentTypes);
        Debug.unityLogger.logHandler = _customLogHandler;
    }

    /// <summary>
    /// 원래 로그 핸들러 복원
    /// </summary>
    private void RestoreOriginalLogger()
    {
        if (_originalLogHandler != null)
        {
            Debug.unityLogger.logHandler = _originalLogHandler;
        }
    }

    /// <summary>
    /// 필터링할 컴포넌트 타입을 추가
    /// </summary>
    /// <param name="componentTypeName">컴포넌트 타입 이름</param>
    public void AddFilteredComponentType(string componentTypeName)
    {
        _playerComponentTypes.Add(componentTypeName);
    }

    /// <summary>
    /// 필터링할 컴포넌트 타입을 제거
    /// </summary>
    /// <param name="componentTypeName">컴포넌트 타입 이름</param>
    public void RemoveFilteredComponentType(string componentTypeName)
    {
        _playerComponentTypes.Remove(componentTypeName);
    }
}

/// <summary>
/// 플레이어 컴포넌트에서 오는 Debug 로그를 필터링하는 커스텀 로그 핸들러
/// </summary>
public class CustomLogHandler : ILogHandler
{
    private ILogHandler _originalHandler;
    private HashSet<string> _playerComponentTypes;
    
    public CustomLogHandler(ILogHandler originalHandler, HashSet<string> playerComponentTypes)
    {
        _originalHandler = originalHandler;
        _playerComponentTypes = playerComponentTypes;
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
        if (_originalHandler != null)
        {
            _originalHandler.LogFormat(logType, context, format, args);
        }
    }
    
    public void LogException(System.Exception exception, Object context)
    {
        // Exception은 항상 표시 (중요하므로)
        if (_originalHandler != null)
        {
            _originalHandler.LogException(exception, context);
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
            if (lowerMessage.Contains("[bress(Clone)]") ||
                lowerMessage.Contains("[Visual]") ||
                lowerMessage.Contains("📷") ||
                lowerMessage.Contains("🎮") || // 플레이어 컨트롤러
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
                lowerMessage.Contains("interpolation data source") ||
                lowerMessage.Contains("아이템")
                )
            {
                return true; // 차단
            }
        }
        
        // Context가 플레이어 컴포넌트인 경우
        if (context != null)
        {
            string contextTypeName = context.GetType().Name;
            if (_playerComponentTypes.Contains(contextTypeName))
            {
                return true; // 차단
            }
        }
        
        return false; // 통과
    }
}
