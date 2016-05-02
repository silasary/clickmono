node {
    stage 'Clone'
    checkout scm
   
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
    archive '**/bin/Debug/'

}
