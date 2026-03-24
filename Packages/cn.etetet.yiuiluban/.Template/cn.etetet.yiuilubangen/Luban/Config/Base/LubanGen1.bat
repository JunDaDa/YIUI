set CONFIG_NAME=Config
set WORKSPACE=Packages\cn.etetet.yiuiluban
set WORKSPACEGEN=Packages\cn.etetet.yiuilubangen
set GEN_CLIENT=%WORKSPACE%\.Tools\Luban\Luban.dll
set CONF_ROOT=%WORKSPACEGEN%\Luban\%CONFIG_NAME%\Base
set CUSTOM=%WORKSPACE%\.ToolsGen\Custom

dotnet %GEN_CLIENT% ^
--customTemplateDir %CUSTOM% ^
-t client ^
-c cs-bin ^
-d bin ^
-d json ^
--conf %CONF_ROOT%\luban.conf ^
-x outputCodeDir=%WORKSPACEGEN%\CodeMode\Model\Client\LubanGen\%CONFIG_NAME% ^
-x bin.outputDataDir=%WORKSPACEGEN%\Assets\LubanGen\%CONFIG_NAME%\Binary\Client ^
-x json.outputDataDir=%WORKSPACEGEN%\Assets\LubanGen\%CONFIG_NAME%\Json\Client
echo ==================== 客户端 完成 ====================