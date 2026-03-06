cmake_minimum_required(VERSION 3.10)

# 1. 프로젝트 설정
project(CK_ProjectRun_Server LANGUAGES CXX C) # fastlz가 .c 파일일 수 있어 C 언어도 추가

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# 2. 경로 설정
set(SENTRY_DIR "${CMAKE_SOURCE_DIR}/thirdparty/sentry-native")
set(TP_DIR "${CMAKE_SOURCE_DIR}/thirdparty")
set(HIREDIS_DIR "${TP_DIR}/hiredis")

# 3. 모든 소스 파일 수집 (서버 + 내비게이션 소스코드)
# 서버 소스
file(GLOB_RECURSE SERVER_SOURCES "Server/*.cpp" "Server/*.h")

# Detour 소스
file(GLOB_RECURSE DETOUR_SOURCES 
    "${TP_DIR}/Detour/*.cpp" 
    "${TP_DIR}/Detour/*.h"
)

# DetourTileCache 소스
file(GLOB_RECURSE TILECACHE_SOURCES 
    "${TP_DIR}/recastnavigation/DetourTileCache/*.cpp" 
    "${TP_DIR}/recastnavigation/DetourTileCache/*.h"
)

# FastLZ 소스 (.c 파일 포함)
file(GLOB_RECURSE FASTLZ_SOURCES 
    "${TP_DIR}/recastnavigation/RecastDemo/Contrib/fastlz/*.c" 
    "${TP_DIR}/recastnavigation/RecastDemo/Contrib/fastlz/*.h"
)

# 4. 실행 파일 생성 (모든 소스를 때려넣습니다)
add_executable(FinalProjectServer 
    ${SERVER_SOURCES} 
    ${DETOUR_SOURCES} 
    ${TILECACHE_SOURCES} 
    ${FASTLZ_SOURCES}
)

# 5. 헤더 파일 경로 설정 (컴파일러가 .h 파일을 찾을 수 있게)
target_include_directories(FinalProjectServer PRIVATE 
    "Server"
    "${SENTRY_DIR}"
    "${TP_DIR}/Detour/Include"
    "${TP_DIR}/recastnavigation/DetourTileCache/Include"
    "${TP_DIR}/recastnavigation/DetourTileCache/Source"
    "${TP_DIR}/recastnavigation/RecastDemo/Contrib/fastlz"
    "${HIREDIS_DIR}"
)

# 6. Sentry 라이브러리 연결 (Sentry는 이미 빌드된 .lib 사용)
target_link_libraries(FinalProjectServer PRIVATE 
    "${SENTRY_DIR}/sentry.lib"
    "${HIREDIS_DIR}/VS-IDE/Release/hiredis.lib" # hiredis 라이브러리 파일 경로
    "Winmm.lib" # Redis나 서버에서 시간 관련 함수를 쓴다면 필요할 수 있음
)

# 7. 전처리기 설정 (경고 및 에러 방지)
target_compile_definitions(FinalProjectServer PRIVATE 
    _WINSOCK_DEPRECATED_NO_WARNINGS
    _CRT_SECURE_NO_WARNINGS # 구형 C 함수 사용 시 발생하는 경고 방지
)

# 8. 빌드 후 종속성 파일 자동 복사 (Windows)
if(WIN32)
    add_custom_command(TARGET FinalProjectServer POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${SENTRY_DIR}/sentry.dll"
            $<TARGET_FILE_DIR:FinalProjectServer>
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            "${SENTRY_DIR}/crashpad_handler.exe"
            $<TARGET_FILE_DIR:FinalProjectServer>
        COMMENT "Sentry binaries copying..."
    )
endif()