using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ET;
using Microsoft.CodeAnalysis;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace YIUI.Numeric.Editor
{
    /// <summary>
    /// Numeric 生成器
    /// </summary>
    public class CreateNumeric
    {
        //定义导出位置
        private const string NumericDefinesOutPath = "Packages/cn.etetet.yiuinumericconfig/Luban/Config/Base/Defines";

        //配置导出位置
        private const string NumericConfigOutPath = "Packages/cn.etetet.yiuinumericconfig/Luban/Config/Datas";

        //影响公式导出位置
        private const string NumericAffectFormulaOutPath = "Packages/cn.etetet.yiuinumericconfig/Scripts/Hotfix/Share/Affect";

        private static void Log(string content)
        {
            Debug.LogError(content);
        }

        private bool CheckConfigVersion()
        {
            var path = $"{Application.dataPath}/../Packages";
            var configSourcePath = $"{path}/cn.etetet.yiuinumeric/configversions.txt";
            if (!File.Exists(configSourcePath))
            {
                Log($"没有找到配置文件 {configSourcePath}");
                return false;
            }

            var configSourceVersion = File.ReadAllText(configSourcePath);

            var configTargetPath = $"{path}/cn.etetet.yiuinumericconfig/configversions.txt";
            if (!File.Exists(configTargetPath))
            {
                Log($"没有找到配置文件 {configTargetPath}");
                return false;
            }

            var configTargetVersion = File.ReadAllText(configTargetPath);

            if (configSourceVersion != configTargetVersion)
            {
                Log($"配置文件版本不匹配 [{configSourceVersion}] != [{configTargetVersion}] 请更新配置文件 请参考文档:https://lib9kmxvq7k.feishu.cn/wiki/Sx86wDViniKzzVkhlUOc2s0VnEh");
                return false;
            }

            return true;
        }

        public bool Create()
        {
            if (!CheckConfigVersion()) return false;

            EditorUtility.DisplayProgressBar("数值", $"生成数据中...", 0);

            try
            {
                //Data
                if (!GetNumericTypeData())
                {
                    return false;
                }

                //Enum
                var enumContent = CreateEnum();
                if (string.IsNullOrEmpty(enumContent))
                {
                    return false;
                }

                if (!WriteTextToProj($"{Application.dataPath}/../{NumericDefinesOutPath}/numeric_enum.xml", enumContent))
                {
                    return false;
                }

                //Check
                var checkContent = CreateCheck();
                if (string.IsNullOrEmpty(checkContent))
                {
                    return false;
                }

                if (!WriteTextToProj($"{Application.dataPath}/../{NumericConfigOutPath}/NumericValueCheck.json", checkContent))
                {
                    return false;
                }

                //AffectData
                if (!GetAffectData())
                {
                    return false;
                }

                //AffectFormula
                var affectContent = CreateAffectFormula();
                if (string.IsNullOrEmpty(affectContent))
                {
                    return false;
                }

                affectContent = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(affectContent).GetRoot().NormalizeWhitespace().ToFullString();

                if (!WriteTextToProj($"{Application.dataPath}/../{NumericAffectFormulaOutPath}/NumericAffectSystem.cs", affectContent))
                {
                    return false;
                }

                //AffectConfig
                var affectConfigContent = CreateAffectConfig();
                if (string.IsNullOrEmpty(affectConfigContent))
                {
                    return false;
                }

                if (!WriteTextToProj($"{Application.dataPath}/../{NumericConfigOutPath}/NumericValueAffect.json", affectConfigContent))
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Log($"生成导出数据失败 {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return false;
        }

        #region NumericTool

        #region Data

        private readonly List<NumericTypeData> m_AllNumericTypeListData = new();
        private readonly Dictionary<string, int> m_AllNumeric = new();
        private readonly HashSet<int> m_NotGrow = new();

        private enum ENumericValueEditorType
        {
            None = 0,
            Int = 1,
            Long = 2,
            Bool = 3,
            Float = 4,
        }

        private class NumericTypeData
        {
            public int Id { get; set; } //0
            public string Name { get; set; } //1
            public string Alias { get; set; } //2
            public ENumericValueEditorType ValueType { get; set; } //3
            public bool NotGrow { get; set; } //4
            public bool NeedSave { get; set; } //5
            public int NoticeType { get; set; } //6
            public string Desc { get; set; } //7

            public int FormulaId { get; set; } //8
        }

        private const int Min = NumericConst.Min;
        private const int Max = NumericConst.Max;

        private List<string> FindAllExcelFiles(string excelName)
        {
            var foundFiles = new List<string>();
            var packagesPath = Path.Combine(Application.dataPath, "..", "Packages");

            if (!Directory.Exists(packagesPath))
            {
                return foundFiles;
            }

            var directories = Directory.GetDirectories(packagesPath, "cn.etetet.*", SearchOption.AllDirectories);
            foreach (string directory in directories)
            {
                var numericFilePath = Path.Combine(directory, "Luban", "Config", "Other", $"{excelName}.xlsx");
                if (File.Exists(numericFilePath))
                {
                    var dirInfo = new DirectoryInfo(directory);

                    var parentDirInfo = dirInfo.Parent;
                    var ignore = false;
                    while (true)
                    {
                        if (parentDirInfo == null)
                        {
                            break;
                        }

                        var name = parentDirInfo.Name;
                        if (name.StartsWith('.') || name.StartsWith('~'))
                        {
                            ignore = true;
                            break;
                        }

                        parentDirInfo = parentDirInfo.Parent;
                    }

                    if (ignore)
                    {
                        continue;
                    }

                    foundFiles.Add(numericFilePath);
                }
            }

            return foundFiles;
        }

        private bool GetNumericTypeData()
        {
            m_NotGrow.Clear();
            m_AllNumeric.Clear();
            m_AllNumericTypeListData.Clear();
            var allFiles = FindAllExcelFiles("NumericType");
            var allId = new HashSet<int>();
            var allName = new HashSet<string>();
            var allAlias = new HashSet<string>();

            foreach (var file in allFiles)
            {
                var dirInfo = new DirectoryInfo(file);
                var fullName = dirInfo.FullName;

                try
                {
                    var book = new XSSFWorkbook(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                    var sheet = book.GetSheetAt(0);
                    var rowCount = sheet.LastRowNum + 1; //总行数 因为0开始他这里给-1了 下面的公式都是按+1计算的
                    for (int i = 3; i < rowCount; i++)
                    {
                        IRow firstRow = sheet.GetRow(i); //每一行
                        if (firstRow == null)
                        {
                            //Log($" 第{i + 1}行错误 没有读取到 请检查");
                            continue;
                        }

                        var cell0 = firstRow.GetString(0);

                        //不填/有# =注释行
                        if (string.IsNullOrEmpty(cell0) || cell0.Contains($"#"))
                        {
                            continue;
                        }

                        int.TryParse(cell0, out int id); //ID

                        if (id is < Min or > Max)
                        {
                            Log($"{fullName}, 第{i + 1}行错误 ID错误 范围{Min}-{Max} 请检查: {id}");
                            continue;
                        }

                        if (allId.Contains(id))
                        {
                            Log($"{fullName}, 第{i + 1}行错误 ID重复 请检查: {id}");
                            continue;
                        }

                        var name = firstRow.GetString(1); //名称
                        if (string.IsNullOrEmpty(name))
                        {
                            Log($"{fullName}, 第{i + 1}行错误 没有名字 请检查");
                            continue;
                        }

                        if (allName.Contains(name))
                        {
                            Log($"{fullName}, 第{i + 1}行错误 名字重复 请检查: {name}");
                            continue;
                        }

                        var alias = firstRow.GetString(2); //别名
                        if (string.IsNullOrEmpty(alias))
                        {
                            Log($"{fullName}, 第{i + 1}行 ID: {name} 必须配置别名 请检查");
                            continue;
                        }

                        if (allAlias.Contains(alias))
                        {
                            Log($"{fullName}, 第{i + 1}行错误 别名重复 请检查: {alias}");
                            continue;
                        }

                        if (!Enum.TryParse<ENumericValueEditorType>(firstRow.GetString(3), true, out var valueType)) //类型
                        {
                            Log($"{fullName}, 第{i + 1}行错误 类型错误 请检查: {firstRow.GetString(3)}");
                            continue;
                        }

                        var notGrow = firstRow.GetBool(4); //成长
                        if (valueType == ENumericValueEditorType.Bool)
                        {
                            notGrow = true;
                        }

                        if (notGrow)
                        {
                            m_NotGrow.Add(id);
                        }

                        var needSave = firstRow.GetBool(5); //是否需要保存

                        int.TryParse(firstRow.GetString(6), out int noticeType); //通知类型

                        var desc = firstRow.GetString(7); //描述
                        if (string.IsNullOrEmpty(desc))
                        {
                            desc = alias;
                        }

                        int.TryParse(firstRow.GetString(8), out int formulaId); //公式ID

                        allId.Add(id);
                        allName.Add(name);
                        allAlias.Add(alias);

                        var data = new NumericTypeData
                        {
                            Id = id,
                            Name = name,
                            Alias = alias,
                            ValueType = valueType,
                            NotGrow = notGrow,
                            NeedSave = needSave,
                            NoticeType = noticeType,
                            Desc = desc,
                            FormulaId = formulaId,
                        };

                        m_AllNumericTypeListData.Add(data);

                        if (!m_AllNumeric.TryAdd($"{alias}_0", id))
                        {
                            Log($"{fullName}, 添加失败: [{alias}],[{id}] 有重复");
                        }

                        if (!notGrow)
                        {
                            for (int j = 1; j < 7; j++)
                            {
                                if (!m_AllNumeric.TryAdd($"{alias}_{j}", (id * 10 + j)))
                                {
                                    Log($"{fullName}, 添加失败: [{alias}],[{id}] 有重复");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"{fullName}, 导出失败:(提示表打开的时候是无法导出的) 信息:\n{ex.Message}");
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Enum

        readonly string TemplateXml = @"
<module name=""cn.etetet.yiuinumericconfig"" >
<enum name=""ENumericType"" >
<!-- 无效 -->
<var name=""None"" value=""0""  alias=""无"" comment = ""无""/>

{0}
</enum>
</module>";

        readonly string TemplateNumeric = @"
<!-- ${Desc} -->
<var name=""${Name}0"" value=""${Id0}""  alias=""${Alias}_0"" comment = ""${Alias}_0 (${ValueType})""/> 
<var name=""${Name}1"" value=""${Id1}"" alias=""${Alias}_1"" comment = ""${Alias}_1 (${ValueType})""/>
<var name=""${Name}2"" value=""${Id2}"" alias=""${Alias}_2"" comment = ""${Alias}_2 (${ValueType})""/>
<var name=""${Name}3"" value=""${Id3}"" alias=""${Alias}_3"" comment = ""${Alias}_3 (Float)""/>
<var name=""${Name}4"" value=""${Id4}"" alias=""${Alias}_4"" comment = ""${Alias}_4 (${ValueType})""/>
<var name=""${Name}5"" value=""${Id5}"" alias=""${Alias}_5"" comment = ""${Alias}_5 (Float)""/>
<var name=""${Name}6"" value=""${Id6}"" alias=""${Alias}_6"" comment = ""${Alias}_6 (${ValueType})""/>
";

        readonly string NotGrowTemplateNumeric = @"
<!-- ${Desc} -->
<var name=""${Name}0"" value=""${Id0}""  alias=""${Alias}_0"" comment = ""${Alias}_0 (${ValueType})""/> 
";

        /*
        string TemplateNumeric2 = @"
        comment=""0最终 ${Desc} 最终"" />
        comment=""1基础 ${Desc} 基础"" />
        comment=""2基础 ${Desc} 增加"" />
        comment=""3基础 ${Desc} 增加百分比"" />
        comment=""4基础 ${Desc} 最终增加"" />
        comment=""5基础 ${Desc} 最终增加百分比"" />
        comment=""6基础 ${Desc} 结果增加"" />
        ";
        */

        private string CreateEnum()
        {
            var numericSb = new StringBuilder();

            foreach (var data in m_AllNumericTypeListData)
            {
                var id = data.Id;
                var name = data.Name;
                var notGrow = data.NotGrow;
                var desc = data.Desc;
                var alias = data.Alias;
                var valueType = data.ValueType.ToString();
                var tempFormat = notGrow ? NotGrowTemplateNumeric : TemplateNumeric;
                var templateStr = tempFormat.Replace("${Name}", name)
                                            .Replace("${Id0}", id.ToString())
                                            .Replace("${Id1}", (id * 10 + 1).ToString())
                                            .Replace("${Id2}", (id * 10 + 2).ToString())
                                            .Replace("${Id3}", (id * 10 + 3).ToString())
                                            .Replace("${Id4}", (id * 10 + 4).ToString())
                                            .Replace("${Id5}", (id * 10 + 5).ToString())
                                            .Replace("${Id6}", (id * 10 + 6).ToString())
                                            .Replace("${Desc}", desc)
                                            .Replace("${Alias}", alias)
                                            .Replace("${ValueType}", valueType);

                numericSb.Append(templateStr);
            }

            return string.Format(TemplateXml, numericSb);
        }

        #endregion

        #region Check

        readonly string TemplateValueTypeCS = @"
[
{0}
]";

        readonly string TemplateValueNumeric = @"
{""id"":""${Id0}"", ""check"":""${Check0}"",""alias"":""${Alias}"",""desc"":""${Desc}"",""not_grow"":false,""need_save"":${NeedSave},""notice_type"":${NoticeType},""formula_id"":${FormulaId}},
{""id"":""${Id1}"",""check"":""${Check1}"",""alias"":"""",""desc"":"""",""not_grow"":false,""need_save"":${NeedSave},""notice_type"":${NoticeType},""formula_id"":${FormulaId}},
{""id"":""${Id2}"",""check"":""${Check2}"",""alias"":"""",""desc"":"""",""not_grow"":false,""need_save"":${NeedSave},""notice_type"":${NoticeType},""formula_id"":${FormulaId}},
{""id"":""${Id3}"",""check"":""${Check3}"",""alias"":"""",""desc"":"""",""not_grow"":false,""need_save"":${NeedSave},""notice_type"":${NoticeType},""formula_id"":${FormulaId}},
{""id"":""${Id4}"",""check"":""${Check4}"",""alias"":"""",""desc"":"""",""not_grow"":false,""need_save"":${NeedSave},""notice_type"":${NoticeType},""formula_id"":${FormulaId}},
{""id"":""${Id5}"",""check"":""${Check5}"",""alias"":"""",""desc"":"""",""not_grow"":false,""need_save"":${NeedSave},""notice_type"":${NoticeType},""formula_id"":${FormulaId}},
{""id"":""${Id6}"",""check"":""${Check6}"",""alias"":"""",""desc"":"""",""not_grow"":false,""need_save"":${NeedSave},""notice_type"":${NoticeType},""formula_id"":${FormulaId}}";

        readonly string NotGrowTemplateValueNumeric = @"
{""id"":""${Id0}"", ""check"":""${Check0}"",""alias"":""${Alias}"",""desc"":""${Desc}"",""not_grow"":true,""need_save"":${NeedSave},""notice_type"":${NoticeType},""formula_id"":${FormulaId}}";

        private string CreateCheck()
        {
            var checkList = new List<string>();

            for (int index = 0; index < m_AllNumericTypeListData.Count; index++)
            {
                var data = m_AllNumericTypeListData[index];
                var id = data.Id;
                var notGrow = data.NotGrow;
                var desc = data.Desc;
                var alias = data.Alias;
                var needSave = data.NeedSave;
                var noticeType = data.NoticeType;
                var valueType = data.ValueType.ToString();
                var formulaId = data.FormulaId;

                var tempFormat = notGrow ? this.NotGrowTemplateValueNumeric : this.TemplateValueNumeric;
                var templateStr = tempFormat
                                  .Replace("${Alias}", alias)
                                  .Replace("${Desc}", desc)
                                  .Replace("${Id0}", id.ToString())
                                  .Replace("${Id1}", (id * 10 + 1).ToString())
                                  .Replace("${Id2}", (id * 10 + 2).ToString())
                                  .Replace("${Id3}", (id * 10 + 3).ToString())
                                  .Replace("${Id4}", (id * 10 + 4).ToString())
                                  .Replace("${Id5}", (id * 10 + 5).ToString())
                                  .Replace("${Id6}", (id * 10 + 6).ToString())
                                  .Replace("${Check0}", valueType)
                                  .Replace("${Check1}", valueType)
                                  .Replace("${Check2}", valueType)
                                  .Replace("${Check3}", "Float")
                                  .Replace("${Check4}", valueType)
                                  .Replace("${Check5}", "Float")
                                  .Replace("${Check6}", valueType)
                                  .Replace("${NeedSave}", needSave ? "true" : "false")
                                  .Replace("${NoticeType}", noticeType.ToString())
                                  .Replace("${FormulaId}", formulaId.ToString());
                checkList.Add(templateStr);
            }

            return string.Format(TemplateValueTypeCS, string.Join(",", checkList));
        }

        #endregion

        #region Affect

        readonly string TemplateAffectCS = @"
using System;
using System.Collections.Generic;
using System.Linq;

//此文件由数值系统自动生成，请不要手动修改
namespace ET
{{
{0}
}}";

        readonly string TemplateAffectInvoke = @"
    [Invoke(${InvokeType})] //${NumericType},${AffectType}
    public class NumericAffectInvokeHandler_${InvokeType} : AInvokeHandler<NumericAffect, long>
    {
        /*
        ${Desc}
         */
        public override long Handle(NumericAffect A)
        {${Formula}
        }
    }
";

        private readonly Dictionary<string, Dictionary<string, AffectData>> m_AllAffectData = new();

        private class AffectData
        {
            public string NumericType;
            public string AffectType;
            public string AffectFormula;
            public string Desc;
        }

        private bool GetAffectData()
        {
            m_AllAffectData.Clear();
            var allFiles = FindAllExcelFiles("NumericValueAffect");

            foreach (var file in allFiles)
            {
                var dirInfo = new DirectoryInfo(file);
                var fullName = dirInfo.FullName;
                var lastNumericType = "";
                try
                {
                    var book = new XSSFWorkbook(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                    var sheet = book.GetSheetAt(0);
                    var rowCount = sheet.LastRowNum + 1; //总行数 因为0开始他这里给-1了 下面的公式都是按+1计算的
                    for (int i = 3; i < rowCount; i++)
                    {
                        IRow firstRow = sheet.GetRow(i); //每一行
                        if (firstRow == null)
                        {
                            //Log($" 第{i + 1}行错误 没有读取到 请检查");
                            continue;
                        }

                        var numericType = firstRow.GetString(1); //影响者
                        var numericTypeNull = string.IsNullOrEmpty(numericType);
                        if (!numericTypeNull && !m_AllNumeric.ContainsKey(numericType))
                        {
                            Log($"{fullName},  第{i + 1}行错误 影响者类型不存在 请检查: {numericType}");
                            lastNumericType = "";
                            continue;
                        }

                        var ignore = firstRow.GetString(0).Contains("#"); //忽略
                        if (ignore)
                        {
                            if (!numericTypeNull)
                            {
                                lastNumericType = "";
                            }

                            continue;
                        }

                        if (!numericTypeNull)
                        {
                            lastNumericType = numericType;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(lastNumericType))
                            {
                                continue;
                            }
                        }

                        var affectType = firstRow.GetString(2); //被影响类型
                        if (string.IsNullOrEmpty(affectType))
                        {
                            Log($"{fullName},  第{i + 1}行错误 C列 被影响类型必须填写 请检查");
                            continue;
                        }

                        if (!m_AllNumeric.TryGetValue(affectType, out int affectId))
                        {
                            Log($"{fullName},  第{i + 1}行错误 被影响类型不存在 请检查: {affectType}");
                            continue;
                        }

                        if (!CheckChangeNumeric(affectId))
                        {
                            Log($"{fullName},  第{i + 1}行错误 被影响类型不允许改变 请检查: {affectType}, {affectId}");
                            continue;
                        }

                        var affectFormula = firstRow.GetString(3); //影响公式
                        if (string.IsNullOrEmpty(affectType))
                        {
                            Log($"{fullName},  第{i + 1}行错误 D列 影响公式必须填写 请检查");
                            continue;
                        }

                        var desc = firstRow.GetString(4); //描述

                        if (!m_AllAffectData.TryGetValue(lastNumericType, out var affectDict))
                        {
                            affectDict = new();
                            m_AllAffectData.Add(lastNumericType, affectDict);
                        }

                        if (affectDict.ContainsKey(affectType))
                        {
                            Log($"{fullName},  第{i + 1}行错误 相同的影响类型和被影响类型不能重复 请检查: {lastNumericType}, {affectType}");
                            continue;
                        }

                        var affectDataTemp = new AffectData()
                        {
                            NumericType = lastNumericType,
                            AffectType = affectType,
                            AffectFormula = affectFormula,
                            Desc = desc
                        };

                        affectDict.Add(affectType, affectDataTemp);
                    }
                }
                catch (Exception ex)
                {
                    Log($"{fullName}, 导出失败:(提示表打开的时候是无法导出的) 信息:\n{ex.Message}");
                    return false;
                }
            }

            return true;
        }

        private string CreateAffectFormula()
        {
            var affectSb = new StringBuilder();

            var allUniqueId = new HashSet<long>();

            foreach (var numericType in m_AllAffectData.Keys)
            {
                var affectDict = m_AllAffectData[numericType];
                foreach (var affectType in affectDict.Keys)
                {
                    var affectDataTemp = affectDict[affectType];

                    var numericId = m_AllNumeric[numericType];
                    var affectId = m_AllNumeric[affectType];
                    var uniqueId = GenerateUniqueId(numericId, affectId);
                    if (uniqueId == 0) continue;

                    if (!allUniqueId.Add(uniqueId))
                    {
                        Log($"影响类型重复 请检查 [{numericType}],[{affectType}] ");
                        continue;
                    }

                    var formula = affectDataTemp.AffectFormula;

                    if (!formula.Contains("\n"))
                    {
                        if (!formula.Contains("return"))
                        {
                            formula = $"return {formula}";
                        }
                    }

                    if (!formula.EndsWith(";"))
                    {
                        formula = $"{formula};";
                    }

                    var desc = affectDataTemp.Desc.Replace("\n", "\n        ");

                    var templateStr = TemplateAffectInvoke
                                      .Replace("${InvokeType}", uniqueId.ToString())
                                      .Replace("${NumericType}", $"{numericType}({numericId})")
                                      .Replace("${AffectType}", $"{affectType}({affectId})")
                                      .Replace("${Desc}", desc)
                                      .Replace("${Formula}", $"\n{formula}");

                    affectSb.Append(templateStr);
                }
            }

            return string.Format(TemplateAffectCS, affectSb);
        }

        readonly string TemplateValueAffectTypeCS = @"
[
{0}
]";

        readonly string TemplateValueAffect = @"
{{""id"":""{0}"", ""affects"":[{1}]}}";

        private string CreateAffectConfig()
        {
            var configList = new List<string>();
            foreach (var numericEntry in m_AllAffectData)
            {
                var numericType = numericEntry.Key;
                if (!m_AllNumeric.TryGetValue(numericType, out int numericId))
                {
                    Log($"转换失败: 影响类型'{numericType}'不存在");
                    continue;
                }

                var affectsList = new List<string>();
                foreach (var affectEntry in numericEntry.Value)
                {
                    var affectType = affectEntry.Value.AffectType;
                    if (!m_AllNumeric.TryGetValue(affectType, out int affectId))
                    {
                        Log($"转换失败: 影响类型'{numericType}'不存在");
                        continue;
                    }

                    affectsList.Add(affectId.ToString());
                }

                var affectsStr = string.Join(",", affectsList);
                configList.Add(string.Format(TemplateValueAffect, numericId, affectsStr));
            }

            return string.Format(this.TemplateValueAffectTypeCS, string.Join(",", configList));
        }

        private long GenerateUniqueId(int value1, int value2)
        {
            if (value1 <= 0 || value2 <= 0)
            {
                Log($"生成唯一ID失败: 值必须大于0 请检查: {value1}, {value2}");
                return 0;
            }

            return ((long)value1 << 32) | (value2 & 0xFFFFFFFFL);
        }

        /// <summary>
        /// 检查修改数值的合理性
        /// </summary>
        private bool CheckChangeNumeric(int numericId)
        {
            //[Min - Max] = 这个数值的最终值 是不允许直接修改的
            if (numericId is >= NumericConst.Min and <= NumericConst.Max)
            {
                //最新版 0可以修改 前提是非成长
                //最新版非成长 只有0这个一个数据了  没有0之后的任何数据
                if (IsNotGrowNumeric(numericId))
                {
                    return true;
                }

                Log($"不允许直接修改最终数据 {numericId}");
                return false;
            }

            //只能修改[min*10+1 - max*10+6] 这个范围的值
            if (numericId is < NumericConst.ChangeMin or > NumericConst.ChangeMax)
            {
                Log($"只能修改[{NumericConst.ChangeMin} - {NumericConst.ChangeMax}] 这个范围的值 当前={numericId}");
                return false;
            }

            //且个位数必须是 1-RangeMax; RangeMax 最高=9;
            var mod = numericId % 10;
            if (mod is <= 0 or > NumericConst.RangeMax)
            {
                Log($"不合法的ID 个位数必须是=( 1 - {mod > NumericConst.RangeMax}) 当前={numericId}");
                return false;
            }

            //如果检查到这个值是非成长的 说明你不能修改他
            if (IsNotGrowNumeric(numericId))
            {
                return false;
            }

            return true;
        }

        private bool IsNotGrowNumeric(int numericId)
        {
            return m_NotGrow.Contains(numericId);
        }

        #endregion

        private bool WriteTextToProj(string path, string clsStr)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
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
                Log($"写入错误:{e}");
                return false;
            }
        }

        #endregion
    }

    public static class NPOIExtensions
    {
        public static string GetString(this IRow self, int index)
        {
            var tempStr = "";
            var cellContent = self.GetCell(index);
            if (cellContent != null)
            {
                tempStr = cellContent.ToString();
            }

            return tempStr;
        }

        public static bool GetBool(this IRow self, int index)
        {
            var tempStr = self.GetString(index);
            if (string.IsNullOrEmpty(tempStr))
            {
                return false;
            }

            if (tempStr.ToLower() == "true")
            {
                return true;
            }

            int.TryParse(tempStr, out var result);
            return result == 1;
        }

        public static int GetInt(this IRow self, int index)
        {
            var tempStr = self.GetString(index);
            if (string.IsNullOrEmpty(tempStr))
            {
                return 0;
            }

            int.TryParse(tempStr, out var result);
            return result;
        }
    }
}