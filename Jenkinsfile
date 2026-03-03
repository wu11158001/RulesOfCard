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
                    bat "if not exist \"${UNITY_EXE}\" (echo Unity Editor Not Found && exit 1)"
                    // 檢查外部資源目錄是否存在 (Git 倉庫所在處)
                    bat "if not exist \"${EXTERNAL_ASSETS_DIR}\" (echo Assets Repository Folder Not Found && exit 1)"
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