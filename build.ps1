function Is-Admin() {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function main() {
    if (-not (Is-Admin)) {
        Write-Host "error: administrator privileges required"
        return 1
    }

    if (Test-Path ".\build\") {
        Remove-Item -Path ".\build\" -Recurse -Force
    }

    mkdir ".\build\"

    # create folder structure
    mkdir ".\build\ReservedCpuSets\"

    # build application
    MSBuild.exe ".\ReservedCpuSets.sln" -p:Configuration=Release -p:Platform=x64

    # create final package
    Copy-Item ".\bin\x64\Release\ReservedCpuSets.exe" ".\build\ReservedCpuSets\"
    Copy-Item ".\bin\x64\Release\ReservedCpuSets.dll" ".\build\ReservedCpuSets\"

    return 0
}

$_exitCode = main
Write-Host # new line
exit $_exitCode
