param([string]$InnoCompiler = "${env:ProgramFiles}\Inno Setup 7\ISCC.exe")
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    if (!(Test-Path -LiteralPath $InnoCompiler)) { throw "Inno Setup 컴파일러를 찾을 수 없습니다: $InnoCompiler" }
    $InnoCompiler = (Resolve-Path -LiteralPath $InnoCompiler).Path
    $version = (Get-Content -Raw -Encoding UTF8 'version.txt').Trim()
    $buildRoot = Join-Path $root ('artifacts\builds\' + [Guid]::NewGuid().ToString('N'))
    $publishRoot = Join-Path $buildRoot 'publish'
    $setupRoot = Join-Path $buildRoot 'setup'
    New-Item -ItemType Directory -Force -Path $setupRoot | Out-Null
    Write-Output "빌드 폴더: $buildRoot"
    Write-Output "제품 버전: $version"
    & dotnet --version
    if ($LASTEXITCODE -ne 0) { throw 'SDK 버전을 확인하지 못했습니다.' }
    & $InnoCompiler --version
    if ($LASTEXITCODE -ne 0) { throw 'Inno Setup 버전을 확인하지 못했습니다.' }
    foreach ($project in @('App', 'Hook', 'Install')) {
        $parameters = @("CodexPet.$project\CodexPet.$project.csproj", '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-o', (Join-Path $publishRoot $project), '-p:UseSharedCompilation=false', '-p:DebugType=None', '-p:DebugSymbols=false')
        if ($project -eq 'Install') { $parameters += '-p:PublishSingleFile=true' }
        & dotnet publish @parameters
        if ($LASTEXITCODE -ne 0) { throw "CodexPet.$project 게시 실패: $LASTEXITCODE. 이전 결과물은 이번 빌드 결과가 아닙니다." }
        $exe = Join-Path (Join-Path $publishRoot $project) "CodexPet.$project.exe"
        if ([Diagnostics.FileVersionInfo]::GetVersionInfo($exe).ProductVersion -ne $version) { throw "제품 버전 불일치: $exe" }
    }
    $licenses = Join-Path $publishRoot 'Licenses'
    New-Item -ItemType Directory -Force -Path $licenses | Out-Null
    Copy-Item -LiteralPath 'LICENSE' -Destination (Join-Path $licenses 'CodexPet-MIT.txt')
    Copy-Item -LiteralPath 'licenses\Apache-2.0.txt' -Destination (Join-Path $licenses 'XamlAnimatedGif-Apache-2.0.txt')
    Copy-Item -LiteralPath 'licenses\THIRD-PARTY-NOTICES.txt' -Destination $licenses
    Copy-Item -LiteralPath (Join-Path (Split-Path -Parent $InnoCompiler) 'license.txt') -Destination (Join-Path $licenses 'Inno-Setup.txt')
    $assets = Get-Content -Raw -Encoding UTF8 'CodexPet.App\obj\project.assets.json' | ConvertFrom-Json
    $packageRoots = @($assets.packageFolders.PSObject.Properties.Name)
    $frameworks = (Get-Content -Raw -Encoding UTF8 (Join-Path $publishRoot 'App\CodexPet.App.runtimeconfig.json') | ConvertFrom-Json).runtimeOptions.includedFrameworks
    foreach ($framework in $frameworks) {
        $packageName = "$($framework.name).Runtime.win-x64".ToLowerInvariant()
        $package = $null
        foreach ($packageRoot in $packageRoots) {
            $candidate = Join-Path (Join-Path $packageRoot $packageName) $framework.version
            if (Test-Path -LiteralPath $candidate) { $package = $candidate; break }
        }
        if (!$package) { throw "런타임 팩의 라이선스를 찾을 수 없습니다: $packageName $($framework.version)" }
        $licenseName = if ($framework.name -eq 'Microsoft.NETCore.App') { 'LICENSE.TXT' } else { 'LICENSE' }
        Copy-Item -LiteralPath (Join-Path $package $licenseName) -Destination (Join-Path $licenses "$($framework.name)-LICENSE.txt")
        $notice = Join-Path $package 'THIRD-PARTY-NOTICES.TXT'
        if (Test-Path -LiteralPath $notice) { Copy-Item -LiteralPath $notice -Destination (Join-Path $licenses "$($framework.name)-THIRD-PARTY-NOTICES.txt") }
        Write-Output "포함 런타임: $($framework.name) $($framework.version)"
    }
    & $InnoCompiler '/Q' ('/DPublishRoot=' + $publishRoot) ('/O' + $setupRoot) 'installer\CodexPet.iss'
    if ($LASTEXITCODE -ne 0) { throw "설치 파일 빌드 실패: $LASTEXITCODE. 이전 결과물은 이번 빌드 결과가 아닙니다." }
    $setup = Join-Path $setupRoot "CodexPet-Setup-$version-win-x64.exe"
    if (!(Test-Path -LiteralPath $setup)) { throw "설치 파일을 찾을 수 없습니다: $setup" }
    $finalDir = Join-Path $root 'artifacts\setup'
    New-Item -ItemType Directory -Force -Path $finalDir | Out-Null
    Copy-Item -LiteralPath $setup -Destination $finalDir -Force
    Write-Output "빌드 성공: $(Join-Path $finalDir (Split-Path -Leaf $setup))"
    Write-Output "격리 시험용 게시 폴더: $publishRoot"
}
finally { Pop-Location }
