#include <windows.h>
#include <shobjidl.h>
#include <shlobj.h>
#include <cstdio>
#include <string>

int wmain(int argc, wchar_t** argv) {
    if (argc != 2) {
        std::fwprintf(stderr, L"Usage: TestShellAssociation.exe <pdf-path>\n");
        return 2;
    }

    const auto init = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    if (FAILED(init)) return static_cast<int>(init);

    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, nullptr, nullptr);

    PIDLIST_ABSOLUTE pidl = nullptr;
    const auto parse = SHParseDisplayName(argv[1], nullptr, &pidl, 0, nullptr);
    if (FAILED(parse)) {
        std::printf("SHParseDisplayName=0x%08X\n", static_cast<unsigned>(parse));
        CoUninitialize();
        return static_cast<int>(parse);
    }

    IShellFolder* folder = nullptr;
    PCITEMID_CHILD child = nullptr;
    const auto bind = SHBindToParent(pidl, IID_IShellFolder, reinterpret_cast<void**>(&folder), &child);
    if (FAILED(bind)) {
        std::printf("SHBindToParent=0x%08X\n", static_cast<unsigned>(bind));
        CoTaskMemFree(pidl);
        CoUninitialize();
        return static_cast<int>(bind);
    }

    IContextMenu* contextMenu = nullptr;
    const auto create = folder->GetUIObjectOf(nullptr, 1, &child, IID_IContextMenu, nullptr,
        reinterpret_cast<void**>(&contextMenu));
    std::printf("GetUIObjectOf=0x%08X\n", static_cast<unsigned>(create));
    if (FAILED(create)) {
        folder->Release();
        CoTaskMemFree(pidl);
        CoUninitialize();
        return static_cast<int>(create);
    }

    const auto nativeMenu = CreatePopupMenu();
    const auto query = contextMenu->QueryContextMenu(nativeMenu, 0, 1, 1000, CMF_NORMAL);
    std::printf("QueryContextMenu=0x%08X\n", static_cast<unsigned>(query));

    bool found = false;
    const auto count = GetMenuItemCount(nativeMenu);
    for (int index = 0; index < count; ++index) {
        wchar_t label[256] = {};
        MENUITEMINFOW item = {};
        item.cbSize = sizeof(item);
        item.fMask = MIIM_STRING | MIIM_SUBMENU;
        item.dwTypeData = label;
        item.cch = static_cast<UINT>(_countof(label));
        if (!GetMenuItemInfoW(nativeMenu, index, TRUE, &item)) continue;
        std::wprintf(L"Menu[%d]=%s submenu=%d\n", index, label, item.hSubMenu != nullptr);
        if (std::wstring(label).find(L"AvaPDF") != std::wstring::npos) found = true;
    }

    DestroyMenu(nativeMenu);
    contextMenu->Release();
    folder->Release();
    CoTaskMemFree(pidl);
    CoUninitialize();
    std::printf("AvaPDF shell association=%s\n", found ? "FOUND" : "NOT FOUND");
    return found && SUCCEEDED(query) ? 0 : 3;
}
