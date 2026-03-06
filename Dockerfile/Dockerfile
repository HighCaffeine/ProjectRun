# 윈도우 기반 컨테이너 사용
FROM mcr.microsoft.com/windows/nanoserver:ltsc2022

WORKDIR /app

# GitHub Actions 빌드 결과물 경로 기준으로 복사
COPY build/Release/FinalProjectServer.exe .
COPY build/Release/sentry.dll .
COPY build/Release/crashpad_handler.exe .
# 서버에 필요한 데이터 파일 (맵 데이터 등) 복사
COPY Server/all_tiles_tilecache.bin .

# Sentry 캐시 폴더 생성
RUN mkdir .sentry-native

# 서버 포트 개방
EXPOSE 5025/udp

ENTRYPOINT ["FinalProjectServer.exe"]