cmake_minimum_required(VERSION 3.10)

# 프로젝트 이름 및 언어 설정
project(FinalProjectServer LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Sentry SDK 포함
add_subdirectory(thirdparty/sentry-native)

# Server 폴더 안의 모든 .cpp와 .h 파일을 자동으로 찾습니다.
file(GLOB_RECURSE SOURCES 
    "Server/*.cpp" 
    "Server/*.h"
)

# 실행 파일 생성
add_executable(FinalProjectServer ${SOURCES})

# 라이브러리 연결
target_link_libraries(FinalProjectServer PRIVATE sentry::sentry)
target_include_directories(FinalProjectServer PRIVATE Server)

# 빌드 후 자동 복사 설정 (Windows)
if(WIN32)
    add_custom_command(TARGET FinalProjectServer POST_BUILD
        # sentry.dll 복사
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            $<TARGET_FILE:sentry::sentry>/sentry.dll    # 원본 파일 (sentry.dll)
            $<TARGET_FILE_DIR:FinalProjectServer>       # 복사될 목적지 (.exe 폴더)
        
        # crashpad_handler.exe 복사
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            $<TARGET_FILE_DIR:sentry::sentry>/crashpad_handler.exe
            $<TARGET_FILE_DIR:FinalProjectServer>
            
        COMMENT "Copying Sentry dependencies to executable directory..."
    )
endif()