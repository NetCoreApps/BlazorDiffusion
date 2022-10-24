if not exist BlazorDiffusion\App_Files\artifacts md BlazorDiffusion\App_Files\artifacts

IF EXIST BlazorDiffusion\App_Data\db.sqlite DEL BlazorDiffusion\App_Data\db.sqlite
aws s3 sync s3://diffusion/artifacts BlazorDiffusion\App_Files\artifacts --profile r2 --endpoint-url https://b95f38ca3a6ac31ea582cd624e6eb385.r2.cloudflarestorage.com --size-only
PUSHD BlazorDiffusion
dotnet run BlazorDiffusion.csproj --AppTasks=migrate
POPD
