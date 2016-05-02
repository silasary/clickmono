node {
    stage 'Clone'
    git 'https://github.com/silasary/zigzagoon.git'
    stage 'Build'
    if (isUnix())
    {

    }
    else
    {
      bat 'nuget restore'
      bat 'msbuild'
    }
    stage 'Archive'
    archive 'Fingbot\\bin\\Debug\\'

}