RD /q /s BlazorDiffusion\App_Files\artifacts
XCOPY /Y /E /H /C /I ..\BlazorDiffusionAssets\artifacts BlazorDiffusion\App_Files\artifacts

IF EXIST BlazorDiffusion\App_Data\db.sqlite DEL BlazorDiffusion\App_Data\db.sqlite

PUSHD BlazorDiffusion
dotnet run BlazorDiffusion.csproj --AppTasks=migrate
POPD
