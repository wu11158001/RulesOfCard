pipeline {
    agent any

    environment {
        UNITY_EXE = 'E:\\Unity\\6000.3.2f1\\Editor\\Unity.exe'
        BUILD_PATH = 'Builds'
    }

    stages {
        stage('Environment Check') {
            steps {
                echo "檢查 Unity 環境與專案狀態..."
                script {
                    // 1. 檢查 Unity 執行檔是否存在
                    bat "if not exist \"${UNITY_EXE}\" (echo Unity Editor Not Found && exit 1)"

                    // 2. 檢查專案是否正在運行 (透過檢查 Unity 進程)
                    // 使用 tasklist 檢查是否有 Unity.exe 在執行，並過濾當前專案
                    // 註：這是一個嚴格檢查，若機器上有任何 Unity 正在執行都會擋住。
                    // 如果要更精準，可以檢查專案目錄下的 Temp/UnityLockFile
                    def status = bat(
                        returnStatus: true, 
                        script: 'tasklist /FI "IMAGENAME eq Unity.exe" | findstr /I "Unity.exe"'
                    )
                    
                    if (status == 0) {
                        error "❌ 檢測到 Unity Editor 正在運行中，為避免檔案衝突，停止打包！請先關閉 Unity。"
                    }
                }
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
            echo '❌ 流程中斷或失敗，請檢查 Log 或確認 Unity 是否已關閉。'
        }
    }
}