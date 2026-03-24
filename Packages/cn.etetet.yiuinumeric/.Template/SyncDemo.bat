@echo off
setlocal

set "currentDir=%~dp0"

if exist "%currentDir%cn.etetet.yiuinumericdemo" (
    rmdir /s /q "%currentDir%cn.etetet.yiuinumericdemo"
)

set "targetDir=%currentDir%..\..\cn.etetet.yiuinumericdemo"

if exist "%targetDir%" (
    xcopy "%targetDir%" "%currentDir%cn.etetet.yiuinumericdemo" /e /i /y
)

if exist "%currentDir%cn.etetet.yiuinumericdemo\*.meta" (
    del /s /q "%currentDir%cn.etetet.yiuinumericdemo\*.meta"
)

echo 操作完成
pause