pipeline {
    agent any

    parameters {
        booleanParam(name: 'BUILD_EXE', defaultValue: true, description: '是否建置主程式 (EXE)')
        booleanParam(name: 'BUILD_ADDRESSABLES', defaultValue: false, description: '是否建置 Addressables 資源包')
        string(name: 'COMMIT_MSG', defaultValue: 'Update Addressables for GitHub Pages', description: 'Git Commit 訊息')
    }

    environment {
        UNITY_EXE = 'E:\\Unity\\6000.3.2f1\\Editor\\Unity.exe'
        EXTERNAL_ASSETS_DIR = 'E:\\JenkinsData\\Jenkins\\.jenkins\\workspace\\Rules Of Card Assets'
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
                echo "📦 準備同步資源至 GitHub main 分支..."
                script {
                    dir("${EXTERNAL_ASSETS_DIR}") {
                        bat """
                            @echo off
                            :: 1. 設定使用者資訊
                            git config user.email "wu11158001@gmail.com"
                            git config user.name "wu11158001"

                            :: 2. 強制切換/追蹤到 main 分支 (防止處於游離狀態)
                            git checkout main || git checkout -b main

                            :: 3. 加入檔案並檢查狀態
                            git add .
                            
                            :: 4. 提交與推送 (帶入強制的遠端分支路徑)
                            :: git diff-index 檢查是否有索引變動
                            git diff --cached --quiet || (
                                echo [Git] 偵測到新資源，正在提交...
                                git commit -m "${params.COMMIT_MSG}"
                                
                                echo [Git] 正在推送到 origin main...
                                :: 使用 HEAD:main 確保本地當前內容推送到遠端的 main
                                git push origin HEAD:main
                            )

                            if %ERRORLEVEL% EQU 0 (
                                echo ✅ GitHub Pages 資源同步完成！
                            ) else (
                                echo ❌ Git 操作失敗，請檢查權限或網路。
                                exit 1
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