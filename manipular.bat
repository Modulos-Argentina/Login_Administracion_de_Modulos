@echo off
title Sistema de Control de Modulos - Git
color 0A

:menu
cls
:: 1. Obtener la rama personal registrada en el archivo local
if exist .mi_rama.txt (
    set /p MI_RAMA=<.mi_rama.txt
) else (
    set MI_RAMA=No asignada
)

:: 2. Consultar a Git en vivo en que rama esta parado en este segundo
set RAMA_ACTUAL=Desconocida
for /f "tokens=*" %%i in ('git branch --show-current') do set RAMA_ACTUAL=%%i

:: 3. Generar estado visual
set ESTADO_UBICACION=[ EN TU RAMA ]
if /I "%RAMA_ACTUAL%"=="master" (
    set ESTADO_UBICACION=[ ATENCION: EN MASTER ]
) else if /I NOT "%RAMA_ACTUAL%"=="%MI_RAMA%" (
    set ESTADO_UBICACION=[ EN OTRA RAMA ]
)

echo ===============================================================
echo              PANEL DE TRABAJO - CONTROL DE MODULOS
echo ===============================================================
echo  Tu rama personal asignada : [%MI_RAMA%]
echo  ESTAS PARADO ACTUALMENTE  : [%RAMA_ACTUAL%] %ESTADO_UBICACION%
echo ===============================================================
echo 1. Actualizar proyecto (Bajar cambios recientes de master)
echo 2. Registrar o crear mi rama (Ejemplo: modulo08)
echo 3. Ir a mi rama de trabajo (%MI_RAMA%)
echo 4. CONSULTAR EN QUE RAMA ESTOY (Ver estado detallado)
echo 5. Ver los archivos que he modificado
echo 6. Subir los cambios de mi rama al servidor
echo 7. Cambiar a otra rama (Ver ramas de companeros)
echo 8. Salir
echo ===============================================================
set /p opcion="Selecciona una opcion (1-8): "

if "%opcion%"=="1" goto actualizar
if "%opcion%"=="2" goto registrar_rama
if "%opcion%"=="3" goto ir_mi_rama
if "%opcion%"=="4" goto donde_estoy
if "%opcion%"=="5" goto ver_estado
if "%opcion%"=="6" goto subir_cambios
if "%opcion%"=="7" goto cambiar_otra_rama
if "%opcion%"=="8" goto salir

echo Opcion no valida. Intenta de nuevo.
pause
goto menu

:donde_estoy
echo.
echo ***************************************************************
echo             DIAGNOSTICO DE UBICACION ACTUAL
echo ***************************************************************
echo Rama registrada para ti : %MI_RAMA%
echo Rama donde te encuentras: %RAMA_ACTUAL%
echo ***************************************************************
echo.
if /I "%RAMA_ACTUAL%"=="master" (
    echo [ PELIGRO ]
    echo Te encuentras en la rama principal MASTER.
    echo NO debes hacer cambios aqui.
    echo Te sugerimos usar la opcion 3 del menu para ir a tu rama.
) else if /I "%RAMA_ACTUAL%"=="%MI_RAMA%" (
    echo [ PERFECTO ]
    echo Estas en tu propia rama de trabajo (%MI_RAMA%).
    echo Puedes programar, guardar y subir con total tranquilidad.
) else (
    echo [ AVISO ]
    echo No estas en tu rama personal ni en master.
    echo Te encuentras en la rama de un companero o en una rama secundaria.
    echo Si terminaste de ayudar, usa la opcion 3 para volver a tu rama.
)
echo.
pause
goto menu

:actualizar
echo.
echo ---------------------------------------------------------------
echo [ ACTUALIZACION ]
echo ---------------------------------------------------------------
echo Descargando cambios del proyecto principal (master)...
git checkout master
git pull origin master
echo.
echo [ OK ] Proyecto principal actualizado correctamente.
pause
goto menu

:registrar_rama
echo.
echo ---------------------------------------------------------------
echo [ REGISTRO DE RAMA PERSONAL ]
echo ---------------------------------------------------------------
echo Escribe el identificador de tu modulo.
echo Formato sugerido: modulo seguido de tu numero (Ejemplo: modulo08)
echo.
set /p nom_rama="Nombre de tu rama: "
if "%nom_rama%"=="" goto menu

git checkout -b %nom_rama% 2>nul
if errorlevel 1 (
    git checkout %nom_rama%
    echo.
    echo [ INFO ] La rama ya existia. Se cambio a ella.
) else (
    echo.
    echo [ OK ] Rama "%nom_rama%" creada exitosamente.
)

echo %nom_rama%>.mi_rama.txt
echo [ OK ] Rama "%nom_rama%" registrada como tu espacio de trabajo.
pause
goto menu

:ir_mi_rama
echo.
echo ---------------------------------------------------------------
echo [ ACCEDIENDO A TU ESPACIO DE TRABAJO ]
echo ---------------------------------------------------------------
if not exist .mi_rama.txt (
    echo [ ALERTA ] Todavia no has registrado tu rama personal.
    echo Por favor usa primero la opcion 2 del menu.
    pause
    goto menu
)
set /p MI_RAMA=<.mi_rama.txt
git checkout %MI_RAMA%
echo.
echo [ OK ] Ya estas ubicado en tu rama: %MI_RAMA%
pause
goto menu

:ver_estado
echo.
echo ---------------------------------------------------------------
echo [ REVISION DE ARCHIVOS MODIFICADOS ]
echo ---------------------------------------------------------------
git status
echo.
pause
goto menu

:subir_cambios
echo.
echo ---------------------------------------------------------------
echo [ INSPECCION DEL GUARDIAN ANTES DE SUBIR ]
echo ---------------------------------------------------------------

:: Validar que no este intentando subir directamente desde master
for /f "tokens=*" %%i in ('git branch --show-current') do set RAMA_ACTUAL=%%i
if /I "%RAMA_ACTUAL%"=="master" (
    echo ===============================================================
    echo [ ACCESO DENEGADO POR EL GUARDIAN ]
    echo ===============================================================
    echo Estas ubicado en la rama MASTER.
    echo Esta estrictamente prohibido subir cambios directos a master.
    echo Debes trabajar dentro de tu rama asignada (ejemplo: modulo08).
    echo Usa la opcion 3 para ir a tu rama antes de subir.
    echo ===============================================================
    pause
    goto menu
)

:: Validar si modifico archivos criticos del nucleo
git status --porcelain | findstr /I "Program.cs appsettings.json Controlador Seguridad Vista \.github" > .archivos_bloqueados.tmp

for %%A in (.archivos_bloqueados.tmp) do if %%~zA GTR 0 (
    echo ===============================================================
    echo [ BLOQUEADO POR EL GUARDIAN ]
    echo ===============================================================
    echo Se detecto que modificaste archivos que NO corresponden a tu modulo:
    echo.
    type .archivos_bloqueados.tmp
    del .archivos_bloqueados.tmp
    echo.
    echo REGLA DEL SISTEMA:
    echo No tienes permisos para alterar el nucleo del sistema
    echo (Program.cs, appsettings.json, Seguridad, Vista, Controlador, etc.).
    echo.
    echo La subida ha sido cancelada para proteger el proyecto.
    echo Revierte esos cambios antes de intentar subir de nuevo.
    echo ===============================================================
    pause
    goto menu
)
if exist .archivos_bloqueados.tmp del .archivos_bloqueados.tmp

:: Mostrar cambios permitidos y pedir confirmacion
echo Archivos listos para enviar de tu modulo (%RAMA_ACTUAL%):
git status -s
echo.
set /p confirmacion="¿Confirmas que deseas enviar estos cambios al servidor? (S/N): "
if /I "%confirmacion%" NEQ "S" (
    echo [ INFO ] Envio cancelado por el usuario.
    pause
    goto menu
)

echo.
set /p mensaje="Escribe una descripcion breve del cambio realizado: "
git add .
git commit -m "%mensaje%"
git push origin HEAD
echo.
echo ===============================================================
echo [ ENVIO EXITOSO ]
echo ===============================================================
echo Tus cambios ya estan en el servidor en la rama: %RAMA_ACTUAL%
echo Ahora debes ir a GitHub y solicitar la union (Pull Request) hacia master.
echo El Administrador revisara y autorizara la fusion.
echo ===============================================================
pause
goto menu

:cambiar_otra_rama
echo.
echo ---------------------------------------------------------------
echo [ EXPLORADOR DE RAMAS DEL EQUIPO ]
echo ---------------------------------------------------------------
git fetch
echo.
git branch -a
echo ---------------------------------------------------------------
echo.
set /p nom_otra="Escribe el nombre de la rama a la que deseas ir: "
git checkout %nom_otra%
echo.
echo [ OK ] Te has movido a la rama: %nom_otra%
pause
goto menu

:salir
exit