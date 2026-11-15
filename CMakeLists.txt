cmake_minimum_required(VERSION 3.20)
project(ScreenResy LANGUAGES C RC)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

# Link the C runtime into the exe, so it runs on any PC with nothing else to install.
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")

add_executable(ScreenResy
    src/main.c
    src/commandline.c
    src/display.c
    src/window.c
    src/ScreenResy.rc
)

target_compile_definitions(ScreenResy PRIVATE UNICODE _UNICODE)
target_link_libraries(ScreenResy PRIVATE user32 gdi32 shell32 comctl32)

if(MSVC)
    target_compile_options(ScreenResy PRIVATE /W4 /WX)
    # src/ScreenResy.rc embeds src/app.manifest; the linker must not add a manifest of its own.
    target_link_options(ScreenResy PRIVATE /MANIFEST:NO)
endif()
