cmake_minimum_required(VERSION 3.10)

# 1. 프로젝트 설정
project(CK_ProjectRun_Server LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# 2. Sentry 경로 설정 (이미지에서 확인된 경로)
set(SENTRY_DIR "${CMAKE_SOURCE_DIR}/thirdparty/sentry-native")

# 3. 서버 소스 파일 등록
file(GLOB_RECURSE SOURCES 
    "Server/*.cpp" 
    "Server/*.h"
)

# 4. 실행 파일 생성
add_executable(FinalProjectServer ${SOURCES})

# 5. Sentry 라이브러리 연결 (Pre-compiled 방식)
# 헤더 파일 위치 알려주기
target_include_directories(FinalProjectServer PRIVATE "${SENTRY_DIR}")

# .lib 파일 직접 연결
target_link_libraries(FinalProjectServer PRIVATE "${SENTRY_DIR}/sentry.lib")

# 6. 빌드 후 종속성 파일 자동 복사 (Windows)
if(WIN32)
    add_custom_command(TARGET FinalProjectServer POST_BUILD
        # sentry.dll 복사
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${SENTRY_DIR}/sentry.dll"
            $<TARGET_FILE_DIR:FinalProjectServer>
        
        # crashpad_handler.exe 복사 (슬랙 알람 필수 파일)
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${SENTRY_DIR}/crashpad_handler.exe"
            $<TARGET_FILE_DIR:FinalProjectServer>
            
        COMMENT "Sentry 바이너리 파일들을 빌드 폴더로 복사 중..."
    )
endif()