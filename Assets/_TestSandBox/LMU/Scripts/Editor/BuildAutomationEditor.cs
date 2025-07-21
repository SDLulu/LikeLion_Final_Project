using UnityEngine;
using UnityEditor;
using System.IO;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public class BuildAutomationEditor : EditorWindow
{
    // EditorPrefs 키 상수들
    private const string PREF_BUILD_FOLDER = "BuildAutomation.BuildFolder";
    private const string PREF_EXECUTABLE_NAME = "BuildAutomation.ExecutableName";
    private const string PREF_WINDOW_WIDTH = "BuildAutomation.WindowWidth";
    private const string PREF_WINDOW_HEIGHT = "BuildAutomation.WindowHeight";
    private const string PREF_PLAYER_COUNT = "BuildAutomation.PlayerCount";
    
    // 기본값들
    private static string _buildFolderPath = @"C:\Users\dndej\OneDrive\바탕 화면\Unity_BuildSpace\Spel";
    private static string _executableName = "FusionTutorial";
    private static int _windowWidth = 800;
    private static int _windowHeight = 600;
    private static int _playerCount = 4;
    
    // 설정값이 로드되었는지 확인하는 플래그
    private static bool settingsLoaded = false;
    
    // 프로퍼티를 통한 lazy loading
    private static string buildFolderPath
    {
        get
        {
            if (!settingsLoaded) LoadSettings();
            return _buildFolderPath;
        }
        set
        {
            _buildFolderPath = value;
        }
    }
    
    private static string executableName
    {
        get
        {
            if (!settingsLoaded) LoadSettings();
            return _executableName;
        }
        set
        {
            _executableName = value;
        }
    }
    
    private static int windowWidth
    {
        get
        {
            if (!settingsLoaded) LoadSettings();
            return _windowWidth;
        }
        set
        {
            _windowWidth = value;
        }
    }
    
    private static int windowHeight
    {
        get
        {
            if (!settingsLoaded) LoadSettings();
            return _windowHeight;
        }
        set
        {
            _windowHeight = value;
        }
    }
    
    private static int playerCount
    {
        get
        {
            if (!settingsLoaded) LoadSettings();
            return _playerCount;
        }
        set
        {
            _playerCount = value;
        }
    }
    
    // delayCall 메모리 누수 방지를 위한 정적 델리게이트
    private static string pendingExecutablePath = "";
    private static EditorApplication.CallbackFunction launchInstancesCallback;
    private static EditorApplication.CallbackFunction arrangeWindowsCallback;
    
    private static Vector2Int[] windowPositions = {
        new Vector2Int(0, 0),      // 왼쪽 위
        new Vector2Int(800, 0),    // 오른쪽 위
        new Vector2Int(0, 600),    // 왼쪽 아래
        new Vector2Int(800, 600)   // 오른쪽 아래
    };
    
    // 정적 생성자에서 델리게이트 초기화
    static BuildAutomationEditor()
    {
        launchInstancesCallback = () => {
            if (!string.IsNullOrEmpty(pendingExecutablePath))
            {
                LaunchMultipleInstances(pendingExecutablePath);
                pendingExecutablePath = "";
            }
        };
        
        arrangeWindowsCallback = () => {
            ArrangeWindowsWithRetry();
        };
    }
    
    // 설정값 저장
    private static void SaveSettings()
    {
        EditorPrefs.SetString(PREF_BUILD_FOLDER, _buildFolderPath);
        EditorPrefs.SetString(PREF_EXECUTABLE_NAME, _executableName);
        EditorPrefs.SetInt(PREF_WINDOW_WIDTH, _windowWidth);
        EditorPrefs.SetInt(PREF_WINDOW_HEIGHT, _windowHeight);
        EditorPrefs.SetInt(PREF_PLAYER_COUNT, _playerCount);
    }
    
    // 설정값 불러오기
    private static void LoadSettings()
    {
        if (settingsLoaded) return;
        
        _buildFolderPath = EditorPrefs.GetString(PREF_BUILD_FOLDER, _buildFolderPath);
        _executableName = EditorPrefs.GetString(PREF_EXECUTABLE_NAME, _executableName);
        _windowWidth = EditorPrefs.GetInt(PREF_WINDOW_WIDTH, _windowWidth);
        _windowHeight = EditorPrefs.GetInt(PREF_WINDOW_HEIGHT, _windowHeight);
        _playerCount = EditorPrefs.GetInt(PREF_PLAYER_COUNT, _playerCount);
        
        settingsLoaded = true;
    }
    
    // 모니터 해상도에 따라 창 위치 자동 계산
    private static Vector2Int[] CalculateWindowPositions()
    {
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);
        
        // 작업 표시줄 높이 고려 (대략 40픽셀)
        int taskbarHeight = 40;
        int availableHeight = screenHeight - taskbarHeight;
        
        // 4분할 위치 계산
        int halfWidth = screenWidth / 2;
        int halfHeight = availableHeight / 2;
        
        return new Vector2Int[] {
            new Vector2Int(0, 0),                    // 왼쪽 위
            new Vector2Int(halfWidth, 0),            // 오른쪽 위
            new Vector2Int(0, halfHeight),           // 왼쪽 아래
            new Vector2Int(halfWidth, halfHeight)    // 오른쪽 아래
        };
    }
    
    // 모니터 해상도에 맞춰 창 크기 및 위치 설정
    private static void SetWindowSizeToMonitor()
    {
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);
        
        // 작업 표시줄 높이 고려
        int taskbarHeight = 40;
        int availableHeight = screenHeight - taskbarHeight;
        
        // 4분할 크기 설정
        windowWidth = screenWidth / 2;
        windowHeight = availableHeight / 2;
        
        // 위치 업데이트
        windowPositions = CalculateWindowPositions();
        
        UnityEngine.Debug.Log($"모니터 해상도 맞춤: {screenWidth}x{screenHeight}, 창 크기: {windowWidth}x{windowHeight}");
    }

    // Windows API for window manipulation
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    
    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
    
    // 모니터 해상도 가져오기
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const int SW_NORMAL = 1;
    
    // 시스템 메트릭스 상수
    private const int SM_CXSCREEN = 0; // 화면 너비
    private const int SM_CYSCREEN = 1; // 화면 높이

    [MenuItem("에디터툴/Build Automation/멀티플레이어 빌드 자동화")]
    public static void StartBuildAndTest()
    {
        BuildAndTest();
    }
    
    [MenuItem("에디터툴/Build Automation/빌드 자동화 설정")]
    public static void ShowWindow()
    {
        GetWindow<BuildAutomationEditor>("빌드 자동화 설정");
    }

    private void OnGUI()
    {
        GUILayout.Label("빌드 자동화 설정", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        // 값 변경 감지를 위한 임시 변수
        string newBuildFolderPath = EditorGUILayout.TextField("빌드 폴더 경로", buildFolderPath);
        string newExecutableName = EditorGUILayout.TextField("실행 파일명", executableName);
        
        EditorGUILayout.Space();
        if (GUILayout.Button("폴더 선택"))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("빌드 폴더 선택", buildFolderPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                newBuildFolderPath = selectedPath;
            }
        }
        
        EditorGUILayout.Space();
        
        int newWindowWidth = EditorGUILayout.IntField("창 너비", windowWidth);
        int newWindowHeight = EditorGUILayout.IntField("창 높이", windowHeight);
        int newPlayerCount = EditorGUILayout.IntSlider("플레이어 수", playerCount, 1, 4);
        
        // 값이 변경되었는지 확인하고 저장
        if (newBuildFolderPath != buildFolderPath || newExecutableName != executableName || 
            newWindowWidth != windowWidth || newWindowHeight != windowHeight || 
            newPlayerCount != playerCount)
        {
            buildFolderPath = newBuildFolderPath;
            executableName = newExecutableName;
            windowWidth = newWindowWidth;
            windowHeight = newWindowHeight;
            playerCount = newPlayerCount;
            SaveSettings();
        }
        
        EditorGUILayout.Space();
        if (GUILayout.Button("모니터 해상도 맞춤"))
        {
            SetWindowSizeToMonitor();
            SaveSettings();
        }
        
        // 현재 모니터 해상도 표시
        int currentScreenWidth = GetSystemMetrics(SM_CXSCREEN);
        int currentScreenHeight = GetSystemMetrics(SM_CYSCREEN);
        EditorGUILayout.LabelField($"현재 모니터 해상도: {currentScreenWidth} x {currentScreenHeight}", EditorStyles.miniLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("창 위치 설정", EditorStyles.boldLabel);
        
        string[] positionLabels = { "왼쪽 위", "오른쪽 위", "왼쪽 아래", "오른쪽 아래" };
        for (int i = 0; i < 4; i++)
        {
            windowPositions[i] = EditorGUILayout.Vector2IntField(positionLabels[i], windowPositions[i]);
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("빌드 및 테스트 시작", GUILayout.Height(30)))
        {
            BuildAndTest();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("• 빌드 폴더 경로는 절대 경로로 설정됩니다.\n• 예: C:\\UnityBuilds\\TestBuild\n• 이 기능은 Windows에서만 완전히 동작합니다.", MessageType.Info);
    }

    private static void BuildAndTest()
    {
        UnityEngine.Debug.Log("빌드 시작...");
        
        // 빌드 폴더 생성 (존재하지 않을 경우)
        if (!Directory.Exists(buildFolderPath))
        {
            Directory.CreateDirectory(buildFolderPath);
            UnityEngine.Debug.Log($"빌드 폴더 생성됨: {buildFolderPath}");
        }
        
        // 빌드 실행
        string executablePath = Path.Combine(buildFolderPath, executableName + ".exe");
        
        BuildPlayerOptions buildOptions = new BuildPlayerOptions();
        buildOptions.scenes = GetEnabledScenes();
        buildOptions.locationPathName = executablePath;
        buildOptions.target = BuildTarget.StandaloneWindows64;
        buildOptions.options = BuildOptions.None;
        
        var buildReport = BuildPipeline.BuildPlayer(buildOptions);
        
        if (buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            UnityEngine.Debug.Log("빌드 성공!");
            
            // 기존 delayCall 제거 후 새로운 실행 요청 등록
            CleanupDelayedCallbacks();
            pendingExecutablePath = executablePath;
            EditorApplication.delayCall += launchInstancesCallback;
        }
        else
        {
            UnityEngine.Debug.LogError("빌드 실패!");
        }
    }
    
    // delayCall 메모리 누수 방지를 위한 정리 메서드
    private static void CleanupDelayedCallbacks()
    {
        EditorApplication.delayCall -= launchInstancesCallback;
        EditorApplication.delayCall -= arrangeWindowsCallback;
        pendingExecutablePath = "";
    }

    private static void LaunchMultipleInstances(string executablePath)
    {
        if (!File.Exists(executablePath))
        {
            UnityEngine.Debug.LogError($"실행 파일을 찾을 수 없습니다: {executablePath}");
            return;
        }

        // 기존 프로세스 종료
        KillExistingProcesses();

        // 새 인스턴스들 실행
        for (int i = 0; i < playerCount; i++)
        {
            LaunchInstance(executablePath, i);
        }
        
        // 창 위치 조정을 위한 지연 호출 등록
        EditorApplication.delayCall += arrangeWindowsCallback;
    }

    private static void LaunchInstance(string executablePath, int instanceIndex)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = executablePath;
            startInfo.Arguments = $"-screen-width {windowWidth} -screen-height {windowHeight}";
            startInfo.UseShellExecute = false;
            
            Process process = Process.Start(startInfo);
            
            UnityEngine.Debug.Log($"인스턴스 {instanceIndex + 1} 실행됨");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"인스턴스 {instanceIndex + 1} 실행 실패: {e.Message}");
        }
    }

    private static void ArrangeWindowsWithRetry()
    {
        Task.Run(async () =>
        {
            int maxRetries = 10;
            int retryDelay = 1000; // 1초
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    await Task.Delay(retryDelay);
                    
                    // 실행 중인 프로세스들을 찾기
                    Process[] processes = Process.GetProcessesByName(executableName);
                    
                    if (processes.Length == 0)
                    {
                        UnityEngine.Debug.Log($"재시도 {attempt + 1}/{maxRetries}: 프로세스를 찾을 수 없음");
                        continue;
                    }
                    
                    // 모든 프로세스가 MainWindowHandle을 가지는지 확인
                    int validWindows = 0;
                    foreach (var process in processes)
                    {
                        if (process.MainWindowHandle != IntPtr.Zero)
                            validWindows++;
                    }
                    
                    if (validWindows < playerCount)
                    {
                        UnityEngine.Debug.Log($"재시도 {attempt + 1}/{maxRetries}: 유효한 창 수 {validWindows}/{playerCount}");
                        continue;
                    }
                    
                    // 창 위치 조정 실행
                    EditorApplication.delayCall += () => ArrangeWindows();
                    UnityEngine.Debug.Log($"창 배치 성공 (재시도 {attempt + 1}회)");
                    return;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"창 배치 재시도 {attempt + 1} 실패: {e.Message}");
                }
            }
            
            UnityEngine.Debug.LogError($"창 배치 최종 실패: {maxRetries}회 재시도 후 포기");
        });
    }
    
    private static void ArrangeWindows()
    {
        try
        {
            // 모니터 해상도에 맞춰 창 위치 다시 계산
            Vector2Int[] calculatedPositions = CalculateWindowPositions();
            
            // 실행 중인 프로세스들을 찾아서 창 위치 조정
            Process[] processes = Process.GetProcessesByName(executableName);
            
            UnityEngine.Debug.Log($"발견된 프로세스 수: {processes.Length}");
            
            for (int i = 0; i < Math.Min(processes.Length, playerCount); i++)
            {
                try
                {
                    IntPtr hWnd = processes[i].MainWindowHandle;
                    if (hWnd != IntPtr.Zero)
                    {
                        Vector2Int position = calculatedPositions[i];
                        SetWindowPos(hWnd, IntPtr.Zero, position.x, position.y, 
                                    windowWidth, windowHeight, SWP_NOZORDER | SWP_NOACTIVATE);
                        ShowWindow(hWnd, SW_NORMAL);
                        
                        UnityEngine.Debug.Log($"창 {i + 1} 위치 조정됨: ({position.x}, {position.y})");
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"창 {i + 1} 위치 조정 실패: {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"창 배치 실패: {e.Message}");
        }
    }

    private static void KillExistingProcesses()
    {
        try
        {
            Process[] processes = Process.GetProcessesByName(executableName);
            foreach (var process in processes)
            {
                process.Kill();
                process.WaitForExit(1000);
            }
            if (processes.Length > 0)
            {
                UnityEngine.Debug.Log($"기존 프로세스 {processes.Length}개 종료됨");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"기존 프로세스 종료 실패: {e.Message}");
        }
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }
        return scenes;
    }
} 