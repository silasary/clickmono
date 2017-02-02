node {
    stage 'Clone'
    checkout scm
   
    stage 'Build'
	msbuild()
	mono("Packager/bin/Release/Packager.exe", "Packager/bin/Release/Packager.exe")
    
    stage 'Archive'
    archive '**/bin/Release/'

	stage 'Post-Build'
	step([$class: 'WarningsPublisher', canComputeNew: false, canResolveRelativePaths: false, consoleParsers: [[parserName: 'MSBuild']], defaultEncoding: '', excludePattern: '', healthy: '', includePattern: '', messagesPattern: '', unHealthy: ''])

}
