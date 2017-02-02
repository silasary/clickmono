node {
    stage 'Clone'
    checkout scm
   
    stage 'Build'
	msbuild()
	mono("Packager/bin/Debug/Packager.exe", "Packager/bin/Debug/Packager.exe")
    
    stage 'Archive'
    archive '**/bin/Debug/'

	stage 'Post-Build'
	step([$class: 'WarningsPublisher', canComputeNew: false, canResolveRelativePaths: false, consoleParsers: [[parserName: 'MSBuild']], defaultEncoding: '', excludePattern: '', healthy: '', includePattern: '', messagesPattern: '', unHealthy: ''])

}
