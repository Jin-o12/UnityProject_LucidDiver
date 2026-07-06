using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ExcelDataReader;
using Newtonsoft.Json;
/// <summary>
/// Excel -> JSON 데이터 변환기
/// </summary>

public class UniversalExcelConverter : EditorWindow
{
    [MenuItem("Tools/데이터 관리/모든 엑셀 범용 변환 (Flat JSON)")]
    public static void ConvertAllExcelToJson()
    {
        string excelFolderPath = Application.dataPath + "/Resources/Excel";
        string jsonFolderPath = Application.dataPath + "/Resources/JSON";

        if (!Directory.Exists(excelFolderPath)) Directory.CreateDirectory(excelFolderPath);
        if (!Directory.Exists(jsonFolderPath)) Directory.CreateDirectory(jsonFolderPath);

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        string[] excelFiles = Directory.GetFiles(excelFolderPath, "*.xlsx");

        int successCount = 0;

        foreach (string filePath in excelFiles)
        {
            string fileName = Path.GetFileName(filePath);
            if (fileName.StartsWith("~$")) continue;

            // 어떤 엑셀이든 담을 수 있는 범용 리스트
            List<Dictionary<string, object>> flatDataList = new List<Dictionary<string, object>>();

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet();
                    var table = result.Tables[0];

                    // 1. 헤더(1행) 읽어오기
                    List<string> headers = new List<string>();
                    for (int col = 0; col < table.Columns.Count; col++)
                    {
                        headers.Add(table.Rows[0][col].ToString().Trim());
                    }

                    // 2. 데이터(2행부터) 읽어오기
                    for (int row = 1; row < table.Rows.Count; row++)
                    {
                        var rowData = new Dictionary<string, object>();
                        bool isEmptyRow = true;

                        for (int col = 0; col < table.Columns.Count; col++)
                        {
                            string header = headers[col];
                            if (string.IsNullOrEmpty(header)) continue; // 헤더가 없으면 무시

                            object rawValue = table.Rows[row][col];
                            
                            // 빈 셀이 아니라면 데이터 타입 분석 후 저장
                            if (rawValue != null && rawValue != DBNull.Value && !string.IsNullOrWhiteSpace(rawValue.ToString()))
                            {
                                isEmptyRow = false;
                                rowData[header] = ParseValue(rawValue.ToString());
                            }
                        }

                        // 빈 줄이 아닐 때만 최종 리스트에 추가
                        if (!isEmptyRow) flatDataList.Add(rowData);
                    }
                }
            }

            // 파일 이름 그대로 JSON 저장
            string jsonOutput = JsonConvert.SerializeObject(flatDataList, Formatting.Indented);
            string savePath = Path.Combine(jsonFolderPath, Path.GetFileNameWithoutExtension(fileName) + ".json");
            
            File.WriteAllText(savePath, jsonOutput);
            successCount++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"총 {successCount}개의 엑셀 파일이 JSON으로 변환되었습니다");
    }

    // 핵심: 문자열을 분석해서 알맞은 자료형으로 자동 캐스팅해주는 헬퍼 메서드
    private static object ParseValue(string value)
    {
        value = value.Trim();
        if (int.TryParse(value, out int intVal)) return intVal;
        if (float.TryParse(value, out float floatVal)) return floatVal;
        if (bool.TryParse(value, out bool boolVal)) return boolVal; // TRUE/FALSE 처리
        
        return value; // 아무것도 아니면 그냥 문자열로 반환
    }
}