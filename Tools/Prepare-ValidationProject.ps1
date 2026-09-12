param(
    [string]$Destination = 'Temp/PackageValidation',
    [string]$UnityVersion = '6000.3.15f1',
    [switch]$UseCachedDependencies
)
$ErrorActionPreference = 'Stop'
$repositoryPath = Split-Path $PSScriptRoot -Parent
$projectPath = [IO.Path]::GetFullPath((Join-Path $repositoryPath $Destination))
if (!$projectPath.StartsWith($repositoryPath + [IO.Path]::DirectorySeparatorChar)) { throw 'Validation project must be inside this repository.' }
if (Test-Path -LiteralPath $projectPath) { throw "Destination already exists: $projectPath. Choose a new destination; existing data is not overwritten." }
New-Item -ItemType Directory -Path "$projectPath/Assets", "$projectPath/Packages", "$projectPath/ProjectSettings" -Force | Out-Null
$packages = @('com.hikanyan.uisystem', 'com.hikanyan.prefabkeysgenerator', 'com.hikanyan.uitools')
$dependencies = [ordered]@{}
foreach ($package in $packages) {
    # Embed only distributable packages: no developer Assets, Core, tools, or editor preferences.
    Copy-Item -LiteralPath "$repositoryPath/Packages/$package" -Destination "$projectPath/Packages/$package" -Recurse
}
$dependencies['com.cysharp.unitask'] = 'https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#a9e27c03d411d2fca01cc7410c24c97cd77cb539'
$dependencies['com.annulusgames.lit-motion'] = 'https://github.com/AnnulusGames/LitMotion.git?path=src/LitMotion/Assets/LitMotion#0b4c588ee75a07198841d92aab653e6b39445089'
if ($UseCachedDependencies) {
    foreach ($package in @('com.cysharp.unitask', 'com.annulusgames.lit-motion')) {
        $cached = Get-ChildItem "$repositoryPath/Library/PackageCache" -Directory | Where-Object Name -Like "$package@*" | Select-Object -First 1
        if (!$cached) { throw "Missing cached dependency: $package" }
        $dependencies[$package] = 'file:' + $cached.FullName.Replace('\', '/')
    }
}
$dependencies['com.unity.test-framework'] = '1.6.0'
$dependencies['com.unity.inputsystem'] = '1.19.0'
$dependencies['com.unity.addressables'] = '2.9.1'
$dependencies['com.unity.ugui'] = '2.0.0'
$dependencies['com.unity.modules.audio'] = '1.0.0'
$dependencies['com.unity.modules.ui'] = '1.0.0'
$dependencies['com.unity.modules.imgui'] = '1.0.0'
$dependencies['com.unity.modules.jsonserialize'] = '1.0.0'
$dependencies['com.unity.modules.physics'] = '1.0.0'
$dependencies['com.unity.modules.physics2d'] = '1.0.0'
$dependencies['com.unity.modules.unitywebrequest'] = '1.0.0'
$dependencies['com.unity.modules.unitywebrequestassetbundle'] = '1.0.0'
@{ dependencies = $dependencies; testables = $packages } | ConvertTo-Json -Depth 10 | Set-Content "$projectPath/Packages/manifest.json"
"m_EditorVersion: $UnityVersion" | Set-Content "$projectPath/ProjectSettings/ProjectVersion.txt"
Copy-Item -LiteralPath "$repositoryPath/Packages/com.hikanyan.uisystem/Samples~/Minimal" -Destination "$projectPath/Assets/Minimal" -Recurse
Copy-Item -LiteralPath "$PSScriptRoot/ValidationAssets/Editor" -Destination "$projectPath/Assets/Editor" -Recurse
Write-Output $projectPath
