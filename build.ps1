param(
    [string]$TMLPath='C:\Program Files (x86)\Steam\steamapps\common\tModLoader',
    [string]$Dotnet=(Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'),
    [switch]$CompileOnly
)
$ErrorActionPreference='Stop'
$buildRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\VodyanitsaBuild'))
$runtime=Join-Path $buildRoot 'build-runtime'
$save=Join-Path $buildRoot 'build-save'
$release=Join-Path $buildRoot 'release'
foreach($file in @($Dotnet,(Join-Path $TMLPath 'tModLoader.dll'))){if(-not(Test-Path -LiteralPath $file)){throw "Missing build dependency: $file"}}
$oldPath=$env:PATH
try{
    $env:PATH=(Split-Path $Dotnet)+';'+$env:PATH
    & $Dotnet build (Join-Path $PSScriptRoot 'Vodyanitsa.csproj') -c Release ('-p:TMLPath='+$TMLPath) --nologo
    if($LASTEXITCODE -ne 0){throw 'Compilation failed.'}
    if($CompileOnly){return}
    foreach($dir in @($runtime,$save,$release)){[IO.Directory]::CreateDirectory($dir)|Out-Null}
    foreach($dir in @('Content','Libraries')){
        $link=Join-Path $runtime $dir
        if(-not(Test-Path -LiteralPath $link)){New-Item -ItemType Junction -Path $link -Target (Join-Path $TMLPath $dir)|Out-Null}
    }
    foreach($file in @('tModLoader.dll','tModLoader.deps.json','tModLoader.runtimeconfig.json','tModLoader.runtimeconfig.dev.json')){
        $source=Join-Path $TMLPath $file
        if(Test-Path -LiteralPath $source){Copy-Item -LiteralPath $source -Destination (Join-Path $runtime $file)}
    }
    $env:PATH=(Join-Path $TMLPath 'Libraries\Native\Windows')+';'+$env:PATH
    Push-Location -LiteralPath $runtime
    try{
        & (Join-Path $TMLPath 'dotnet\dotnet.exe') (Join-Path $runtime 'tModLoader.dll') -server -build $PSScriptRoot -eac (Join-Path $PSScriptRoot 'bin\Release\net8.0\Vodyanitsa.dll') -savedirectory $save
        if($LASTEXITCODE -ne 0){throw 'Packaging failed.'}
    }finally{Pop-Location}
    $package=Join-Path $save 'tModLoader\Mods\Vodyanitsa.tmod'
    if(-not(Test-Path -LiteralPath $package)){throw 'Build did not produce Vodyanitsa.tmod.'}
    Copy-Item -LiteralPath $package -Destination (Join-Path $release 'Vodyanitsa.tmod')
    Get-FileHash -LiteralPath (Join-Path $release 'Vodyanitsa.tmod') -Algorithm SHA256
}finally{$env:PATH=$oldPath}
