pipeline {
    agent any

    parameters {
        booleanParam(name: 'BUILD_APP', defaultValue: true, description: '是否建置主程式 (EXE)')
        booleanParam(name: 'BUILD_ADDRESSABLES', defaultValue: false, description: '是否建置 Addressables 資源包')
        string(name: 'COMMIT_MSG', defaultValue: 'Update Addressables for GitHub Pages', description: 'Git Commit 訊息')
    }

    environment {
        UNITY_EXE = 'E:\\Unity\\6000.3.2f1\\Editor\\Unity.exe'
        EXTERNAL_ASSETS_DIR = 'E:\\MyUnityProject\\Rules Of Card File\\Rules Of Card Assets'
        TARGET_PLATFORM = 'StandaloneWindows64'
    }

    stages {
        
        stage('Environment Check') {
            steps {
                script {
                    echo "🔍 正在驗證建置環境..."

                    // 1. 檢查 Unity 執行檔
                    def unityExists = fileExists(UNITY_EXE)
                    
                    // 使用 bat 回傳狀態來檢查
                    int unityCheck = bat(returnStatus: true, script: "if not exist \"${UNITY_EXE}\" exit 1")
                    if (unityCheck != 0) {
                        error "❌ [錯誤] 找不到 Unity 執行檔！\n路徑：${UNITY_EXE}\n請檢查 Jenkins Server 的 E 槽是否掛載正確。"
                    }

                    // 2. 檢查外部資源 Git 倉庫資料夾
                    int assetsCheck = bat(returnStatus: true, script: "if not exist \"${EXTERNAL_ASSETS_DIR}\" exit 1")
                    if (assetsCheck != 0) {
                        error "❌ [錯誤] 找不到資源倉庫資料夾！\n路徑：${EXTERNAL_ASSETS_DIR}\n請確認該路徑存在且 Jenkins 有權限存取。"
                    }

                    // 3. 檢查 Unity 是否正在執行
                    def status = bat(returnStatus: true, script: 'tasklist /FI "IMAGENAME eq Unity.exe" | findstr /I "Unity.exe"')
                    if (status == 0) {
                        error "❌ [錯誤] 檢測到 Unity Editor 正在運行中！請先關閉 Unity 以免檔案被鎖死導致打包失敗。"
                    }
                    
                    echo "✅ 環境檢查通過！"
                }
            }
        }

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build Addressables') {
            when { expression { return params.BUILD_ADDRESSABLES } }
            steps {
                echo "🚀 打包 Addressables..."
                bat """
                "${UNITY_EXE}" -batchmode -nographics -quit ^
                -projectPath "%WORKSPACE%" ^
                -executeMethod JenkinsBuild.BuildAddressables ^
                -logFile "%WORKSPACE%\\logs_addressables.txt"
                """
            }
        }

        stage('Build Main App') {
            when { expression { return params.BUILD_APP } }
            steps {
                echo "🚀 打包主程式..."
                bat """
                "${UNITY_EXE}" -batchmode -nographics -quit ^
                -projectPath "%WORKSPACE%" ^
                -executeMethod JenkinsBuild.BuildProject ^
                -logFile "%WORKSPACE%\\logs_mainbuild.txt"
                """
            }
        }

        stage('Push to GitHub Pages') {
            when { expression { return params.BUILD_ADDRESSABLES } }
            steps {
                echo "📦 切換到外部資料夾並同步至 GitHub..."
                script {
                    // 使用 dir 切換到外部的資源 Git 倉庫執行指令
                    dir("${EXTERNAL_ASSETS_DIR}") {
                        bat """
                            git config user.email "wu11158001@gmail.com"
                            git config user.name "wu11158001"

                            :: 檢查是否有檔案變動
                            git status
                            git add .
                            
                            :: 只有在有變動時才執行 commit 與 push
                            git diff-index --quiet HEAD || (
                                git commit -m "${params.COMMIT_MSG}"
                                git push origin HEAD
                                echo "✅ 資源已成功更新至 GitHub"
                            )
                        """
                    }
                }
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: 'logs_*.txt', allowEmptyArchive: true
        }
    }
}