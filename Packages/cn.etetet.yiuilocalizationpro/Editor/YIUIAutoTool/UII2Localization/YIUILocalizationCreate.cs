#if UNITY_EDITOR
using System;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using I2.Loc;
using UnityEditor;
using UnityEngine;

namespace YIUIFramework.Editor
{
    /// <summary>
    /// 导入本地化文件 一键生成I2Localization
    /// </summary>
    public static class YIUILocalizationCreate
    {
        private static string GetSourceXlsxResPath()
        {
            var sourcePath = "Packages/cn.etetet.yiuilocalizationpro/Luban/Config/Datas";
            var projPath = EditorHelper.GetProjPath(sourcePath);
            var path = $"{projPath}/{I2LocalizeHelper.I2ResAssetNamePrefix}{UII2LocalizationModule.UII2SourceResName}.xlsx";
            return path;
        }

        public static void CreateI2LocalizationByXlsx()
        {
            var editorAsset = UpgradeManager.CreateLanguageSources();
            var languageSourceData = editorAsset?.SourceData;

            if (languageSourceData == null)
            {
                UnityTipsHelper.ShowError($"没有找到多语言编辑器下的源数据 请重新生成");
                return;
            }

            var path = GetSourceXlsxResPath();

            try
            {
                var hashKey = new HashSet<string>();
                var excelData = ReadExcelFile(path, hashKey);
                if (excelData == null)
                {
                    return;
                }

                var sError = languageSourceData.Import_CSV(string.Empty, excelData, eSpreadsheetUpdateMode.Replace);
                if (!string.IsNullOrEmpty(sError))
                {
                    UnityTipsHelper.ShowError($"多语言生成全数据错误 请检查, {sError} ,{path}");
                    return;
                }

                if (!YIUILocalizationExport.ExportAllCsv(languageSourceData))
                {
                    return;
                }

                Selection.activeObject = editorAsset;
                EditorUtility.SetDirty(editorAsset);
                CreateScript(hashKey);
                CreateCheckKeyConfig(hashKey);
                EditorApplication.ExecuteMenuItem("ET/Excel/ExcelExporter");
            }
            catch (Exception e)
            {
                Debug.LogError($"多语言生成全数据错误 请检查 {e}");
                return;
            }

            UnityTipsHelper.Show($"多语言生成全数据完成 {path}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #region 读取配置文件

        private static List<string[]> ReadExcelFile(string path, HashSet<string> hashKey)
        {
            var data = new List<string[]>();
            var allLanguages = new List<string>();

            try
            {
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var workbook = new XSSFWorkbook(file);
                var sheet = workbook.GetSheetAt(0);
                var definition = sheet.GetRow(0);
                if (definition == null)
                {
                    Debug.LogError($"第一行 必须是定义行不可空 不可修改");
                    return data;
                }

                var definitionCount = definition.LastCellNum;
                if (definitionCount < 4)
                {
                    Debug.LogError($"必须有一个多语言设定");
                    return null;
                }

                var definitionData = new List<string>();
                definitionData.Add("Key"); //前3个为固定 所以不读了 直接写死
                definitionData.Add("Type");
                definitionData.Add("Desc");

                for (int cellIndex = 3; cellIndex < definitionCount; cellIndex++)
                {
                    var language = definition.GetCell(cellIndex);
                    if (language == null)
                    {
                        Debug.LogError($"第一行, 第{cellIndex + 1}列 不可以有空的多语言设定");
                        return null;
                    }

                    var value = GetCellValueAsString(language);
                    if (string.IsNullOrEmpty(value))
                    {
                        Debug.LogError($"第一行, 第{cellIndex + 1}列 不可以有空的多语言设定");
                        return null;
                    }

                    definitionData.Add(value);
                    allLanguages.Add(value);
                }

                data.Add(definitionData.ToArray());

                var autoComplete = YIUIConstHelper.Const.I2AutoComplete;
                var autoCompleteFormat = YIUIConstHelper.Const.I2AutoCompleteFormat;
                if (string.IsNullOrEmpty(autoCompleteFormat))
                {
                    autoCompleteFormat = "{0}";
                }

                var nullTranslationError = YIUIConstHelper.Const.I2NullTranslationError;
                var defaultLanguage = YIUIConstHelper.Const.I2DefaultLanguage; //默认语言
                if (string.IsNullOrEmpty(defaultLanguage))
                {
                    defaultLanguage = allLanguages[0];
                    Debug.Log($"没有设置默认语言 将第一个语言作为默认语言: {defaultLanguage}");
                }
                var commentLanguage = YIUIConstHelper.Const.I2CodeCommentLanguage; //注释语言
                if (string.IsNullOrEmpty(commentLanguage))
                {
                    commentLanguage = defaultLanguage;
                    Debug.Log($"没有设置注释语言 将默认语言作为注释语言: {commentLanguage}");
                }
                var defaultLanguageIndex = -1;
                var commentLanguageIndex = -1;
                for (int index = 0; index < allLanguages.Count; index++)
                {
                    var language = allLanguages[index];
                    if (language == defaultLanguage)
                    {
                        defaultLanguageIndex = index;
                    }

                    if (language == commentLanguage)
                    {
                        commentLanguageIndex = index;
                    }
                }

                if (defaultLanguageIndex == -1)
                {
                    Debug.LogError($"设定默认语言 {defaultLanguage},但是配置中没有这个语言 请检查默认语言是否存在");
                    return null;
                }

                if (commentLanguageIndex == -1)
                {
                    Debug.LogError($"设定注释语言 {commentLanguage},但是配置中没有这个语言 请检查注释语言是否存在");
                    return null;
                }

                for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null)
                    {
                        continue;
                    }

                    var firstCell = row.GetCell(0);
                    if (firstCell == null)
                    {
                        continue;
                    }

                    var keyValue = GetCellValueAsString(firstCell);
                    if (string.IsNullOrEmpty(keyValue))
                    {
                        continue;
                    }

                    //## = 忽略
                    if (keyValue.Contains("##"))
                    {
                        continue;
                    }

                    //key检查是否有空格
                    if (keyValue.Contains(" "))
                    {
                        Debug.LogError($"第{rowIndex + 1}行, key: {keyValue} ,有空格 已强制删除空格 请后续手动修改");
                        keyValue = keyValue.Replace(" ", "");
                    }

                    //重复检查
                    if (!hashKey.Add(keyValue))
                    {
                        Debug.LogError($"第{rowIndex + 1}行重复的Key: {keyValue}");
                        continue;
                    }

                    //从1开始读取所有内容
                    var rowData = new List<string>();
                    rowData.Add(keyValue);
                    var cellCount = row.LastCellNum;
                    for (int cellIndex = 1; cellIndex < cellCount; cellIndex++)
                    {
                        var cell = row.GetCell(cellIndex);
                        var value = GetCellValueAsString(cell);
                        rowData.Add(value);
                    }

                    //补齐空白
                    var limitCount = allLanguages.Count + 3;
                    if (rowData.Count < limitCount)
                    {
                        for (int i = 0; i < limitCount - rowData.Count; i++)
                        {
                            rowData.Add(string.Empty);
                        }
                    }

                    //语言内容空检查
                    var defValue = string.Empty;
                    for (int j = 3; j < rowData.Count; j++)
                    {
                        var content = rowData[j];
                        if (!string.IsNullOrEmpty(content))
                        {
                            defValue = content;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(defValue))
                    {
                        Debug.LogError($"第{rowIndex + 1}行, key: {keyValue} ,所有语言都没有配置内容 请检查");
                        continue;
                    }

                    //找到默认注释语言 如果其他语言没有填的则自动补齐
                    var commentIndex = commentLanguageIndex + 3;
                    var commentValue = string.Empty;
                    if (rowData.Count > commentIndex)
                    {
                        commentValue = rowData[commentIndex];
                    }

                    var autoValue = string.IsNullOrEmpty(commentValue) ? defValue : commentValue;

                    for (int j = 3; j < rowData.Count; j++)
                    {
                        var content = rowData[j];
                        if (string.IsNullOrEmpty(content))
                        {
                            //TODO 以后这里可以加判断 如果没有填就报错之类的 一般是测试前检查
                            //开发期间就自动填充了 防止报错
                            if (nullTranslationError)
                            {
                                Debug.LogError($"第{rowIndex + 1}行, key: {keyValue} ,{allLanguages[j - 3]} 语言没有配置内容 请检查");
                            }

                            if (autoComplete)
                            {
                                rowData[j] = string.Format(autoCompleteFormat, autoValue);
                            }
                        }
                    }

                    data.Add(rowData.ToArray());
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"读取Excel文件失败: {e}");
                return null;
            }

            return data;

            static string GetCellValueAsString(ICell cell)
            {
                if (cell == null)
                {
                    return string.Empty;
                }

                switch (cell.CellType)
                {
                    case CellType.String:
                        return cell.StringCellValue?.Trim() ?? string.Empty;
                    case CellType.Numeric:
                        return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
                    case CellType.Boolean:
                        return cell.BooleanCellValue.ToString();
                    case CellType.Formula:
                        try
                        {
                            if (cell.CachedFormulaResultType == CellType.String)
                                return cell.StringCellValue?.Trim() ?? string.Empty;
                            if (cell.CachedFormulaResultType == CellType.Numeric)
                                return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
                            if (cell.CachedFormulaResultType == CellType.Boolean)
                                return cell.BooleanCellValue.ToString();
                        }
                        catch
                        {
                            //忽略
                        }

                        return string.Empty;
                    default:
                        return string.Empty;
                }
            }
        }

        #endregion

        #region 生成检查元数据

        private const string TemplateBase = @"
[
{0}
]";

        private const string TemplateValue = @"
{""key"":""${key}""}";

        private static void CreateCheckKeyConfig(HashSet<string> hashKey)
        {
            var checkList = new List<string>();

            foreach (var key in hashKey)
            {
                var templateStr = TemplateValue.Replace("${key}", key);
                checkList.Add(templateStr);
            }

            var content = string.Format(TemplateBase, string.Join(",", checkList));

            WriteTextToProj($"{Application.dataPath}/../{CheckDefinesOutPath}/LocalizationCheck.json", content);
        }

        private const string CheckDefinesOutPath = "Packages/cn.etetet.yiuilocalizationpro/Luban/Config/Datas";

        private static bool WriteTextToProj(string path, string clsStr)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (dir == null)
                {
                    return false;
                }

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(path, clsStr);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"写入错误:{e}");
                return false;
            }
        }

        #endregion

        private static void CreateScript(HashSet<string> hashKey)
        {
            var globalSourcesAsset = UpgradeManager.CreateLanguageSources();
            LocalizationEditor.mLanguageSource = globalSourcesAsset.mSource;
            LocalizationEditor.mSelectedCategories.Clear();
            LocalizationEditor.mSelectedCategories.Add(LanguageSourceData.EmptyCategory);
            var hashCategory = new HashSet<string>();
            foreach (var key in hashKey)
            {
                LocalizationEditor.GetParsedTerm(key);
                var categories = LanguageSourceData.GetCategoryFromFullTerm(key);
                if (categories != key && hashCategory.Add(categories))
                {
                    LocalizationEditor.mSelectedCategories.Add(categories);
                }
            }

            LocalizationEditor.mSelectedKeys.Clear();
            LocalizationEditor.mSelectedKeys.AddRange(hashKey);
            LocalizationEditor.BuildScriptWithSelected();
        }
    }
}
#endif