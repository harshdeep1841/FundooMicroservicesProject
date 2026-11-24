
pipeline {
    agent any

    options {
        disableConcurrentBuilds()
        timestamps()
        timeout(time: 30, unit: 'MINUTES')
    }

    environment {
        DOCKER_IMAGE = 'harshdeep0406/fundoo-notes'
        K8S_NAMESPACE = 'fundoo'
        K8S_DEPLOYMENT = 'notes-deploy'
        K8S_CONTAINER = 'notes'
        DOTNET_IMAGE = 'mcr.microsoft.com/dotnet/sdk:8.0'
    }

    stages {

        stage('Checkout') {
            steps {
                checkout scm

                script {
                    env.IMAGE_TAG = sh(
                        script: 'git rev-parse --short=7 HEAD',
                        returnStdout: true
                    ).trim()

                    echo "Building commit: ${env.IMAGE_TAG}"
                }
            }
        }

        stage('Restore, Build and Test') {
            steps {
                sh '''
                    set -eu

                    docker run --rm \
                      --user "$(id -u):$(id -g)" \
                      -e HOME=/tmp \
                      -e NUGET_PACKAGES=/src/.nuget/packages \
                      -v "$WORKSPACE:/src" \
                      -w /src \
                      "$DOTNET_IMAGE" \
                      sh -c '
                        dotnet restore Fundoo.Notes.Api/Fundoo.Notes.Api.csproj &&
                        dotnet restore Fundoo.Notes.UnitTests/Fundoo.Notes.UnitTests.csproj &&
                        dotnet build Fundoo.Notes.Api/Fundoo.Notes.Api.csproj -c Release --no-restore &&
                        dotnet test Fundoo.Notes.UnitTests/Fundoo.Notes.UnitTests.csproj -c Release --no-restore --logger "console;verbosity=normal"
                      '
                '''
            }
        }

        stage('Build Docker Image') {
            steps {
                sh '''
                    docker build \
                      --build-arg PROJECT_PATH=Fundoo.Notes.Api/Fundoo.Notes.Api.csproj \
                      --build-arg APP_DLL=Fundoo.Notes.Api.dll \
                      -t "$DOCKER_IMAGE:$IMAGE_TAG" \
                      .
                '''
            }
        }

        stage('Push Docker Image') {
            steps {
                withCredentials([
                    usernamePassword(
                        credentialsId: 'dockerhub-credentials',
                        usernameVariable: 'DOCKER_USER',
                        passwordVariable: 'DOCKER_TOKEN'
                    )
                ]) {
                    sh '''
                        set +x
                        set -eu

                        export DOCKER_CONFIG="$(mktemp -d)"
                        trap 'docker logout >/dev/null 2>&1 || true; rm -rf "$DOCKER_CONFIG"' EXIT

                        printf '%s' "$DOCKER_TOKEN" |
                          docker login \
                            --username "$DOCKER_USER" \
                            --password-stdin

                        docker push "$DOCKER_IMAGE:$IMAGE_TAG"
                    '''
                }
            }
        }

        stage('Deploy to Kubernetes') {
            steps {
                withCredentials([
                    file(
                        credentialsId: 'k3s-fundoo-kubeconfig',
                        variable: 'KUBECONFIG_FILE'
                    )
                ]) {
                    sh '''
                        set -eu

                        export KUBECONFIG="$KUBECONFIG_FILE"

                        /usr/local/bin/kubectl set image \
                          deployment/"$K8S_DEPLOYMENT" \
                          "$K8S_CONTAINER"="$DOCKER_IMAGE:$IMAGE_TAG" \
                          -n "$K8S_NAMESPACE"

                        /usr/local/bin/kubectl rollout status \
                          deployment/"$K8S_DEPLOYMENT" \
                          -n "$K8S_NAMESPACE" \
                          --timeout=180s
                    '''
                }
            }
        }
    }

    post {
        success {
            echo "Notes deployment successful: ${env.DOCKER_IMAGE}:${env.IMAGE_TAG}"
        }

        failure {
            echo 'Fundoo Notes CI/CD pipeline failed. Check the stage logs.'
        }

        always {
            echo 'Fundoo Notes pipeline execution completed.'
        }
    }
}
