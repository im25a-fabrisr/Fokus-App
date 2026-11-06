param(
    [string]$AppPath = (Join-Path $PSScriptRoot '..\artifacts\windows\Fokus-App.exe')
)
$ErrorActionPreference = 'Stop'
$resolvedApp = (Resolve-Path -LiteralPath $AppPath).Path
if ([IO.Path]::GetFileName($resolvedApp) -ne 'Fokus-App.exe') {
    throw 'Bitte die gebaute Fokus-App.exe angeben.'
}
$appDirectory = Split-Path -Parent $resolvedApp
$desktopDirectory = [Environment]::GetFolderPath('Desktop')
$programsDirectory = [Environment]::GetFolderPath('Programs')
$shell = New-Object -ComObject WScript.Shell
try {
    foreach ($directory in @($desktopDirectory, $programsDirectory)) {
        if ([string]::IsNullOrWhiteSpace($directory)) { throw 'Windows-Verknuepfungsordner nicht gefunden.' }
        [IO.Directory]::CreateDirectory($directory) | Out-Null
        $shortcutPath = Join-Path $directory 'Fokus-App.lnk'
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $resolvedApp
        $shortcut.WorkingDirectory = $appDirectory
        $shortcut.IconLocation = "$resolvedApp,0"
        $shortcut.Description = 'Fokusmodi und konzentrierte Lernphasen'
        $shortcut.WindowStyle = 1
        $shortcut.Save()
        Write-Output "Verknuepfung erstellt: $shortcutPath"
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcut)
    }
} finally {
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell)
}
