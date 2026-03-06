cmake_minimum_required(VERSION 3.10)

# 1. 프로젝트 설정
project(CK_ProjectRun_Server LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# 2. Sentry Native SDK 포함 (경로: thirdparty/sentry-native)
add_subdirectory(thirdparty/sentry-native)

# 3. 서버 소스 파일 등록
file(GLOB_RECURSE SOURCES 
    "Server/*.cpp" 
    "Server/*.h"
)

# 4. 실행 파일 생성
add_executable(FinalProjectServer ${SOURCES})

# 5. 라이브러리 및 헤더 연결
target_link_libraries(FinalProjectServer PRIVATE sentry::sentry)
target_include_directories(FinalProjectServer PRIVATE Server)

# 6. 빌드 후 종속성 파일 자동 복사 (Windows)
if(WIN32)
    add_custom_command(TARGET FinalProjectServer POST_BUILD
        # sentry.dll 복사
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            $<TARGET_FILE:sentry::sentry>
            $<TARGET_FILE_DIR:FinalProjectServer>
        
        # crashpad_handler.exe 복사 (슬랙 알람 필수 파일)
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            $<TARGET_FILE_DIR:sentry::sentry>/crashpad_handler.exe
            $<TARGET_FILE_DIR:FinalProjectServer>
            
        COMMENT "Sentry 종속성 파일을 빌드 폴더로 복사 중..."
    )
endif()