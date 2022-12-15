@ECHO OFF
rmdir "Publish" /S /Q
dotnet build Sicoob.Visualizer.Monitor --output Publish --configuration Release
dotnet build Sicoob.Visualizer.Conector --output Publish\Conector --configuration Release