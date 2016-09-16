node {
    stage('Build') {
    if (isUnix())
    {
        sh 'nuget restore'
        sh 'xbuild'
    }
    else
    {
      bat 'nuget restore'
      bat 'msbuild'
    }
    }
    stage('Archive') {
      archive '**/bin/**/'
      //archiveArtifacts allowEmptyArchive: false, artifacts: '\'**/bin/**/', caseSensitive: false, excludes: null, fingerprint: true, onlyIfSuccessful: true
    }
	stage('Post-Build') {
	  step([$class: 'WarningsPublisher', canComputeNew: false, canResolveRelativePaths: false, consoleParsers: [[parserName: 'MSBuild']], defaultEncoding: '', excludePattern: '', healthy: '', includePattern: '', messagesPattern: '', unHealthy: ''])
	}
}
