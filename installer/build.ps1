param([string]$InnoCompiler = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe")
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    if (!(Test-Path -LiteralPath $InnoCompiler)) { throw "Inno Setup 컴파일러를 찾을 수 없습니다: $InnoCompiler" }
    foreach ($project in @('App', 'Hook', 'Install')) {
        $parameters = @("CodexPet.$project\CodexPet.$project.csproj", '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-o', "artifacts\publish\$project", '-p:UseSharedCompilation=false')
        if ($project -eq 'Install') { $parameters += '-p:PublishSingleFile=true' }
        & dotnet publish @parameters
        if ($LASTEXITCODE -ne 0) { throw "CodexPet.$project 게시 실패: $LASTEXITCODE" }
    }
    & $InnoCompiler 'installer\CodexPet.iss'
    if ($LASTEXITCODE -ne 0) { throw "설치 파일 빌드 실패: $LASTEXITCODE" }
}
finally { Pop-Location }
