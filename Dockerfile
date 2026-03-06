# 런타임이 포함된 서버 코어 이미지로 변경
FROM mcr.microsoft.com/windows/servercore:ltsc2022

WORKDIR /app

# 빌드 결과물 복사
COPY build/Release/ .

# Sentry용 폴더 생성
RUN mkdir .sentry-native

EXPOSE 5025/udp
EXPOSE 11021/tcp

ENTRYPOINT ["FinalProjectServer.exe"]