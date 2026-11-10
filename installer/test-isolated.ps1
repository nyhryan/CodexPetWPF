param([Parameter(Mandatory)][string]$InnoCompiler, [Parameter(Mandatory)][string]$PublishRoot)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $PublishRoot = (Resolve-Path -LiteralPath $PublishRoot).Path
    $testRoot = Join-Path (Join-Path $root 'artifacts') ('isolated 한글 ' + [Guid]::NewGuid().ToString('N'))
    $appDir = Join-Path $testRoot 'CodexPet'
    $configDir = Join-Path $testRoot 'user\.codex'
    $config = Join-Path $configDir 'hooks.json'
    New-Item -ItemType Directory -Force $configDir | Out-Null
    '{"description":"preserved","hooks":{"Stop":[{"hooks":[{"type":"command","command":"other"}]}]}}' | Set-Content -Encoding UTF8 $config
    & $InnoCompiler '/Qp' ('/DTestRoot=' + $testRoot) ('/DPublishRoot=' + $PublishRoot) 'installer\CodexPet.iss'
    if ($LASTEXITCODE -ne 0) { throw '시험용 설치 파일 컴파일 실패' }
    $setup = Join-Path $root 'artifacts\setup\CodexPet-isolated-test.exe'
    $before = Get-Content -Raw -Encoding UTF8 $config
    $noConsent = Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-') -Wait -PassThru -WindowStyle Hidden
    if ($noConsent.ExitCode -eq 0 -or (Test-Path -LiteralPath $appDir) -or (Get-Content -Raw -Encoding UTF8 $config) -ne $before) {
        throw '라이선스 명시 동의 없이 무인 설치가 진행되었습니다.'
    }
    $wrongPath = Join-Path $testRoot 'wrong-location'
    $wrong = Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/ACCEPTLICENSE', ('/DIR="' + $wrongPath + '"')) -Wait -PassThru -WindowStyle Hidden
    if ($wrong.ExitCode -eq 0 -or (Test-Path -LiteralPath $wrongPath) -or (Get-Content -Raw -Encoding UTF8 $config) -ne $before) {
        throw '고정 설치 위치가 아닌 경로를 허용했습니다.'
    }
    $lock = [IO.File]::Open($config, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $failed = Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/ACCEPTLICENSE') -Wait -PassThru -WindowStyle Hidden
        if ($failed.ExitCode -ne 10) { throw "등록 실패 종료 코드 오류: $($failed.ExitCode)" }
    } finally { $lock.Dispose() }
    if ((Get-Content -Raw -Encoding UTF8 $config) -ne $before) { throw '등록 실패가 기존 설정을 변경했습니다.' }
    $installed = Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/ACCEPTLICENSE') -Wait -PassThru -WindowStyle Hidden
    if ($installed.ExitCode -ne 0) { throw "설치 실패: $($installed.ExitCode)" }
    foreach ($project in @('App', 'Hook', 'Install')) {
        $directory = if ($project -eq 'Install') { 'Installer' } else { $project }
        $exe = Join-Path (Join-Path $appDir $directory) "CodexPet.$project.exe"
        if (!(Test-Path -LiteralPath $exe)) { throw "실행 파일이 설치되지 않았습니다: $exe" }
        $version = (Get-Content -Raw -Encoding UTF8 'version.txt').Trim()
        if ([Diagnostics.FileVersionInfo]::GetVersionInfo($exe).ProductVersion -ne $version) { throw "설치된 제품 버전 불일치: $exe" }
    }
    if (Get-ChildItem -LiteralPath $appDir -Recurse -File | Where-Object { $_.Extension -in @('.gif', '.pdb') }) {
        throw '설치 파일에 GIF 또는 디버그 심볼이 포함되어 있습니다.'
    }
    foreach ($requiredNotice in @(
        'CodexPet-Setup-Terms.txt',
        'Microsoft-dotnet-SDK-10.0.403-LICENSE.txt',
        'Microsoft-WindowsDesktop-SDK-10.0.403-THIRD-PARTY-NOTICES.txt',
        'Microsoft-Windows-SDK-10.0.19041-LICENSE.rtf',
        'Microsoft-Windows-SDK-10.0.19041-THIRD-PARTY-NOTICES.rtf'
    )) {
        if (!(Test-Path -LiteralPath (Join-Path (Join-Path $PublishRoot 'Licenses') $requiredNotice))) {
            throw "게시된 고지를 찾을 수 없습니다: $requiredNotice"
        }
    }
    foreach ($license in Get-ChildItem -File -LiteralPath (Join-Path $PublishRoot 'Licenses')) {
        $installedLicense = Join-Path (Join-Path $appDir 'Licenses') $license.Name
        if (!(Test-Path -LiteralPath $installedLicense) -or
            [Convert]::ToBase64String([IO.File]::ReadAllBytes($license.FullName)) -ne [Convert]::ToBase64String([IO.File]::ReadAllBytes($installedLicense))) {
            throw "설치된 라이선스가 원본과 다릅니다: $($license.Name)"
        }
    }
    $installed = Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/ACCEPTLICENSE') -Wait -PassThru -WindowStyle Hidden
    if ($installed.ExitCode -ne 0) { throw "재설치 실패: $($installed.ExitCode)" }
    $json = Get-Content -Raw -Encoding UTF8 $config | ConvertFrom-Json
    if ($json.description -ne 'preserved' -or $json.hooks.Stop.Count -ne 2) { throw '기존 훅이 변경되었거나 중복되었습니다.' }
    $uninstaller = Join-Path $appDir 'unins000.exe'
    $mutex = [Threading.Mutex]::new($false, 'Local\CodexPet.Widget')
    try {
        $running = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -Wait -PassThru -WindowStyle Hidden
        if ($running.ExitCode -eq 0 -or !(Test-Path -LiteralPath (Join-Path $appDir 'App\CodexPet.App.exe'))) { throw '실행 중 제거가 중단되지 않았습니다.' }
    } finally { $mutex.Dispose() }
    New-Item -ItemType Directory -Force (Join-Path $appDir 'images'), (Join-Path $appDir 'Logs\v2') | Out-Null
    'settings' | Set-Content -Encoding UTF8 (Join-Path $appDir 'settings.json')
    'image' | Set-Content -Encoding UTF8 (Join-Path $appDir 'images\example.png')
    'old' | Set-Content -Encoding UTF8 (Join-Path $appDir 'Logs\hook.log')
    'new' | Set-Content -Encoding UTF8 (Join-Path $appDir 'Logs\v2\hook.log')
    $json.hooks.Stop[-1].hooks[0].timeout = 3
    $json | ConvertTo-Json -Depth 30 | Set-Content -Encoding UTF8 $config
    $before = Get-Content -Raw -Encoding UTF8 $config
    $removed = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -Wait -PassThru -WindowStyle Hidden
    if ($removed.ExitCode -eq 0 -or !(Test-Path -LiteralPath (Join-Path $appDir 'App\CodexPet.App.exe')) -or
        (Get-Content -Raw -Encoding UTF8 $config) -ne $before) { throw '변경된 훅의 제거가 안전하게 중단되지 않았습니다.' }
    $json.hooks.Stop[-1].hooks[0].timeout = 2
    $json | ConvertTo-Json -Depth 30 | Set-Content -Encoding UTF8 $config
    $removed = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -Wait -PassThru -WindowStyle Hidden
    if ($removed.ExitCode -ne 0 -or (Test-Path -LiteralPath (Join-Path $appDir 'App\CodexPet.App.exe'))) { throw '정상 제거에 실패했습니다.' }
    if ((Get-Content -Raw -Encoding UTF8 $config | ConvertFrom-Json).hooks.Stop.Count -ne 1) { throw '다른 훅을 보존하지 못했습니다.' }
    foreach ($item in @('settings.json', 'images\example.png', 'Logs\hook.log', 'Logs\v2\hook.log')) {
        if (!(Test-Path -LiteralPath (Join-Path $appDir $item))) { throw "사용자 데이터가 보존되지 않았습니다: $item" }
    }
    & $InnoCompiler '/Qp' ('/DTestRoot=' + $testRoot) ('/DPublishRoot=' + $PublishRoot) '/DTestDeleteData' 'installer\CodexPet.iss'
    if ($LASTEXITCODE -ne 0) { throw '삭제 선택 시험 설치 파일 컴파일 실패' }
    $installed = Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/ACCEPTLICENSE') -Wait -PassThru -WindowStyle Hidden
    if ($installed.ExitCode -ne 0) { throw "삭제 선택 시험 설치 실패: $($installed.ExitCode)" }
    'owned image' | Set-Content -Encoding UTF8 (Join-Path $appDir 'images\0123456789abcdef0123456789abcdef.png')
    'owned image with unusual extension' | Set-Content -Encoding UTF8 (Join-Path $appDir 'images\123456789abcdef0123456789abcdef0.data')
    'owned image without extension' | Set-Content -Encoding UTF8 (Join-Path $appDir 'images\23456789abcdef0123456789abcdef01')
    'other' | Set-Content -Encoding UTF8 (Join-Path $appDir 'Logs\v2\other.txt')
    $removed = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -Wait -PassThru -WindowStyle Hidden
    if ($removed.ExitCode -ne 0) { throw '선택한 사용자 데이터 삭제 시험 실패' }
    foreach ($item in @('settings.json', 'images\0123456789abcdef0123456789abcdef.png', 'images\123456789abcdef0123456789abcdef0.data', 'images\23456789abcdef0123456789abcdef01', 'Logs\hook.log', 'Logs\v2\hook.log')) {
        if (Test-Path -LiteralPath (Join-Path $appDir $item)) { throw "선택한 데이터가 삭제되지 않았습니다: $item" }
    }
    foreach ($item in @('images\example.png', 'Logs\v2\other.txt')) {
        if (!(Test-Path -LiteralPath (Join-Path $appDir $item))) { throw "다른 파일이 삭제되었습니다: $item" }
    }
    Write-Output "격리된 설치·재설치·실패·제거 검사 통과: $testRoot"
}
finally { Pop-Location }
