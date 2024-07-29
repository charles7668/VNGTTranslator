dotnet publish .\VNGTTranslator\VNGTTranslator.csproj -o "./bin/VNGTTranslator x86" -c Release -a x86
dotnet publish .\VNGTTranslator\VNGTTranslator.csproj -o "./bin/VNGTTranslator x64" -c Release -a x64
del ".\bin\VNGTTranslator x86\*.pdb"
del ".\bin\VNGTTranslator x64\*.pdb"