#include <windows.h>
#include <shobjidl.h>
#include <cstdio>
#include <cstring>
#include <string>
#include <cwchar>

constexpr GUID TestClsid = { 0x6f4a17d2, 0x6a4b, 0x4f1e, { 0xa6, 0xb3, 0x0a, 0x9a, 0x0d, 0xfd, 0x00, 0x01 } };

int wmain(int argc, wchar_t** argv) {
    const auto init = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    if (FAILED(init)) return static_cast<int>(init);
    IContextMenu* menu = nullptr;
    const auto create = CoCreateInstance(TestClsid, nullptr, CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER, IID_IContextMenu,
        reinterpret_cast<void**>(&menu));
    std::printf("CoCreateInstance=0x%08X\n", static_cast<unsigned>(create));
    if (FAILED(create)) return static_cast<int>(create);
    const auto nativeMenu = CreateMenu();
    const auto query = menu->QueryContextMenu(nativeMenu, 0, 1, 100, 0);
    std::printf("QueryContextMenu=0x%08X\n", static_cast<unsigned>(query));
    if (argc > 2 && wcscmp(argv[1], L"--invoke") == 0) {
        const auto length = WideCharToMultiByte(CP_UTF8, 0, argv[2], -1, nullptr, 0, nullptr, nullptr);
        std::string directory(static_cast<size_t>(length - 1), '\0');
        WideCharToMultiByte(CP_UTF8, 0, argv[2], -1, directory.data(), length, nullptr, nullptr);
        CMINVOKECOMMANDINFO invoke = {};
        invoke.cbSize = sizeof(invoke);
        invoke.lpVerb = "extracttext";
        invoke.lpDirectory = directory.c_str();
        invoke.nShow = SW_SHOWNORMAL;
        const auto invoked = menu->InvokeCommand(&invoke);
        std::printf("InvokeCommand=0x%08X\n", static_cast<unsigned>(invoked));
        if (FAILED(invoked)) {
            menu->Release();
            CoUninitialize();
            return static_cast<int>(invoked);
        }
    }
    menu->Release();
    CoUninitialize();
    return static_cast<int>(query);
}
