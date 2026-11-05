#include <windows.h>
#include <shobjidl.h>
#include <shlobj.h>
#include <cstdio>
#include <string>
#include <vector>

namespace {
constexpr GUID TestClsid = { 0x6f4a17d2, 0x6a4b, 0x4f1e, { 0xa6, 0xb3, 0x0a, 0x9a, 0x0d, 0xfd, 0x00, 0x01 } };

using DllGetClassObjectHandler = HRESULT(STDAPICALLTYPE*)(REFCLSID, REFIID, void**);
DllGetClassObjectHandler GetClassObject = nullptr;

struct Scenario {
    std::wstring Name;
    std::vector<std::wstring> Files;
    std::wstring ExpectedPrimary;
    bool ExpectMenu;
    int ExpectedItems = 2;
};

std::wstring MenuLabel(HMENU menu, int index) {
    wchar_t label[256] = {};
    MENUITEMINFOW item = {};
    item.cbSize = sizeof(item);
    item.fMask = MIIM_STRING | MIIM_SUBMENU;
    item.dwTypeData = label;
    item.cch = static_cast<UINT>(_countof(label));
    if (!GetMenuItemInfoW(menu, index, TRUE, &item)) return {};
    return label;
}

HMENU AvaPdfSubmenu(HMENU menu) {
    MENUITEMINFOW item = {};
    item.cbSize = sizeof(item);
    item.fMask = MIIM_SUBMENU;
    if (!GetMenuItemInfoW(menu, 0, TRUE, &item)) return nullptr;
    return item.hSubMenu;
}

IDataObject* CreateDataObject(const std::vector<std::wstring>& files) {
    std::vector<PIDLIST_ABSOLUTE> absolute;
    std::vector<PCITEMID_CHILD> children;
    for (const auto& file : files) {
        auto pidl = ILCreateFromPathW(file.c_str());
        if (pidl == nullptr) {
            std::wprintf(L"ILCreateFromPathW failed: %s\n", file.c_str());
            for (const auto allocated : absolute) ILFree(allocated);
            return nullptr;
        }
        absolute.push_back(pidl);
        children.push_back(ILFindLastID(pidl));
    }

    auto folder = ILClone(absolute[0]);
    if (folder == nullptr || !ILRemoveLastID(folder)) {
        std::printf("Folder PIDL preparation failed.\n");
        for (const auto allocated : absolute) ILFree(allocated);
        if (folder != nullptr) ILFree(folder);
        return nullptr;
    }

    IDataObject* data = nullptr;
    const auto result = SHCreateDataObject(folder, static_cast<UINT>(children.size()), children.data(), nullptr,
        IID_IDataObject, reinterpret_cast<void**>(&data));
    ILFree(folder);
    for (const auto allocated : absolute) ILFree(allocated);
    std::printf("SHCreateDataObject=0x%08X data=%p\n", static_cast<unsigned>(result), data);
    return SUCCEEDED(result) ? data : nullptr;
}

bool RunScenario(const Scenario& scenario) {
    IClassFactory* factory = nullptr;
    auto result = GetClassObject(TestClsid, IID_IClassFactory, reinterpret_cast<void**>(&factory));
    IContextMenu* menu = nullptr;
    if (SUCCEEDED(result)) {
        result = factory->CreateInstance(nullptr, IID_IContextMenu, reinterpret_cast<void**>(&menu));
        factory->Release();
    }
    if (FAILED(result)) {
        std::printf("DllGetClassObject=0x%08X\n", static_cast<unsigned>(result));
        return false;
    }

    IShellExtInit* init = nullptr;
    result = menu->QueryInterface(IID_IShellExtInit, reinterpret_cast<void**>(&init));
    if (FAILED(result)) {
        menu->Release();
        return false;
    }

    auto data = CreateDataObject(scenario.Files);
    if (data == nullptr) {
        std::wprintf(L"CreateDataObject failed for %s\n", scenario.Name.c_str());
        init->Release();
        menu->Release();
        return false;
    }
    result = init->Initialize(nullptr, data, nullptr);
    data->Release();
    init->Release();
    if (FAILED(result)) {
        std::printf("Initialize=0x%08X\n", static_cast<unsigned>(result));
        menu->Release();
        return false;
    }

    const auto nativeMenu = CreatePopupMenu();
    result = menu->QueryContextMenu(nativeMenu, 0, 1, 100, CMF_NORMAL);
    const auto count = GetMenuItemCount(nativeMenu);
    std::wprintf(L"%s query=0x%08X rootCount=%d\n", scenario.Name.c_str(), static_cast<unsigned>(result), count);
    bool passed = SUCCEEDED(result);
    if (!scenario.ExpectMenu) {
        passed = passed && count == 0;
    }
    else {
        passed = passed && count == 1 && MenuLabel(nativeMenu, 0) == L"AvaPDF";
        const auto submenu = AvaPdfSubmenu(nativeMenu);
        passed = passed && submenu != nullptr && GetMenuItemCount(submenu) == scenario.ExpectedItems;
        if (submenu != nullptr && GetMenuItemCount(submenu) > 0) {
            const auto primary = MenuLabel(submenu, 0);
            const auto expected = scenario.ExpectedItems == 1 ? L"更多工具…" : scenario.ExpectedPrimary;
            std::wprintf(L"primary=%s expected=%s\n", primary.c_str(), expected.c_str());
            passed = passed && primary == expected;
        }
    }

    std::wprintf(L"%s: %s\n", scenario.Name.c_str(), passed ? L"PASS" : L"FAIL");
    DestroyMenu(nativeMenu);
    menu->Release();
    return passed;
}
}

int wmain(int argc, wchar_t** argv) {
    if (argc != 3) {
        std::fwprintf(stderr, L"Usage: TestShellMenu.exe <test-directory> <shell-dll>\n");
        return 2;
    }
    const auto module = LoadLibraryW(argv[2]);
    if (module == nullptr) return 2;
    GetClassObject = reinterpret_cast<DllGetClassObjectHandler>(GetProcAddress(module, "DllGetClassObject"));
    if (GetClassObject == nullptr) {
        FreeLibrary(module);
        return 2;
    }

    const auto init = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    if (FAILED(init)) return static_cast<int>(init);

    const auto directory = std::wstring(argv[1]);
    CreateDirectoryW(directory.c_str(), nullptr);
    const auto files = std::vector<std::wstring> {
        directory + L"\\one.pdf", directory + L"\\two.pdf", directory + L"\\one.docx",
        directory + L"\\two.txt", directory + L"\\one.png", directory + L"\\two.png",
        directory + L"\\one.exe"
    };
    for (const auto& file : files) {
        const auto handle = CreateFileW(file.c_str(), GENERIC_WRITE, FILE_SHARE_READ, nullptr,
            CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (handle == INVALID_HANDLE_VALUE) {
            CoUninitialize();
            return 2;
        }
        CloseHandle(handle);
    }

    const std::vector<Scenario> scenarios = {
        { L"single PDF", { files[0] }, L"提取文本", true },
        { L"multiple PDFs", { files[0], files[1] }, L"合并 PDF", true },
        { L"single document", { files[2] }, L"转换为 PDF", true },
        { L"multiple documents", { files[2], files[3] }, L"批量转换为 PDF", true },
        { L"single image", { files[4] }, L"转换为 PDF", true },
        { L"multiple images", { files[4], files[5] }, L"合并为 PDF", true },
        { L"document and image", { files[2], files[4] }, L"批量转换为 PDF", true },
        { L"PDF and image", { files[0], files[4] }, L"", true, 1 },
        { L"unsupported file", { files[6] }, L"", false }
    };

    auto failures = 0;
    for (const auto& scenario : scenarios) {
        if (!RunScenario(scenario)) ++failures;
    }
    CoUninitialize();
    FreeLibrary(module);
    std::printf("Shell menu scenario failures=%d\n", failures);
    return failures == 0 ? 0 : 1;
}
