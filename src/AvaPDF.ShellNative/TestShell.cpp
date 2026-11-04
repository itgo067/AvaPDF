#include <windows.h>
#include <shobjidl.h>
#include <cstdio>

constexpr GUID TestClsid = { 0x6f4a17d2, 0x6a4b, 0x4f1e, { 0xa6, 0xb3, 0x0a, 0x9a, 0x0d, 0xfd, 0x00, 0x01 } };

int main() {
    const auto init = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    IContextMenu* menu = nullptr;
    const auto create = CoCreateInstance(TestClsid, nullptr, CLSCTX_INPROC_SERVER, IID_IContextMenu,
        reinterpret_cast<void**>(&menu));
    std::printf("CoCreateInstance=0x%08X\n", static_cast<unsigned>(create));
    if (FAILED(create)) return static_cast<int>(create);
    const auto nativeMenu = CreateMenu();
    const auto query = menu->QueryContextMenu(nativeMenu, 0, 1, 100, 0);
    std::printf("QueryContextMenu=0x%08X\n", static_cast<unsigned>(query));
    menu->Release();
    CoUninitialize();
    return static_cast<int>(query);
}
