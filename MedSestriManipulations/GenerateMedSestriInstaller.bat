@echo off 
dotnet publish -f:net9.0-android -c Release -p:ApplicationTitle="MedSestri" -o ../MedSestri
pause