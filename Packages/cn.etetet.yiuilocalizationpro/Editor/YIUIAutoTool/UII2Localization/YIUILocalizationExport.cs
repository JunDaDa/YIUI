#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using I2.Loc;
using UnityEngine;

namespace YIUIFramework.Editor
{
    /// <summary>
    /// 导出生成最后使用的本地化文件
    /// </summary>
    public static class YIUILocalizationExport
    {
        private static string GetSourceResPath()
        {
            var projPath = EditorHelper.GetProjPath(UII2LocalizationModule.UII2SourceResPath);
            var path = $"{projPath}/{I2LocalizeHelper.I2ResAssetNamePrefix}{UII2LocalizationModule.UII2SourceResName}.csv";
            return path;
        }

        public static bool ExportAllCsv(LanguageSourceData data)
        {
            var path = GetSourceResPath();

            try
            {
                var content = Export_CSV(data, null);
                var utf8 = new UTF8Encoding(false);
                File.WriteAllText(path, content, utf8);
            }
            catch (Exception e)
            {
                UnityTipsHelper.ShowError($"导出全数据时发生错误 请检查");
                Debug.LogError(e);
                return false;
            }

            Debug.Log($"多语言 全数据 {UII2LocalizationModule.UII2SourceResName} 导出CSV成功 {path}");

            var projPath = EditorHelper.GetProjPath(UII2LocalizationModule.UII2TargetLanguageResPath);
            if (!Directory.Exists(projPath))
            {
                Directory.CreateDirectory(projPath);
            }
            else
            {
                try
                {
                    foreach (var subDirectory in Directory.GetDirectories(projPath))
                    {
                        try
                        {
                            Directory.Delete(subDirectory, true);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"删除目录失败: {subDirectory}, {ex}");
                            return false;
                        }
                    }

                    foreach (var filePath in Directory.GetFiles(projPath))
                    {
                        try
                        {
                            if (!Path.GetExtension(filePath).Equals(".meta", StringComparison.OrdinalIgnoreCase))
                            {
                                File.Delete(filePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"删除文件时发生错误: {filePath}, {ex}");
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"删除文件时发生错误: {ex}");
                    return false;
                }
            }

            foreach (var languages in data.mLanguages)
            {
                var targetPath = "";

                try
                {
                    var content = Export_CSV(data, languages.Name);
                    targetPath = $"{projPath}/{I2LocalizeHelper.I2ResAssetNamePrefix}{languages.Name}.csv";
                    File.WriteAllText(targetPath, content, Encoding.UTF8);
                }
                catch (Exception e)
                {
                    UnityTipsHelper.ShowError($"导出指定数据时发生错误 {languages.Name} 请检查 ");
                    Debug.LogError(e);
                    return false;
                }

                Debug.Log($"多语言 指定数据 {languages.Name} 导出CSV成功 {targetPath}");
            }

            Debug.Log($"导出全数据完成 {path}");
            return true;
        }

        #region 导出方法

        private static string Export_CSV(LanguageSourceData data, string selectLanguage)
        {
            char Separator = ',';
            var Builder = new StringBuilder();

            var languages = data.mLanguages;
            var languagesCount = languages.Count;
            Builder.AppendFormat("Key{0}Type{0}Desc", Separator);
            var currentLanguageIndex = -1;

            for (int i = 0; i < languagesCount; i++)
            {
                var langData = languages[i];

                var currentLanguage = GoogleLanguages.GetCodedLanguage(langData.Name, langData.Code);

                if (!string.IsNullOrEmpty(selectLanguage) && currentLanguage != selectLanguage)
                {
                    continue;
                }

                Builder.Append(Separator);
                if (!langData.IsEnabled())
                    Builder.Append('$');
                AppendString(Builder, currentLanguage, Separator);
                currentLanguageIndex = i;
            }

            if (string.IsNullOrEmpty(selectLanguage))
            {
                currentLanguageIndex = -1;
            }

            Builder.Append("\n");

            var terms = data.mTerms;

            if (string.IsNullOrEmpty(selectLanguage))
            {
                terms.Sort((a, b) => string.CompareOrdinal(a.Term, b.Term));
            }

            foreach (var termData in terms)
            {
                var term = termData.Term;

                foreach (var specialization in termData.GetAllSpecializations())
                    AppendTerm(Builder, currentLanguageIndex, term, termData, specialization, Separator);
            }

            return Builder.ToString();
        }

        private static void AppendTerm(StringBuilder Builder,
                                       int selectLanguageIndex,
                                       string Term,
                                       TermData termData,
                                       string specialization,
                                       char Separator)
        {
            //--[ Key ] --------------
            AppendString(Builder, Term, Separator);

            if (!string.IsNullOrEmpty(specialization) && specialization != "Any")
                Builder.AppendFormat("[{0}]", specialization);

            //--[ Type and Description ] --------------
            Builder.Append(Separator);
            Builder.Append(termData.TermType.ToString());
            Builder.Append(Separator);
            AppendString(Builder, selectLanguageIndex <= -1 ? termData.Description : "", Separator);

            var startIndex = selectLanguageIndex <= -1 ? 0 : selectLanguageIndex;
            var maxIndex = selectLanguageIndex <= -1 ? termData.Languages.Length : selectLanguageIndex + 1;

            //--[ Languages ] --------------
            for (var i = startIndex; i < maxIndex; ++i)
            {
                Builder.Append(Separator);

                var translation = termData.Languages[i];
                if (!string.IsNullOrEmpty(specialization))
                    translation = termData.GetTranslation(i, specialization);

                AppendTranslation(Builder, translation, Separator, null);
            }

            Builder.Append("\n");
        }

        private static void AppendString(StringBuilder Builder, string Text, char Separator)
        {
            if (string.IsNullOrEmpty(Text))
                return;
            Text = Text.Replace("\\n", "\n");
            if (Text.IndexOfAny((Separator + "\n\"").ToCharArray()) >= 0)
            {
                Text = Text.Replace("\"", "\"\"");
                Builder.AppendFormat("\"{0}\"", Text);
            }
            else
            {
                Builder.Append(Text);
            }
        }

        private static void AppendTranslation(StringBuilder Builder, string Text, char Separator, string tags)
        {
            if (string.IsNullOrEmpty(Text))
                return;
            Text = Text.Replace("\\n", "\n");
            if (Text.IndexOfAny((Separator + "\n\"").ToCharArray()) >= 0)
            {
                Text = Text.Replace("\"", "\"\"");
                Builder.AppendFormat("\"{0}{1}\"", tags, Text);
            }
            else
            {
                Builder.Append(tags);
                Builder.Append(Text);
            }
        }

        #endregion
    }
}
#endif