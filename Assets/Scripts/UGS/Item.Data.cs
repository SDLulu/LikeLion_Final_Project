/* ===== Do not touch this. Auto Generated Code. ===== */
/* If you want custom code generation modify this => 'CodeGeneratorUnityEngine.cs' */

using GoogleSheet.Protocol.v2.Res;
using GoogleSheet.Protocol.v2.Req;
using UGS;
using System;
using UGS.IO;
using GoogleSheet;
using System.Collections.Generic;
using System.IO;
using GoogleSheet.Type;
using System.Reflection;
using UnityEngine;

namespace Item
{
    [GoogleSheet.Attribute.TableStruct]
    public partial class Data : ITable
    {
        // Google Sheet에서 불러왔는지 여부
        static bool isLoaded = false;

        // Google Spreadsheet 정보
        static string spreadSheetID = "1nOtR3KnENm_cWyBUZqETioM4Wi7dnC4hSiN9Lksv3L4";
        static string sheetID = "0";
        static UnityFileReader reader = new UnityFileReader();

        // 로드된 데이터를 저장하는 곳
        public static Dictionary<int, Data> DataMap = new Dictionary<int, Data>();
        public static List<Data> DataList = new List<Data>();

        // Google Sheet에서 데이터 불러오기 (자동 로드)
        public static List<Data> GetList()
        {
            if (isLoaded == false) Load();
            return DataList;
        }

        // Google Sheet에서 데이터 불러오기 (Dictionary 형태로)
        public static Dictionary<int, Data> GetDictionary()
        {
            if (isLoaded == false) Load();
            return DataMap;
        }

        // === 데이터 필드 ===
        public int DataID;
        public string ItemName;
        public int Damage;
        public float Knockback;
        public int MinMoney;
        public int MaxMoney;
        public int IsPassiveItem;
        public int SpawnChance;
        public string ItemIcon;
        public string PrefabPath;

        #region functions

        // 로컬 파일에서 데이터 불러오기
        public static void Load(bool forceReload = false)
        {
            if (isLoaded && forceReload == false)
            {
#if UGS_DEBUG
                Debug.Log("Data is already loaded! if you want reload then, forceReload parameter set true");
#endif
                return;
            }

            string text = reader.ReadData("Item");
            if (text != null)
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<ReadSpreadSheetResult>(text);
                CommonLoad(result.jsonObject, forceReload);
                if (!isLoaded) isLoaded = true;
            }
        }

        // Google Sheet에서 직접 데이터 불러오기 (웹 통신)
        public static void LoadFromGoogle(Action<List<Data>, Dictionary<int, Data>> onLoaded, bool updateCurrentData = false)
        {
            IHttpProtcol webInstance = null;
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                webInstance = UnityEditorWebRequest.Instance as IHttpProtcol;
            }
            else
            {
                webInstance = UnityPlayerWebRequest.Instance as IHttpProtcol;
            }
#endif
#if !UNITY_EDITOR
            webInstance = UnityPlayerWebRequest.Instance as IHttpProtcol;
#endif
            var mdl = new ReadSpreadSheetReqModel(spreadSheetID);
            webInstance.ReadSpreadSheet(mdl, OnError, (data) => {
                var loaded = CommonLoad(data.jsonObject, updateCurrentData);
                onLoaded?.Invoke(loaded.list, loaded.map);
            });
        }

        // 공통 데이터 로딩 처리
        public static (List<Data> list, Dictionary<int, Data> map) CommonLoad(Dictionary<string, Dictionary<string, List<string>>> jsonObject, bool forceReload)
        {
            Dictionary<int, Data> Map = new Dictionary<int, Data>();
            List<Data> List = new List<Data>();
            TypeMap.Init();

            FieldInfo[] fields = typeof(Data).GetFields(BindingFlags.Public | BindingFlags.Instance);
            List<(string original, string propertyName, string type)> typeInfos = new List<(string, string, string)>();
            List<List<string>> rows = new List<List<string>>();
            var sheet = jsonObject["Data"];

            foreach (var column in sheet.Keys)
            {
                string[] split = column.Replace(" ", null).Split(':');
                string column_field = split[0];
                string column_type = split[1];

                typeInfos.Add((column, column_field, column_type));
                List<string> typeValues = sheet[column];
                rows.Add(typeValues);
            }

            // 실제 데이터 파싱
            if (rows.Count != 0)
            {
                int rowCount = rows[0].Count;
                for (int i = 0; i < rowCount; i++)
                {
                    Data instance = new Data();
                    for (int j = 0; j < typeInfos.Count; j++)
                    {
                        try
                        {
                            var typeInfo = TypeMap.StrMap[typeInfos[j].type];
                            var readedValue = TypeMap.Map[typeInfo].Read(rows[j][i]);
                            fields[j].SetValue(instance, readedValue);
                        }
                        catch (Exception e)
                        {
                            if (e is UGSValueParseException)
                            {
                                Debug.LogError("<color=red> UGS Value Parse Failed! </color>");
                                Debug.LogError(e);
                                return (null, null);
                            }

                            // Enum 파싱 처리
                            var type = typeInfos[j].type.Replace("Enum<", null).Replace(">", null);
                            var readedValue = TypeMap.EnumMap[type].Read(rows[j][i]);
                            fields[j].SetValue(instance, readedValue);
                        }
                    }
                    List.Add(instance);
                    Map.Add(instance.DataID, instance);
                }

                if (isLoaded == false || forceReload)
                {
                    DataList = List;
                    DataMap = Map;
                    isLoaded = true;
                }
            }

            return (List, Map);
        }

        // Google Sheet에 한 줄 쓰기
        public static void Write(Data data, Action<WriteObjectResult> onWriteCallback = null)
        {
            TypeMap.Init();
            FieldInfo[] fields = typeof(Data).GetFields(BindingFlags.Public | BindingFlags.Instance);
            var datas = new string[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                var type = fields[i].FieldType;
                string writeRule = null;

                if (type.IsEnum)
                {
                    writeRule = TypeMap.EnumMap[type.Name].Write(fields[i].GetValue(data));
                }
                else
                {
                    writeRule = TypeMap.Map[type].Write(fields[i].GetValue(data));
                }

                datas[i] = writeRule;
            }

#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                UnityPlayerWebRequest.Instance.WriteObject(new WriteObjectReqModel(spreadSheetID, sheetID, datas[0], datas), OnError, onWriteCallback);
            }
            else
            {
                UnityPlayerWebRequest.Instance.WriteObject(new WriteObjectReqModel(spreadSheetID, sheetID, datas[0], datas), OnError, onWriteCallback);
            }
#endif

#if !UNITY_EDITOR
            UnityPlayerWebRequest.Instance.WriteObject(new WriteObjectReqModel(spreadSheetID, sheetID, datas[0], datas), OnError, onWriteCallback);
#endif
        }

        #endregion

        #region Odin Inspector Extensions

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button("UploadToSheet")]
        public void Upload()
        {
            Write(this);
        }
#endif

        #endregion

        // 공통 에러 처리
        public static void OnError(Exception e)
        {
            UnityGoogleSheet.OnTableError(e);
        }
    }
}
