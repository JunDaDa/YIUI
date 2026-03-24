set CONFIG_NAME=Config
set WORKSPACE=Packages\cn.etetet.yiuiluban
set WORKSPACEGEN=Packages\cn.etetet.yiuilubangen
set GEN_CLIENT=%WORKSPACE%\.Tools\Luban\Luban.dll
set CONF_ROOT=%WORKSPACEGEN%\Luban\%CONFIG_NAME%\Base
set CUSTOM=%WORKSPACE%\.ToolsGen\Custom

dotnet %GEN_CLIENT% ^
--customTemplateDir %CUSTOM% ^
-t all ^
-c cs-bin ^
-d bin ^
-d json ^
--conf %CONF_ROOT%\luban.conf ^
-x outputCodeDir=%WORKSPACEGEN%\CodeMode\Model\ClientServer\LubanGen\%CONFIG_NAME% ^
-x bin.outputDataDir=%WORKSPACEGEN%\Assets\LubanGen\%CONFIG_NAME%\Binary\ClientServer ^
-x json.outputDataDir=%WORKSPACEGEN%\Assets\LubanGen\%CONFIG_NAME%\Json\ClientServer
echo ==================== 所有 完成 ====================