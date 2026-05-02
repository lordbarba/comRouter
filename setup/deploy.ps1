<#
.SYNOPSIS
    Builds, publishes, and packages the ComRouter application.
.DESCRIPTION
    This script performs a complete deployment workflow:
    1. Builds the React frontend using Vite (output goes directly to wwwroot)
    2. Publishes the .NET WebServer backend project (Windows x64 + Linux ARM64)
    3. Publishes the WinForms client
    4. Updates version information in version.json
    5. Compiles the InnoSetup installer (Windows)
    6. Creates a tar.gz + install.sh for Linux ARM64
    7. Uploads installers and version info via FTP
.PARAMETER ProjectPath
    The path to the .NET project file (.csproj) to publish.
    Defaults to "..\src\Backend\CommRouter.WebServer\CommRouter.WebServer.csproj"
.PARAMETER ProfileName
    The name of the publishing profile to use.
    Defaults to "Release"
.PARAMETER AdditionalArguments
    Additional arguments to pass to the dotnet publish command.
.PARAMETER FtpServer
    The FTP server address to upload files to.
.PARAMETER FtpUsername
    The FTP username for authentication.
.PARAMETER FtpPassword
    The FTP password for authentication.
.PARAMETER FtpPath
    The path on the FTP server to upload files to.
.EXAMPLE
    .\deploy.ps1 -FtpServer "ftp.example.com" -FtpUsername "user" -FtpPassword "password" -FtpPath "/public/comRouter"
#>

param(
    [Parameter(Mandatory = $false, Position = 0)]
    [string]$ProjectPath = "..\src\Backend\CommRouter.WebServer\CommRouter.WebServer.csproj",
    
    [Parameter(Mandatory = $false, Position = 1)]
    [string]$ProfileName = "Release",
    
    [Parameter(Mandatory = $false, Position = 1)]
    [string]$ProductName = "ComRouter",

    [Parameter(Mandatory = $false, Position = 1)]
    [string]$CompanyName = "JBTechnology",

    [Parameter(Mandatory = $false)]
    [string]$AdditionalArguments,
    
    [Parameter(Mandatory = $false)]
    [string]$FtpServer= "ftp.jbtechnology.co.uk",
    
    [Parameter(Mandatory = $false)]
    [string]$FtpUsername="ftpuser",
    
    [Parameter(Mandatory = $false)]
    [string]$FtpPassword="JBTechnology2025!!!",
    
    [Parameter(Mandatory = $false)]
    [string]$FtpPath = "/public/comRouter",

    [Parameter(Mandatory = $false)]
    [switch]$SkipFrontend,

    [Parameter(Mandatory = $false)]
    [switch]$SkipFTP


)

# Get the directory where the script is located
$scriptRoot = $PSScriptRoot
$logFile = "deploy.log"
# Clear previous log
if (Test-Path -Path $logFile) {
    Remove-Item -Path $logFile -Force
}

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
    Add-Content -Path $logFile -Value $Message
}

# Colori per output
function Write-Header { Write-Log "`n=== $args ===" "Cyan" }
function Write-Success { Write-Log "✓ $args" "Green" }
function Write-Error { Write-Log "✗ $args" "Red" }
function Write-Info { Write-Log "ℹ $args" "White" }
function Write-Warning { Write-Log "⚠️ $args" "Yellow" }

# Function to check if the path exists
function Test-PathExists {
    param (
        [string]$Path
    )
    
    if (-not (Test-Path -Path $Path)) {
        Write-Error "The specified path does not exist: $Path"
        exit 1
    }
}

# Function to update version information
function Update-VersionInfo {
    param(
        [string]$Product = $ProductName,
        [string]$Company = $CompanyName
    )
    
    Write-Info "Updating version information..." 
    
    $versionFilePath = Join-Path -Path $scriptRoot -ChildPath "version.json"
    $buildDate = Get-Date -Format "yyyyMMdd_HHmmss"  # Format changed to YYYYMMDD_HHMMSS
    
    # Default version if file doesn't exist
    $versionNumber = "1.0.0.0"
    
    # Read existing version if file exists
    if (Test-Path -Path $versionFilePath) {
        try {
            $versionInfo = Get-Content -Path $versionFilePath -Raw | ConvertFrom-Json
            if ($versionInfo.version_number) {
                $versionParts = $versionInfo.version_number.Split('.')
                if ($versionParts.Count -ge 3) {
                    # Increment minor version
                    $majorVersion = [int]$versionParts[0]
                    $minorVersion = [int]$versionParts[1] + 1
                    $patchVersion = [int]$versionParts[2]
                    $buildNumber = if ($versionParts.Count -ge 4) { [int]$versionParts[3] } else { 0 }
                    $versionNumber = "$majorVersion.$minorVersion.$patchVersion.$buildNumber"
                }
            }
        }
        catch {
            Write-Warning "Could not parse existing version.json. Creating a new one."
        }
    }
    
    # Create new version info with the required format
    $versionInfo = @{
        author = $Company
        description = ""
        date = $buildDate
        name = $Product
        version_number = $versionNumber
        version_type = ""
    }
    
    # Write to file
    try {
        $versionInfoJson = $versionInfo | ConvertTo-Json
        $setupFolder = Join-Path -Path $scriptRoot -ChildPath "Setup"
        
        # Create Setup folder if it doesn't exist
        if (-not (Test-Path -Path $setupFolder)) {
            New-Item -ItemType Directory -Path $setupFolder -Force | Out-Null
        }

        Set-Content -Path $versionFilePath -Value $versionInfoJson -Force
        $version_number= $versionInfo.version_number
        Write-Success "Version updated to $version_number" 
    }
    catch {
        Write-Error "Failed to update version information: $_"
        exit 1
    }
    
    # Return version for use in other functions
    return $versionInfo    
}
# Function to build the React frontend with Vite
# Output goes directly to src/Backend/CommRouter.WebServer/wwwroot/ via vite.config.ts
function Build-Frontend {
    Write-Info "Building React frontend with Vite..."
    
    $frontendPath = Join-Path -Path $scriptRoot -ChildPath "..\src\Frontend"
    Test-PathExists -Path $frontendPath
    
    try {
        Set-Location -Path $frontendPath
        Write-Info "Running npm run build in $frontendPath"
        npm run build
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Frontend build failed with exit code $LASTEXITCODE"
            exit $LASTEXITCODE
        }

        Write-Success "Frontend build completed successfully."
    }
    catch {
        Write-Error "An error occurred during frontend build: $_"
        exit 1
    }
    finally {
        Set-Location -Path $scriptRoot
    }
}

# Function to copy frontend assets to backend wwwroot
# NOTE: In ComRouter, Vite builds directly to CommRouter.WebServer/wwwroot/ (see vite.config.ts outDir).
# This function is kept for reference but is NOT called in the main workflow.
function Copy-FrontendAssets {
    Write-Host "Copying frontend assets to backend wwwroot..." -ForegroundColor Cyan
    
    $sourcePath = Join-Path -Path $scriptRoot -ChildPath "..\src\Frontend\dist"
    Test-PathExists -Path $sourcePath
    
    $destinationPath = Join-Path -Path $scriptRoot -ChildPath "..\src\Backend\CommRouter.WebServer\wwwroot"
    
    try {
        # Create the destination directory if it doesn't exist
        if (Test-Path -Path $destinationPath) {
            Write-Host "Clearing existing content in $destinationPath" -ForegroundColor Gray
            Remove-Item -Path "$destinationPath\*" -Recurse -Force
        } else {
            New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
            Write-Host "Created wwwroot directory" -ForegroundColor Gray
        }
        
        Write-Host "Copying from $sourcePath to $destinationPath" -ForegroundColor Gray
        Copy-Item -Path "$sourcePath\*" -Destination $destinationPath -Recurse -Force
        
        Write-Host "Frontend assets copied successfully." -ForegroundColor Green
    }
    catch {
        Write-Error "An error occurred during asset copying: $_"
        exit 1
    }
}

function Publish-Backend {
    param (
        [Parameter(Mandatory = $false)]
        [string]$Version
    )
    Write-Info "Publishing .NET backend project..." 

    $fullProjectPath = Join-Path -Path $scriptRoot -ChildPath $ProjectPath
    Test-PathExists -Path $fullProjectPath
    
    # WebServer goes to publish\server\ — WinForms client root is cleared once before both publishes
    $publishServerDir = Join-Path -Path $scriptRoot -ChildPath "..\src\Backend\publish\server"
    Write-Info "Clearing existing content in $publishServerDir"
    if (Test-Path -Path $publishServerDir) {
        Remove-Item -Path "$publishServerDir\*" -Recurse -Force
    }
    try {
        # Construct publish command — WebServer outputs to publish\server\
        $publishCommand = "dotnet publish `"$fullProjectPath`" " +
                         "/p:Configuration=Release " +
                         "/p:Platform=`"Any CPU`" " +
                         "/p:PublishDir=`".\\..\\publish\\server\\\\`" " +
                         "/p:TargetFramework=net9.0 " +
                         "/p:RuntimeIdentifier=win-x64 " +
                         "/p:PublishSingleFile=true " +
                         "/p:SelfContained=true " +
                         "/p:DeleteExistingFiles=true"
        
                          # Add version if provided
        if (-not [string]::IsNullOrEmpty($Version)) {
            $publishCommand += " /p:Version=`"$Version`" /p:FileVersion=`"$Version`" /p:AssemblyVersion=`"$Version`""
            Write-Info "Setting version to $Version" 
        }
        # Add additional arguments if provided
        if ($AdditionalArguments) {
            $publishCommand += " $AdditionalArguments"
        }
        
        Write-Info "Executing: $publishCommand" 
        
        # Execute the publish command
        Invoke-Expression $publishCommand
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Backend publish completed successfully."
        }
        else {
            Write-Error "Publish failed with exit code $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    }
    catch {
        Write-Error "An error occurred during backend publishing: $_"
        exit 1
    }
}
# Function to compile the InnoSetup installer
function Build-Installer {
    param (
        [Parameter(Mandatory = $false)]
        [string]$Version
    )
    Write-Info "Compiling InnoSetup installer..."
    
    $innoSetupPath = Join-Path -Path $scriptRoot -ChildPath "installer.iss"
    Test-PathExists -Path $innoSetupPath
    
    try {
        # Find ISCC.exe (InnoSetup Command Line Compiler)
        $isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
        if (-not (Test-Path -Path $isccPath)) {
            $isccPath = "C:\Program Files\Inno Setup 6\ISCC.exe"
            if (-not (Test-Path -Path $isccPath)) {
                Write-Error "Could not find InnoSetup Compiler (ISCC.exe)"
                exit 1
            }
        }
        
        # Prepare arguments with version information if provided
        $arguments = "`"$innoSetupPath`""
        if (-not [string]::IsNullOrEmpty($Version)) {
            $arguments = "/DApplicationVersion=`"$Version`" $arguments"
            Write-Info "Setting InnoSetup version to $Version"   
        }

         # Use Start-Process instead of Invoke-Expression to handle paths with spaces
         Write-Info "Running InnoSetup compiler on $innoSetupPath"
         $process = Start-Process -FilePath $isccPath -ArgumentList "`"$innoSetupPath`"" -NoNewWindow -Wait -PassThru
         
         if ($process.ExitCode -eq 0) {
             Write-Success "InnoSetup compilation completed successfully."
         }
         else {
             Write-Error "InnoSetup compilation failed with exit code $($process.ExitCode)" 
             exit $process.ExitCode
         }
    }
    catch {
        Write-Error "An error occurred during InnoSetup compilation: $_"
        exit 1
    }
}

# Function to upload files via FTP
function Upload-ToFTP {
    param (
        [string]$LocalFile,
        [string]$RemoteFile
    )
    
    if (-not $FtpServer -or -not $FtpUsername -or -not $FtpPassword) {
        Write-Warning "FTP credentials not provided. Skipping upload of $LocalFile"
        return
    }
    
    try {
        # Create WebClient and set credentials
        $webClient = New-Object System.Net.WebClient
        $webClient.Credentials = New-Object System.Net.NetworkCredential($FtpUsername, $FtpPassword)
        
        # Construct the full remote path
        $remoteUrl = "ftp://$FtpServer"
        if (-not $FtpPath.StartsWith("/")) {
            $remoteUrl += "/"
        }
        $remoteUrl += "$FtpPath"
        if (-not $remoteUrl.EndsWith("/")) {
            $remoteUrl += "/"
        }
        $remoteUrl += $RemoteFile
        
        Write-Host "Uploading $LocalFile to $remoteUrl" -ForegroundColor Gray
        
        # Upload the file
        $webClient.UploadFile($remoteUrl, $LocalFile)
        
        Write-Host "Successfully uploaded $LocalFile" -ForegroundColor Green
    }
    catch {
        Write-Warning "Failed to upload ${LocalFile}: ${_}"
    }
    finally {
        if ($webClient) {
            $webClient.Dispose()
        }
    }
}

# Function to commit and push changes to Git
function Commit-AndPushToGit {
    param (
        [Parameter(Mandatory = $true)]
        [string]$CommitMessage
    )
    
    Write-Header "Committing and pushing to Git"
    
    try {
        # Check if git is available
        $gitVersion = git --version 2>$null
        if (-not $gitVersion) {
            Write-Warning "Git is not installed or not in PATH. Skipping Git commit."
            return
        }
        
        # Check if we're in a git repository
        $gitStatus = git status 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Not in a Git repository. Skipping Git commit."
            return
        }
        
        # Add all changes
        Write-Info "Adding all changes to Git..."
        git add .
        
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to add changes to Git"
            return
        }
        
        # Check if there are changes to commit
        $gitDiff = git diff --cached --quiet 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Info "No changes to commit"
            return
        }
        
        # Commit changes
        Write-Info "Committing changes with message: $CommitMessage"
        git commit -m $CommitMessage
        
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to commit changes to Git"
            return
        }
        
        # Push changes
        Write-Info "Pushing changes to remote repository..."
        git push
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Successfully committed and pushed changes to Git"
        }
        else {
            Write-Warning "Failed to push changes to Git. You may need to push manually."
        }
    }
    catch {
        Write-Warning "An error occurred during Git operations: $_"
    }
}

# Function to publish the WinForms client (CommRouter.exe) to publish\ root
function Publish-WinFormsClient {
    param (
        [Parameter(Mandatory = $false)]
        [string]$Version
    )
    Write-Info "Publishing WinForms client (CommRouter.exe)..."

    $clientProjectPath = Join-Path -Path $scriptRoot -ChildPath "..\src\Backend\CommRouter\CommRouter.csproj"
    Test-PathExists -Path $clientProjectPath

    $publishRootDir = Join-Path -Path $scriptRoot -ChildPath "..\src\Backend\publish"
    Write-Info "Clearing WinForms artifacts from $publishRootDir (preserves server\ subfolder)"
    if (Test-Path -Path $publishRootDir) {
        # Remove only root-level items, leaving server\ subfolder intact
        Get-ChildItem -Path $publishRootDir -Exclude "server" | Remove-Item -Recurse -Force
    }

    try {
        $publishCommand = "dotnet publish `"$clientProjectPath`" " +
                         "/p:Configuration=Release " +
                         "/p:Platform=`"Any CPU`" " +
                         "/p:PublishDir=`".\\..\\publish\\\\`" " +
                         "/p:TargetFramework=net10.0-windows " +
                         "/p:RuntimeIdentifier=win-x64 " +
                         "/p:PublishSingleFile=true " +
                         "/p:SelfContained=true " +
                         "/p:DeleteExistingFiles=false"

        if (-not [string]::IsNullOrEmpty($Version)) {
            $publishCommand += " /p:Version=`"$Version`" /p:FileVersion=`"$Version`" /p:AssemblyVersion=`"$Version`""
            Write-Info "Setting WinForms client version to $Version"
        }
        if ($AdditionalArguments) {
            $publishCommand += " $AdditionalArguments"
        }

        Write-Info "Executing: $publishCommand"
        Invoke-Expression $publishCommand

        if ($LASTEXITCODE -eq 0) {
            Write-Success "WinForms client publish completed successfully."
        }
        else {
            Write-Error "WinForms client publish failed with exit code $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    }
    catch {
        Write-Error "An error occurred during WinForms client publishing: $_"
        exit 1
    }
}

# Function to publish WebServer for Linux ARM64, generate install.sh and create tar.gz
function Publish-LinuxArm64 {
    param (
        [Parameter(Mandatory = $false)]
        [string]$Version
    )
    Write-Header "Build Linux ARM64 package"

    $fullProjectPath = Join-Path -Path $scriptRoot -ChildPath $ProjectPath
    Test-PathExists -Path $fullProjectPath

    $linuxPublishDir = Join-Path -Path $scriptRoot -ChildPath "..\src\Backend\publish\linux-arm64"
    Write-Info "Clearing existing content in $linuxPublishDir"
    if (Test-Path -Path $linuxPublishDir) {
        Remove-Item -Path "$linuxPublishDir\*" -Recurse -Force
    }

    try {
        $publishCommand = "dotnet publish `"$fullProjectPath`" " +
                         "/p:Configuration=Release " +
                         "/p:TargetFramework=net9.0 " +
                         "/p:RuntimeIdentifier=linux-arm64 " +
                         "/p:PublishSingleFile=true " +
                         "/p:SelfContained=true " +
                         "/p:PublishDir=`".\\..\\publish\\linux-arm64\\\\`" " +
                         "/p:DeleteExistingFiles=true"

        if (-not [string]::IsNullOrEmpty($Version)) {
            $publishCommand += " /p:Version=`"$Version`" /p:FileVersion=`"$Version`" /p:AssemblyVersion=`"$Version`""
            Write-Info "Setting Linux version to $Version"
        }

        Write-Info "Executing: $publishCommand"
        Invoke-Expression $publishCommand

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Linux ARM64 publish failed with exit code $LASTEXITCODE"
            exit $LASTEXITCODE
        }

        Write-Success "Linux ARM64 publish completed."
    }
    catch {
        Write-Error "An error occurred during Linux ARM64 publishing: $_"
        exit 1
    }

    # ── Copy install.sh from setup\ folder, normalizing to LF endings ─────────
    $installScriptSrc  = Join-Path -Path $scriptRoot -ChildPath "comRouter_install.sh"
    $installScriptPath = Join-Path -Path $linuxPublishDir -ChildPath "comRouter_install.sh"

    Test-PathExists -Path $installScriptSrc
    Write-Info "Copying comRouter_install.sh with LF line endings..."

    # Read source (may have CRLF on Windows), normalize to LF, write as UTF-8 without BOM
    $installContent = [System.IO.File]::ReadAllText($installScriptSrc) `
                        -replace "`r`n", "`n" -replace "`r", "`n"
    [System.IO.File]::WriteAllText($installScriptPath, $installContent, [System.Text.UTF8Encoding]::new($false))
    Write-Success "comRouter_install.sh copied with LF endings."

    # ── Create tar.gz ─────────────────────────────────────────────────────────
    $outputDir = Join-Path -Path $scriptRoot -ChildPath "Output"
    if (-not (Test-Path -Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    $versionSuffix = if ([string]::IsNullOrEmpty($Version)) { "" } else { "-$Version" }
    $tarFileName = "ComRouterLinux-arm64${versionSuffix}.tar.gz"
    $tarFilePath = Join-Path -Path $outputDir -ChildPath $tarFileName

    Write-Info "Creating $tarFileName..."
    try {
        & tar -czf $tarFilePath -C $linuxPublishDir .
        if ($LASTEXITCODE -ne 0) {
            Write-Error "tar failed with exit code $LASTEXITCODE"
            exit $LASTEXITCODE
        }
        $tarSizeMB = [math]::Round((Get-Item $tarFilePath).Length / 1MB, 2)
        Write-Success "Linux ARM64 package created: $tarFileName ($tarSizeMB MB)"
    }
    catch {
        Write-Error "Failed to create tar.gz: $_"
        exit 1
    }

    return $tarFilePath
}

# ============================================================
# MOBILE — Publish-Android (non applicabile a ComRouter)
# Sezione commentata: ComRouter non ha un client mobile MAUI.
# ============================================================
<#
function Publish-Android
{
    param (
        [Parameter(Mandatory = $false)]
        [string]$Version
    )
    Write-Header "Build Android App (MAUI)"

    $mauiProject = "Mobile\GestioneOrafo\GestioneOrafo.csproj"
    
    if (-not (Test-Path $mauiProject)) {
        Write-Error "Progetto MAUI non trovato: $mauiProject"
        exit 1
    }

    # Aggiorna versioni nel file csproj
    Write-Info "Aggiornamento versioni nel progetto MAUI..."
    $csprojContent = Get-Content $mauiProject -Raw
    
    # Aggiorna MajorVersion e MinorVersion
    # Ricava major e minor da $version (es: "1.2.3.4" -> major=1, minor=2)
    $versionParts = $version -split '\.'
    $major = if ($versionParts.Count -ge 1) { $versionParts[0] } else { "1" }
    $minor = if ($versionParts.Count -ge 2) { $versionParts[1] } else { "0" }
    Write-Info "Andorid version: Major-->$major   Minor-->$minor"

    $csprojContent = $csprojContent -replace '<MajorVersion>\d+</MajorVersion>', "<MajorVersion>$major</MajorVersion>"
    $csprojContent = $csprojContent -replace '<MinorVersion>\d+</MinorVersion>', "<MinorVersion>$minor</MinorVersion>"
    
    Set-Content -Path $mauiProject -Value $csprojContent -NoNewline
    Write-Success "Versioni aggiornate nel progetto MAUI"

    # Clean
    Write-Info "Pulizia progetto MAUI..."
    dotnet clean $mauiProject -c Release -f net9.0-android
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Errore durante la pulizia del progetto MAUI"
        exit 1
    }

    # Build e Publish Android
    Write-Info "Build e pubblicazione Android Release..."
    $androidOutput = "Mobile\GestioneOrafo\bin\Release\net9.0-android"
    
    dotnet publish $mauiProject `
        -f net9.0-android `
        -c Release `
        /p:AndroidPackageFormats=apk `
        /p:AndroidKeyStore=True `
        /p:AndroidSigningKeyStore=gestioneorafo.keystore `
        /p:AndroidSigningKeyAlias=gestioneorafo `
        /p:AndroidSigningKeyPass="Jacopo24agosto2007!" `
        /p:AndroidSigningStorePass="Jacopo24agosto2007!"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Errore durante la build Android"
        exit 1
    }

    # Verifica APK firmato
    $apkFile = "$androidOutput\com.jbtechnology.gestioneorafo-Signed.apk"
    if (Test-Path $apkFile) {
        $apkSize = (Get-Item $apkFile).Length / 1MB
        Write-Success "APK firmato creato: $apkFile ($([math]::Round($apkSize, 2)) MB)"

        # Store APK path for later copying (AFTER backend publish)
        $androidApkPath = $apkFile
    } else {
        Write-Error "APK firmato non trovato!"
        exit 1
    }
}
#>


# Main deployment workflow
Write-Info "Starting deployment process for ComRouter..." 
Write-Info "Script location: $scriptRoot" 

# Step 1: Update version information
$versionInfo = Update-VersionInfo
$version = $versionInfo.version_number

# Step 2: Build the React frontend
# Note: Vite outputs directly to CommRouter.WebServer/wwwroot/ — no copy step needed.
if (-not $SkipFrontend) {
    Build-Frontend
} else {
    Write-Info "Skipping frontend build due to -SkipFrontend."
}

# Step 3a: Publish the .NET WebServer project (→ publish\server\)
Publish-Backend -Version $version

# Step 3b: Publish the WinForms client (→ publish\)
Publish-WinFormsClient -Version $version

# Step 3c: Publish WebServer for Linux ARM64, generate install.sh, create tar.gz
$linuxTarPath = Publish-LinuxArm64 -Version $version

# Step 4: Compile the InnoSetup installer
Build-Installer -Version $version

# ============================================================
# MOBILE — Step Android (non applicabile a ComRouter)
# $compileAndroid = Read-Host "Do you want to compile the Android app? (Y/n)"
# if ([string]::IsNullOrWhiteSpace($compileAndroid) -or $compileAndroid -eq "Y" -or $compileAndroid -eq "y") {
#     Publish-Android -Version $version
# } else {
#     Write-Info "Skipping Android compilation."
# }
# ============================================================

# Step 5: Upload files to FTP if credentials are provided and not skipped
if ((-not $SkipFTP) -and $FtpServer -and $FtpUsername -and $FtpPassword) {
    Write-Info "Uploading files to FTP server..." 
    
    # Upload Windows Setup.exe
    $setupExePath = Join-Path -Path $scriptRoot -ChildPath "Output\*Setup.exe"
    $setupFiles = Get-ChildItem -Path $setupExePath -ErrorAction SilentlyContinue
    
    if ($setupFiles -and $setupFiles.Count -gt 0) {
        $setupFile = $setupFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        Upload-ToFTP -LocalFile $setupFile.FullName -RemoteFile $setupFile.Name
    } else {
        Write-Warning "No Setup.exe file found to upload"
    }

    # Upload Linux ARM64 tar.gz
    if (-not [string]::IsNullOrEmpty($linuxTarPath) -and (Test-Path $linuxTarPath)) {
        Upload-ToFTP -LocalFile $linuxTarPath -RemoteFile (Split-Path $linuxTarPath -Leaf)
    } else {
        Write-Warning "Linux ARM64 tar.gz not found, skipping upload"
    }
    
    # Upload version.json
    $versionFilePath = Join-Path -Path $scriptRoot -ChildPath "version.json"
    Upload-ToFTP -LocalFile $versionFilePath -RemoteFile "version.json"
}
elseif ($SkipFTP) {
    Write-Info "Skipping FTP upload due to -SkipFTP."
}
else {
    Write-Warning "FTP credentials not provided. Skipping file upload."
}

# Step 6: Commit and push to Git
Commit-AndPushToGit -CommitMessage "Version updated to $version"

Write-Success "Deployment of ComRouter completed successfully!"