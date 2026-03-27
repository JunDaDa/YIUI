using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

[CustomEditor(typeof(CameraFollow))]
public class CameraFollowEditor : Editor
{
    private static readonly string s_ExcelPath =
        "Packages/cn.etetet.sjgameplay/Luban/Config/Datas/GlobalParam.xlsx";

    private static readonly string s_LubanDll =
        "Packages/cn.etetet.yiuiluban/.Tools/Luban/Luban.dll";

    private static readonly string s_CustomTemplateDir =
        "Packages/cn.etetet.yiuiluban/.ToolsGen/Custom";

    private static readonly string s_ConfRoot =
        "Packages/cn.etetet.yiuilubangen/Luban/Config/Base";

    private static readonly string s_TableFullName =
        "cn.etetet.sjgameplay.GlobalParamConfigCategory";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Save & Export", GUILayout.Height(30)))
        {
            if (SaveToExcel((CameraFollow)target))
            {
                ExportSingleTable();
            }
        }
    }

    private static bool SaveToExcel(CameraFollow cam)
    {
        string absPath = Path.GetFullPath(s_ExcelPath).Replace('\\', '/');
        if (!File.Exists(absPath))
        {
            Debug.LogError($"[CameraFollow] Excel not found: {absPath}");
            return false;
        }

        float fov = Camera.main != null ? Camera.main.fieldOfView : 60f;

        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("import openpyxl");
        sb.AppendLine($"wb = openpyxl.load_workbook(r'{absPath}')");
        sb.AppendLine("ws = wb.active");
        sb.AppendLine("# 按 Id 列(B) 查找行并更新 Value 列(C)");
        sb.AppendLine("params = {");
        sb.AppendLine($"  'CameraOffsetX': {cam.OffsetX.ToString(ci)},");
        sb.AppendLine($"  'CameraOffsetY': {cam.OffsetY.ToString(ci)},");
        sb.AppendLine($"  'CameraOffsetZ': {cam.OffsetZ.ToString(ci)},");
        sb.AppendLine($"  'CameraFOV': {fov.ToString(ci)},");
        sb.AppendLine($"  'CameraSmoothTime': {cam.SmoothTime.ToString(ci)},");
        sb.AppendLine($"  'CameraZoomSpeed': {cam.ZoomSpeed.ToString(ci)},");
        sb.AppendLine($"  'CameraMinZoom': {cam.MinZoom.ToString(ci)},");
        sb.AppendLine($"  'CameraMaxZoom': {cam.MaxZoom.ToString(ci)},");
        sb.AppendLine("}");
        sb.AppendLine("for row in ws.iter_rows(min_row=4, max_col=3):");
        sb.AppendLine("    key = row[1].value");
        sb.AppendLine("    if key in params:");
        sb.AppendLine("        row[2].value = params[key]");
        sb.AppendLine($"wb.save(r'{absPath}')");
        sb.AppendLine("print('OK')");

        string tempPy = Path.Combine(Path.GetTempPath(), "camera_save_excel.py");

        try
        {
            File.WriteAllText(tempPy, sb.ToString(), Encoding.UTF8);
            bool ok = RunProcess("python", $"\"{tempPy}\"", "OK");
            if (ok)
            {
                Debug.Log("[CameraFollow] Saved to Excel");
            }
            return ok;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CameraFollow] Python error: {e.Message}");
            return false;
        }
        finally
        {
            if (File.Exists(tempPy))
            {
                File.Delete(tempPy);
            }
        }
    }

    private static void ExportSingleTable()
    {
        string lubanDll = Path.GetFullPath(s_LubanDll).Replace('\\', '/');
        string customDir = Path.GetFullPath(s_CustomTemplateDir).Replace('\\', '/');
        string confPath = Path.GetFullPath(s_ConfRoot).Replace('\\', '/');
        string genRoot = Path.GetFullPath("Packages/cn.etetet.yiuilubangen").Replace('\\', '/');

        string[] targets = { "client", "clientserver", "server" };
        bool allOk = true;

        foreach (string target in targets)
        {
            string binDir = $"{genRoot}/Assets/LubanGen/Config/Binary/{CapFirst(target)}";
            string jsonDir = $"{genRoot}/Assets/LubanGen/Config/Json/{CapFirst(target)}";

            string args = $"\"{lubanDll}\" " +
                          $"--customTemplateDir \"{customDir}\" " +
                          $"-t {target} " +
                          $"-d bin -d json " +
                          $"--conf \"{confPath}/luban.conf\" " +
                          $"-x bin.outputDataDir=\"{binDir}\" " +
                          $"-x json.outputDataDir=\"{jsonDir}\" " +
                          $"-o {s_TableFullName}";

            if (!RunProcess("dotnet", args, null, 15000))
            {
                allOk = false;
                break;
            }
        }

        if (allOk)
        {
            AssetDatabase.Refresh();
            Debug.Log("[CameraFollow] Export done: GlobalParamConfigCategory");
        }
    }

    private static string CapFirst(string s)
    {
        return s switch
        {
            "client" => "Client",
            "server" => "Server",
            "clientserver" => "ClientServer",
            _ => s,
        };
    }

    private static bool RunProcess(string exe, string arguments, string successMarker, int timeoutMs = 5000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        try
        {
            using var process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit(timeoutMs);

            if (!string.IsNullOrEmpty(error) && error.Contains("ERROR"))
            {
                Debug.LogError($"[CameraFollow] {exe} error: {error}");
                return false;
            }

            if (successMarker != null && !output.Contains(successMarker))
            {
                Debug.LogError($"[CameraFollow] {exe} failed: {output}\n{error}");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CameraFollow] Process error: {e.Message}");
            return false;
        }
    }
}
