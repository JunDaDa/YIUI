@echo off
setlocal

set "currentDir=%~dp0"

if exist "%currentDir%cn.etetet.yiuinumericconfig" (
    rmdir /s /q "%currentDir%cn.etetet.yiuinumericconfig"
)

set "targetDir=%currentDir%..\..\cn.etetet.yiuinumericconfig"

if exist "%targetDir%" (
    xcopy "%targetDir%" "%currentDir%cn.etetet.yiuinumericconfig" /e /i /y
)

if exist "%currentDir%cn.etetet.yiuinumericconfig\*.meta" (
    del /s /q "%currentDir%cn.etetet.yiuinumericconfig\*.meta"
)

echo 操作完成
pause