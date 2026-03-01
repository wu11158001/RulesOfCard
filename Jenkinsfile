pipeline {
    agent any

    environment {
        UNITY_EXE = 'E:\\Unity\\6000.3.2f1\\Editor\\Unity.exe'
        BUILD_PATH = 'Builds/Windows'
    }

    stages {

        stage('Environment Check') {
            steps {
                echo "檢查 Unity 是否存在..."
                bat "if not exist \"${UNITY_EXE}\" (echo Unity Editor Not Found && exit 1)"
            }
        }

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Unity Build Windows') {
            steps {
                echo "開始打包 Windows..."
                bat """
                "${UNITY_EXE}" ^
                -batchmode ^
                -nographics ^
                -quit ^
                -projectPath "%WORKSPACE%" ^
                -buildTarget Win64 ^
                -executeMethod JenkinsBuild.BuildProject ^
                -logFile "%WORKSPACE%\\unity_build_log.txt"
                """
            }
        }

        stage('Archive Results') {
            steps {
                echo "儲存打包成品..."
                archiveArtifacts artifacts: "${BUILD_PATH}/**", fingerprint: true
                archiveArtifacts artifacts: 'unity_build_log.txt', allowEmptyArchive: true
            }
        }
    }

    post {
        success {
            echo '✅ 打包成功！'
        }
        failure {
            echo '❌ 打包失敗，請檢查 unity_build_log.txt'
        }
    }
}