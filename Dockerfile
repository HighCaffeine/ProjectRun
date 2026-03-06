# 윈도우 기반 서버용 경량 이미지
FROM mcr.microsoft.com/windows/nanoserver:ltsc2022

WORKDIR /app

# 빌드 결과물 및 필수 DLL 복사
COPY build/Release/FinalProjectServer.exe .
COPY build/Release/sentry.dll .
COPY build/Release/crashpad_handler.exe .

# Sentry용 캐시 폴더 생성
RUN mkdir .sentry-native

# 게임 서버 UDP 포트 개방
EXPOSE 5025/udp
EXPOSE 11021/tcp

ENTRYPOINT ["FinalProjectServer.exe"]