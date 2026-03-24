$PACKAGE = "Packages/cn.etetet.yiuilubangen"
$CONFIG_NAME = "Localhost"
$WORKSPACE = "Packages/cn.etetet.yiuiluban"
$GEN_CLIENT = "Packages/cn.etetet.yiuiluban/.Tools/Luban/Luban.dll"
$CUSTOM = "Packages/cn.etetet.yiuiluban/.ToolsGen/Custom"

$DotNet = "dotnet.exe"
if ($null -ne $IsMacOS) {
    $DotNet = "/usr/local/share/dotnet/dotnet"
}

& $DotNet $GEN_CLIENT --customTemplateDir $CUSTOM -t all -c cs-bin -d bin -d json --conf $PACKAGE/Luban/$CONFIG_NAME/Base/luban.conf -x outputCodeDir=$PACKAGE/CodeMode/Model/Server/LubanGen/StartConfig -x bin.outputDataDir=$PACKAGE/Assets/LubanGen/StartConfig/$CONFIG_NAME/Binary/Server/ -x json.outputDataDir=$PACKAGE/Assets/LubanGen/StartConfig/$CONFIG_NAME/Json/Server/
Write-Host "==================== $CONFIG_NAME Server完成 ===================="

exit $LASTEXITCODE