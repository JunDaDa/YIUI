using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace YIUI.Luban.Editor
{
    public partial class LubanTools
    {
        [MenuItem("ET/Excel/ExcelExporter")]
        public static void MenuLubanGen()
        {
            LubanGen();
        }

        public static bool LubanGen()
        {
            ClearAll();

            return CreateLubanConf();
        }

        private class SchemaFile
        {
            public string fileName { get; set; }
            public string type { get; set; }
        }

        private static bool CreateLubanConf()
        {
            var m_LubanInfos = new Dictionary<string, List<string>>();
            var m_LubanConfigs = new Dictionary<string, string>();

            foreach (string packageDir in Directory.GetDirectories("Packages", "cn.etetet.*"))
            {
                var d = Path.Combine(packageDir, "Luban");
                if (!Directory.Exists(d))
                {
                    continue;
                }

                foreach (string directory in Directory.GetDirectories(d))
                {
                    var basePath = Path.Combine(directory, "Base");
                    var datasPath = Path.Combine(directory, "Datas");

                    if (!Directory.Exists(basePath) || !Directory.Exists(datasPath))
                    {
                        continue;
                    }

                    var configCollectionName = Path.GetFileName(directory);

                    var lubanConfigPath = Path.Combine(directory, "Base/luban.conf");
                    if (File.Exists(lubanConfigPath))
                    {
                        if (m_LubanConfigs.TryGetValue(configCollectionName, out string config))
                        {
                            Debug.LogError($"{configCollectionName}, 已经存在这个分类的luban.conf 一种类型只允许存在一个, {config}");
                            continue;
                        }

                        m_LubanConfigs.Add(configCollectionName, lubanConfigPath);
                    }

                    if (!m_LubanInfos.TryGetValue(configCollectionName, out var lubanInfos))
                    {
                        lubanInfos = new();
                        m_LubanInfos.Add(configCollectionName, lubanInfos);
                    }

                    lubanInfos.Add(directory);
                }
            }

            var allSchemaFiles = new Dictionary<string, List<SchemaFile>>();

            foreach ((var configCollectionsName, var lubanInfo) in m_LubanInfos)
            {
                if (!allSchemaFiles.TryGetValue(configCollectionsName, out var schemaFiles))
                {
                    schemaFiles = new();
                    allSchemaFiles.Add(configCollectionsName, schemaFiles);
                }

                foreach (var configPath in lubanInfo)
                {
                    var tablesPath = Path.Combine(configPath, "Base/__tables__.xlsx").Replace('\\', '/');
                    if (File.Exists(tablesPath))
                    {
                        schemaFiles.Add(new() { fileName = $"../../../../../{tablesPath}", type = "table" });
                    }

                    var beansPath = Path.Combine(configPath, "Base/__beans__.xlsx").Replace('\\', '/');
                    if (File.Exists(beansPath))
                    {
                        schemaFiles.Add(new() { fileName = $"../../../../../{beansPath}", type = "bean" });
                    }

                    var enumsPath = Path.Combine(configPath, "Base/__enums__.xlsx").Replace('\\', '/');
                    if (File.Exists(enumsPath))
                    {
                        schemaFiles.Add(new() { fileName = $"../../../../../{enumsPath}", type = "enum" });
                    }

                    var definesPath = Path.Combine(configPath, "Base/Defines").Replace('\\', '/');
                    if (Directory.Exists(definesPath))
                    {
                        schemaFiles.Add(new() { fileName = $"../../../../../{definesPath}", type = "" });
                    }
                }
            }

            var succeed = false;

            try
            {
                CreateLubanBefore();

                EditorUtility.DisplayProgressBar("Luban", "导出Luban配置中...", 0);

                var tasks = new List<Task<bool>>();

                foreach ((var configCollectionsName, var schemaFiles) in allSchemaFiles)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        if (!m_LubanConfigs.TryGetValue(configCollectionsName, out var lubanConfigPath))
                        {
                            Debug.LogError($"类型{configCollectionsName} 源文件 luban.conf 不存在");
                            return false;
                        }

                        if (!File.Exists(lubanConfigPath))
                        {
                            Debug.LogError($"类型{configCollectionsName} 源文件 luban.conf 不存在");
                            return false;
                        }

                        try
                        {
                            string fileContent = File.ReadAllText(lubanConfigPath);

                            JObject json = JObject.Parse(fileContent);

                            JArray schemaFilesArray = JArray.FromObject(schemaFiles);

                            json["schemaFiles"] = schemaFilesArray;

                            string updatedJsonContent = json.ToString(Newtonsoft.Json.Formatting.Indented);

                            File.WriteAllText(lubanConfigPath, updatedJsonContent, Encoding.UTF8);

                            //Debug.Log($"开始导出 {configCollectionsName}");

                            var result = RunLubanGen($"{lubanConfigPath}/../");
                            if (!result)
                            {
                                return false;
                            }
                            else
                            {
                                //Debug.Log($"Luban导出完成,{lubanConfigPath}");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"创建 LubanConf失败 {configCollectionsName} {e.Message}");
                            return false;
                        }

                        return true;
                    }));
                }

                Task.WaitAll(tasks.ToArray());

                succeed = tasks.All(t => t.Result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                if (succeed)
                {
                    CreateLubanAfterSucceed();
                    AssetDatabase.SaveAssets();
                    EditorApplication.ExecuteMenuItem("Assets/Refresh");
                    CloseWindowRefresh?.Invoke();
                    Debug.Log($"Luban 导出成功");
                }
                else
                {
                    CreateLubanAfterFailed();
                    CloseWindow?.Invoke();
                    Debug.LogError($"Luban 导出失败");
                }
            }

            return true;
        }

        private static bool m_UsePs = true; // 是否使用PowerShell
        private static bool m_ToUnixEOL = false; // 是否转换为Unix换行符

        private static void ConvertLineEndings(string filePath, string newLine)
        {
            string fileContent = File.ReadAllText(filePath);
            string updatedContent = fileContent.Replace("\r\n", "\n").Replace("\n", newLine);
            File.WriteAllText(filePath, updatedContent);
        }

        private static bool RunLubanGen(string configCollectionPath, bool tips = false)
        {
            var scriptDir = Path.GetDirectoryName(configCollectionPath);

            var filePattern = m_UsePs ? "*.ps1" : "*.bat";
            var foundScripts = Directory.GetFiles(scriptDir, filePattern);

            if (foundScripts.Length == 0)
            {
                Debug.LogError($"找不到Luban脚本: 在目录 {scriptDir} 中找不到任何 {filePattern} 文件");
                return false;
            }

            if (foundScripts.Length > 1)
            {
                var tasks = new List<Task<bool>>();
                foreach (var script in foundScripts)
                {
                    tasks.Add(Task.Run(() => ExecuteSingleScript(script, tips)));
                }

                Task.WaitAll(tasks.ToArray());
                return tasks.All(t => t.Result);
            }

            return ExecuteSingleScript(foundScripts[0], tips);
        }

        private static bool ExecuteSingleScript(string scriptPath, bool tips)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (m_UsePs)
                {
                    return RunProcess("powershell.exe", $"-ExecutionPolicy Bypass -File \"{scriptPath}\"", tips);
                }
                else
                {
                    ConvertLineEndings(scriptPath, "\r\n");
                    return RunProcess(scriptPath, "", tips);
                }
            }
            else
            {
                if (m_UsePs)
                {
                    return RunProcess("/usr/local/bin/pwsh", $"-ExecutionPolicy Bypass -File \"{scriptPath}\"", tips);
                }
                else
                {
                    ChangePermissions(scriptPath, "755");
                    return RunProcess("/bin/bash", $"-c \"{scriptPath}\"", tips);
                }
            }
        }

        private static void ChangePermissions(string filePath, string permissions)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "/bin/chmod",
                    Arguments = $"{permissions} \"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();

                    // You can handle the output if needed  
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to change file permissions: {e}");
            }
        }

        private static bool RunProcess(string exe, string arguments, bool tips = false, string workingDirectory = ".", bool waitExit = true)
        {
            var redirectStandardOutput = false;
            var redirectStandardError = false;
            var useShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (waitExit)
            {
                redirectStandardOutput = true;
                redirectStandardError = true;
                useShellExecute = false;
            }

            var ImportAllOutput = new StringBuilder();
            var ImportAllError = new StringBuilder();
            var importProcess = new Process();
            importProcess.StartInfo.FileName = exe;
            importProcess.StartInfo.Arguments = arguments;
            importProcess.StartInfo.WorkingDirectory = workingDirectory;
            importProcess.StartInfo.UseShellExecute = useShellExecute;
            importProcess.StartInfo.CreateNoWindow = true;
            importProcess.StartInfo.RedirectStandardOutput = redirectStandardOutput;
            importProcess.StartInfo.RedirectStandardError = redirectStandardError;
            importProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            importProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;

            importProcess.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    if (args.Data.Contains("ERROR"))
                    {
                        ImportAllError.AppendLine(args.Data);
                    }

                    ImportAllOutput.AppendLine(args.Data);
                }
            };

            importProcess.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    ImportAllError.AppendLine(args.Data);
                }
            };

            var succeed = false;

            try
            {
                importProcess.Start();
                importProcess.BeginOutputReadLine();
                importProcess.BeginErrorReadLine();
                var exited = importProcess.WaitForExit(20000);
                if (!exited)
                {
                    Debug.Log($"Luban导出超时，终止进程 请检查原因是 真是太大 还是有问题!! 如果是真的太大就修改这个超时时间");
                    importProcess.Kill();
                    succeed = false;
                }
                else
                {
                    importProcess.WaitForExit();

                    var output = ImportAllOutput.ToString();
                    if (!string.IsNullOrEmpty(output))
                    {
                        //Debug.Log($"Luban导出日志:\n{output}");
                    }

                    var error = ImportAllError.ToString();
                    succeed = string.IsNullOrEmpty(error);
                    if (!succeed)
                    {
                        Debug.Log($"Luban导出日志:\n{output}");

                        if (tips)
                        {
                            UnityTipsHelper.ShowError($"Luban导出错误:\n{error}");
                        }
                        else
                        {
                            Debug.LogError($"Luban导出错误:\n{error}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Luban导出执行报错: {e}");
                return false;
            }
            finally
            {
                importProcess.Close();
            }

            return succeed;
        }

        private static List<string> GetLubanCodeModePath()
        {
            var allPath = new List<string>();

            foreach (string directory in Directory.GetDirectories("Packages", "cn.etetet.*"))
            {
                var cPath = Path.Combine(directory, "CodeMode/Model/Client/LubanGen");
                if (Directory.Exists(cPath))
                {
                    allPath.Add(cPath);
                }

                var csPath = Path.Combine(directory, "CodeMode/Model/ClientServer/LubanGen");
                if (Directory.Exists(csPath))
                {
                    allPath.Add(csPath);
                }

                var sPath = Path.Combine(directory, "CodeMode/Model/Server/LubanGen");
                if (Directory.Exists(sPath))
                {
                    allPath.Add(sPath);
                }
            }

            return allPath;
        }

        //执行前
        private static void CreateLubanBefore()
        {
            if (Directory.Exists(BackupRootPath))
            {
                Directory.Delete(BackupRootPath, true);
            }

            Directory.CreateDirectory(BackupRootPath);

            foreach (var path in GetLubanCodeModePath())
            {
                foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
                {
                    if (Path.GetExtension(file).Equals(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var relativePath = Path.GetRelativePath(path, file);
                            var backupPath = Path.Combine(BackupRootPath, path, relativePath);
                            Directory.CreateDirectory(Path.GetDirectoryName(backupPath) ?? throw new InvalidOperationException());
                            File.Copy(file, backupPath, true);
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Backup failed: {file}\n{ex}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 转换单个文件的换行符为Unix格式 (LF)
        /// </summary>
        private static void ConvertFileToUnixEOL(string filePath)
        {
            try
            {
                // 读取文件内容
                var content = File.ReadAllText(filePath);
                bool modified = false;

                // 检查是否包含 Windows 换行符 (CRLF)
                if (content.Contains("\r\n"))
                {
                    // 转换为 Unix 换行符 (LF)
                    content = content.Replace("\r\n", "\n");
                    modified = true;
                }

                // 检查是否包含旧 Mac 换行符 (CR)
                else if (content.Contains("\r") && !content.Contains("\n"))
                {
                    // 转换为 Unix 换行符 (LF)
                    content = content.Replace("\r", "\n");
                    modified = true;
                }

                // 如果有修改，写回文件
                if (modified)
                {
                    File.WriteAllText(filePath, content);
                    var fileType = Path.GetExtension(filePath).ToLower();
                    Debug.Log($"Converted {fileType} EOL to Unix format: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to convert EOL for file: {filePath}\n{ex}");
            }
        }

        //执行后 成功
        private static void CreateLubanAfterSucceed()
        {
            foreach (var path in GetLubanCodeModePath())
            {
                var directories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories).ToList();
                directories.Add(path);

                foreach (var dir in directories.OrderByDescending(d => d.Length))
                {
                    if (!Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories).Any())
                    {
                        var parentDir = Path.GetDirectoryName(dir);
                        if (!string.IsNullOrEmpty(parentDir))
                        {
                            var metaFileName = Path.GetFileName(dir) + ".meta";
                            var metaFilePath = Path.Combine(parentDir, metaFileName);

                            if (File.Exists(metaFilePath))
                            {
                                try
                                {
                                    File.Delete(metaFilePath);
                                }
                                catch
                                {
                                    /* 可添加日志 */
                                }
                            }
                        }

                        try
                        {
                            Directory.Delete(dir, true);
                        }
                        catch
                        {
                            /* 可添加日志记录 */
                        }
                    }
                }
            }

            if (m_ToUnixEOL)
            {
                // 遍历所有生成的 .cs 文件，统一转换为 Unix 换行符 (LF)
                foreach (var path in GetLubanCodeModePath())
                {
                    foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
                    {
                        ConvertFileToUnixEOL(file);
                    }
                }

                // 处理生成的 JSON 文件，统一转换为 Unix 换行符 (LF)
                var jsonConfigPath = "Packages/cn.etetet.yiuilubangen/Assets/LubanGen/Config/Json";
                if (Directory.Exists(jsonConfigPath))
                {
                    foreach (var file in Directory.EnumerateFiles(jsonConfigPath, "*.json", SearchOption.AllDirectories))
                    {
                        ConvertFileToUnixEOL(file);
                    }
                }

                // 处理所有的 luban.conf 文件，统一转换为 Unix 换行符 (LF)
                foreach (string packageDir in Directory.GetDirectories("Packages", "cn.etetet.*"))
                {
                    var lubanDir = Path.Combine(packageDir, "Luban");
                    if (!Directory.Exists(lubanDir))
                    {
                        continue;
                    }

                    foreach (var confFile in Directory.EnumerateFiles(lubanDir, "luban.conf", SearchOption.AllDirectories))
                    {
                        ConvertFileToUnixEOL(confFile);
                    }
                }
            }

            if (Directory.Exists(BackupRootPath))
            {
                Directory.Delete(BackupRootPath, true);
            }
        }

        //临时备份目录
        private static readonly string BackupRootPath = Path.Combine(Path.GetTempPath(), "LubanBackup");

        //执行后 失败
        private static void CreateLubanAfterFailed()
        {
            if (!Directory.Exists(BackupRootPath)) return;

            foreach (var codeModePath in GetLubanCodeModePath())
            {
                var backupCodePath = Path.Combine(BackupRootPath, codeModePath);
                if (!Directory.Exists(backupCodePath)) continue;

                foreach (var backupFile in Directory.EnumerateFiles(backupCodePath, "*.cs", SearchOption.AllDirectories))
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(backupCodePath, backupFile);
                        var originalPath = Path.Combine(codeModePath, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(originalPath) ?? throw new InvalidOperationException());
                        File.Copy(backupFile, originalPath, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Restore failed: {backupFile}\n{ex}");
                    }
                }
            }

            Directory.Delete(BackupRootPath, true);
        }

        private static void ClearAll()
        {
            try
            {
                var modelAssembly = Assembly.Load("ET.Model");
                if (modelAssembly == null)
                {
                    //throw new Exception("没有找到 ET.Model 程序集");
                    return;
                }

                var type = modelAssembly.GetType("ET.LubanEditorConfigCategory");
                if (type == null)
                {
                    //throw new Exception("找不到 ET.LubanEditorConfigCategory 类型");
                    return;
                }

                var clearAllMethod = type.GetMethod("ClearAll", BindingFlags.Public | BindingFlags.Static);
                if (clearAllMethod == null)
                {
                    //throw new Exception("找不到 ClearAll 方法");
                    return;
                }

                clearAllMethod.Invoke(null, null);
            }
            catch
            {
                //throw;
            }
        }
    }
}