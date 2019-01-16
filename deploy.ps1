
$ErrorActionPreference = 'Stop';

if (!(Test-Path ~/.docker)) { mkdir ~/.docker }
# "$env:DOCKER_PASS" | docker login --username "$env:DOCKER_USER" --password-stdin
# docker login with the old config.json style that is needed for manifest-tool
$auth =[System.Text.Encoding]::UTF8.GetBytes("$($env:DOCKER_USER):$($env:DOCKER_PASS)")
$auth64 = [Convert]::ToBase64String($auth)
@"
{
  "auths": {
    "https://index.docker.io/v1/": {
      "auth": "$auth64"
    }
  },
  "experimental": "enabled"
}
"@ | Out-File -Encoding Ascii ~/.docker/config.json

$os = If ($isWindows) {"windows"} Else {"linux"}
docker tag local_image:$os-${env:ARCH} ${env:REMOTE_IMAGE}:$os-${env:ARCH}
docker push ${env:REMOTE_IMAGE}:$os-${env:ARCH}
IF (${env:APPVEYOR_REPO_TAG} -eq "true") {
    docker tag local_image:$os-${env:ARCH} ${env:REMOTE_IMAGE}:${env:APPVEYOR_REPO_TAG_NAME}-$os-${env:ARCH}
    docker push ${env:REMOTE_IMAGE}:${env:APPVEYOR_REPO_TAG_NAME}-$os-${env:ARCH}
}

if (($env:ARCH -eq "amd64") -and (-not $isWindows)) {
    # The last in the build matrix
    docker -D manifest create "$($env:REMOTE_IMAGE):latest" `
        "$($env:REMOTE_IMAGE):linux-arm32v7" `
        "$($env:REMOTE_IMAGE):linux-arm64v8" `
        "$($env:REMOTE_IMAGE):linux-amd64" `
        "$($env:REMOTE_IMAGE):windows-amd64"
    docker manifest push "$($env:REMOTE_IMAGE):latest"

    IF (${env:APPVEYOR_REPO_TAG} -eq "true") {
        docker -D manifest create "$($env:REMOTE_IMAGE):$($env:APPVEYOR_REPO_TAG_NAME)" `
            "$($env:REMOTE_IMAGE):$($env:APPVEYOR_REPO_TAG_NAME)-linux-arm32v7" `
            "$($env:REMOTE_IMAGE):$($env:APPVEYOR_REPO_TAG_NAME)-linux-arm64v8" `
            "$($env:REMOTE_IMAGE):$($env:APPVEYOR_REPO_TAG_NAME)-linux-amd64" `
            "$($env:REMOTE_IMAGE):$($env:APPVEYOR_REPO_TAG_NAME)-windows-amd64"
        docker manifest push "$($env:REMOTE_IMAGE):$($env:APPVEYOR_REPO_TAG_NAME)"
    }
}