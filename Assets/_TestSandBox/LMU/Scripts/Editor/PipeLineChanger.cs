using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using System;

public class PipeLineChanger : EditorWindow
{
    [MenuItem("에디터툴/편의기능/PipeLineChanger")]
    private static void OpenWindow()
    {
        GetWindow<PipeLineChanger>().Show();
    }

    // 찾은 렌더 파이프라인 자산들을 저장할 리스트입니다.
    private List<RenderPipelineAsset> foundPipelines = new List<RenderPipelineAsset>();
    
    // 선택된 파이프라인 인덱스
    private int selectedPipelineIndex = -1;
    private RenderPipelineAsset selectedPipeline;
    
    // 렌더링 파이프라인 변경 이력을 저장할 클래스
    [Serializable]
    private class PipelineHistory
    {
        public string fromName; // 이전 파이프라인 이름
        public string fromType; // 이전 파이프라인 유형
        public string toName;   // 새 파이프라인 이름
        public string toType;   // 새 파이프라인 유형
        public string timestamp; // 변경 시간
    }
    
    // 렌더링 파이프라인 히스토리 리스트
    private List<PipelineHistory> pipelineHistories = new List<PipelineHistory>();
    private const string PipelineHistoryKey = "PipelineChangerHistory";
    private bool showHistory = false;
    private Vector2 historyScrollPosition;
    // 히스토리 표시 최대 개수
    private const int MaxHistoryDisplayCount = 5;
    
    private void OnEnable()
    {
        // 저장된 히스토리 불러오기
        LoadPipelineHistory();
    }
    
    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("렌더 파이프라인 설정", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        // 현재 프로젝트에 적용된 렌더링 파이프라인 표시
        DisplayCurrentRenderPipeline();
        
        EditorGUILayout.Space(10);
        
        // 파이프라인 찾기 버튼
        if (GUILayout.Button("렌더링 파이프라인 찾기", GUILayout.Height(30)))
        {
            FindAllRenderingPipeLine();
        }
        
        EditorGUILayout.Space(10);
        
        // 드롭다운으로 파이프라인 선택
        if (foundPipelines.Count > 0)
        {
            EditorGUILayout.LabelField("선택된 렌더 파이프라인", EditorStyles.boldLabel);
            
            // 파이프라인 이름 목록 생성
            string[] pipelineNames = new string[foundPipelines.Count];
            for (int i = 0; i < foundPipelines.Count; i++)
            {
                pipelineNames[i] = foundPipelines[i].name;
            }
            
            // 드롭다운 표시
            int newIndex = EditorGUILayout.Popup(selectedPipelineIndex, pipelineNames);
            if (newIndex != selectedPipelineIndex)
            {
                selectedPipelineIndex = newIndex;
                selectedPipeline = foundPipelines[selectedPipelineIndex];
            }
            
            EditorGUILayout.Space(20);
            
            // 적용 버튼
            GUI.enabled = selectedPipelineIndex >= 0;
            if (GUILayout.Button("적용하기", GUILayout.Height(30)))
            {
                ApplyRenderingPipeLine();
            }
            GUI.enabled = true;
        }
        else
        {
            EditorGUILayout.HelpBox("파이프라인을 먼저 검색해주세요.", MessageType.Info);
        }
        
        // 히스토리 표시 섹션
        EditorGUILayout.Space(20);
        showHistory = EditorGUILayout.Foldout(showHistory, "렌더링 파이프라인 변경 이력", true);
        
        if (showHistory && pipelineHistories.Count > 0)
        {
            DisplayPipelineHistory();
        }
    }
    
    // 현재 프로젝트에 적용된 렌더링 파이프라인 표시 함수
    private void DisplayCurrentRenderPipeline()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("현재 프로젝트에 적용된 렌더링 파이프라인", EditorStyles.boldLabel);
        
        RenderPipelineAsset currentPipeline = QualitySettings.renderPipeline;
        
        GUIStyle infoStyle = new GUIStyle(EditorStyles.label);
        infoStyle.wordWrap = true;
        
        if (currentPipeline != null)
        {
            EditorGUILayout.LabelField("이름: " + currentPipeline.name, infoStyle);
            
            // 파이프라인 유형 표시
            string pipelineType = GetPipelineTypeName(currentPipeline);
            
            EditorGUILayout.LabelField("유형: " + pipelineType, infoStyle);
            EditorGUILayout.LabelField("경로: " + AssetDatabase.GetAssetPath(currentPipeline), infoStyle);
        }
        else
        {
            EditorGUILayout.LabelField("빌트인 렌더 파이프라인 (Built-in Render Pipeline)", infoStyle);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    // 파이프라인 유형 이름 가져오기
    private string GetPipelineTypeName(RenderPipelineAsset pipeline)
    {
        if (pipeline == null) return "빌트인 렌더 파이프라인";
        
        string pipelineType = "알 수 없음";
        if (pipeline.GetType().ToString().Contains("Universal"))
        {
            pipelineType = "Universal Render Pipeline (URP)";
        }
        else if (pipeline.GetType().ToString().Contains("HD"))
        {
            pipelineType = "High Definition Render Pipeline (HDRP)";
        }
        else if (pipeline.GetType().ToString().Contains("Lightweight"))
        {
            pipelineType = "Lightweight Render Pipeline (LWRP)";
        }
        
        return pipelineType;
    }
    
    // 파이프라인 변경 이력 표시 함수
    private void DisplayPipelineHistory()
    {
        GUIStyle historyHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
        historyHeaderStyle.alignment = TextAnchor.MiddleCenter;
        
        GUIStyle historyItemStyle = new GUIStyle(EditorStyles.label);
        historyItemStyle.wordWrap = true;
        
        GUIStyle arrowStyle = new GUIStyle(EditorStyles.boldLabel);
        arrowStyle.alignment = TextAnchor.MiddleCenter;
        arrowStyle.fontSize = 14;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // 스크롤 뷰 시작 - 높이를 조절하여 5개 항목이 보이도록 설정
        historyScrollPosition = EditorGUILayout.BeginScrollView(historyScrollPosition, 
            GUILayout.Height(Mathf.Min(pipelineHistories.Count, MaxHistoryDisplayCount) * 100));
        
        // 최대 보여줄 히스토리 항목 수 계산
        int displayCount = Mathf.Min(pipelineHistories.Count, 20); // 실제 저장은 최대 20개까지
        
        for (int i = 0; i < displayCount; i++)
        {
            PipelineHistory history = pipelineHistories[i];
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField($"변경 #{i + 1}: {history.timestamp}", historyHeaderStyle);
            EditorGUILayout.Space(5);
            
            // 이전 파이프라인 정보
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 20));
            EditorGUILayout.LabelField("변경 전:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(history.fromName, historyItemStyle);
            EditorGUILayout.LabelField(history.fromType, historyItemStyle);
            EditorGUILayout.EndVertical();
            
            // 화살표 표시
            EditorGUILayout.BeginVertical(GUILayout.Width(40));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("→", arrowStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            
            // 새 파이프라인 정보
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 20));
            EditorGUILayout.LabelField("변경 후:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(history.toName, historyItemStyle);
            EditorGUILayout.LabelField(history.toType, historyItemStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            if (i < displayCount - 1) // 마지막 항목이 아니면 간격 추가
            {
                EditorGUILayout.Space(3);
            }
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
    
    // 파이프라인 변경 이력 저장 함수
    private void SavePipelineHistory()
    {
        string json = JsonUtility.ToJson(new { histories = pipelineHistories });
        EditorPrefs.SetString(PipelineHistoryKey, json);
    }
    
    // 파이프라인 변경 이력 불러오기 함수
    private void LoadPipelineHistory()
    {
        if (EditorPrefs.HasKey(PipelineHistoryKey))
        {
            string json = EditorPrefs.GetString(PipelineHistoryKey);
            
            try
            {
                var wrapper = JsonUtility.FromJson<HistoryWrapper>(json);
                if (wrapper != null && wrapper.histories != null)
                {
                    pipelineHistories = wrapper.histories;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("파이프라인 히스토리 로드 중 오류 발생: " + e.Message);
                pipelineHistories = new List<PipelineHistory>();
            }
        }
    }
    
    // JsonUtility를 위한 래퍼 클래스
    [Serializable]
    private class HistoryWrapper
    {
        public List<PipelineHistory> histories;
    }
    
    public void FindAllRenderingPipeLine()
    {
        foundPipelines.Clear();
        selectedPipelineIndex = -1;
        // "t:RenderPipelineAsset"으로 검색하면 RenderPipelineAsset 타입의 모든 에셋을 찾을 수 있습니다.
        string[] guids = AssetDatabase.FindAssets("t:RenderPipelineAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
            if (asset != null)
            {
                foundPipelines.Add(asset);
            }
        }
        Debug.Log("총 " + foundPipelines.Count + "개의 렌더링 파이프라인을 찾았습니다.");
    }
    
    public void ApplyRenderingPipeLine()
    {
        if (selectedPipeline != null)
        {
            // 현재 파이프라인 정보 저장
            RenderPipelineAsset currentPipeline = QualitySettings.renderPipeline;
            
            // 히스토리 객체 생성
            PipelineHistory history = new PipelineHistory
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            // 이전 파이프라인 정보
            if (currentPipeline != null)
            {
                history.fromName = currentPipeline.name;
                history.fromType = GetPipelineTypeName(currentPipeline);
            }
            else
            {
                history.fromName = "빌트인 렌더 파이프라인";
                history.fromType = "Built-in Render Pipeline";
            }
            
            // 새 파이프라인 정보
            history.toName = selectedPipeline.name;
            history.toType = GetPipelineTypeName(selectedPipeline);
            
            // 히스토리에 추가
            pipelineHistories.Insert(0, history);
            
            // 히스토리 저장 수 제한 (최대 20개)
            if (pipelineHistories.Count > 20)
            {
                pipelineHistories.RemoveAt(pipelineHistories.Count - 1);
            }
            
            SavePipelineHistory();
            
            // Unity 2019.3 이상부터 GraphicsSettings.renderPipelineAsset를 사용합니다.
            QualitySettings.renderPipeline = selectedPipeline;
            Debug.Log("렌더링 파이프라인이 적용되었습니다: " + selectedPipeline.name);
            
            // 적용 후 화면 갱신
            Repaint();
        }
        else
        {
            Debug.LogWarning("적용할 렌더링 파이프라인이 선택되지 않았습니다.");
        }
    }
}
