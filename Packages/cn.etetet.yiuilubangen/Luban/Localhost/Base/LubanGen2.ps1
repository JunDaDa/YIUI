$PACKAGE = "Packages/cn.etetet.yiuilubangen"
$CONFIG_NAME = "Localhost"
$WORKSPACE = "Packages/cn.etetet.yiuiluban"
$GEN_CLIENT = "Packages/cn.etetet.yiuiluban/.Tools/Luban/Luban.dll"
$CUSTOM = "Packages/cn.etetet.yiuiluban/.ToolsGen/Custom"

$DotNet = "dotnet.exe"
if ($null -ne $IsMacOS) {
    $DotNet = "/usr/local/share/dotnet/dotnet"
}

& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t all -c cs-bin -d bin -d json --conf $PACKAGE/Luban/$CONFIG_NAME/Base/luban.conf -x outputCodeDir=$PACKAGE/CodeMode/Model/ClientServer/LubanGen/StartConfig -x bin.outputDataDir=$PACKAGE/Assets/LubanGen/StartConfig/$CONFIG_NAME/Binary/ClientServer/ -x json.outputDataDir=$PACKAGE/Assets/LubanGen/StartConfig/$CONFIG_NAME/Json/ClientServer/
Write-Host "==================== $CONFIG_NAME ClientServer完成 ===================="

exit $LASTEXITCODE