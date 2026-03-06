FROM mcr.microsoft.com/windows/nanoserver:ltsc2022

WORKDIR /app

# 빌드 결과물 복사
COPY build/Release/FinalProjectServer.exe .
COPY build/Release/sentry.dll .
COPY build/Release/crashpad_handler.exe .
# 맵 데이터 등 필수 파일 복사
COPY Server/all_tiles_tilecache.bin .

RUN mkdir .sentry-native

EXPOSE 5025/udp

ENTRYPOINT ["FinalProjectServer.exe"]