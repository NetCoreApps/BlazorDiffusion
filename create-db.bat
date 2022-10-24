RD /q /s BlazorDiffusion\App_Files\artifacts
if not exist BlazorDiffusion\App_Files\artifacts md BlazorDiffusion\App_Files\artifacts

IF EXIST BlazorDiffusion\App_Data\db.sqlite DEL BlazorDiffusion\App_Data\db.sqlite
aws s3 sync s3://diffusion/artifacts BlazorDiffusion\App_Files\artifacts --profile r2
PUSHD BlazorDiffusion
dotnet run BlazorDiffusion.csproj --AppTasks=migrate
POPD
