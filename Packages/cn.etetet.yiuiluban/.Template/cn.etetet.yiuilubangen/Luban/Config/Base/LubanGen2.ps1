$PACKAGE = "Packages/cn.etetet.yiuilubangen"
$CONFIG_NAME = "Config"
$WORKSPACE = "Packages/cn.etetet.yiuiluban"
$GEN_CLIENT = "Packages/cn.etetet.yiuiluban/.Tools/Luban/Luban.dll"
$CUSTOM = "Packages/cn.etetet.yiuiluban/.ToolsGen/Custom"

$DotNet = "dotnet.exe"
if ($null -ne $IsMacOS) {
    $DotNet = "/usr/local/share/dotnet/dotnet"
}

& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t server -c cs-bin -d bin -d json --conf $PACKAGE/Luban/$CONFIG_NAME/Base/luban.conf -x outputCodeDir=$PACKAGE/CodeMode/Model/Server/LubanGen/$CONFIG_NAME/ -x bin.outputDataDir=$PACKAGE/Assets/LubanGen/$CONFIG_NAME/Binary/Server/ -x json.outputDataDir=$PACKAGE/Assets/LubanGen/$CONFIG_NAME/Json/Server/
Write-Host "==================== 服务器 完成 ===================="

exit $LASTEXITCODE