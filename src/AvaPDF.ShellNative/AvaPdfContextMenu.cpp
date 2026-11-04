#include <windows.h>
#include <shobjidl.h>
#include <string>
#include <sstream>
#include <array>

namespace {
constexpr GUID CLSID_AvaPdfContextMenu =
    { 0x6f4a17d2, 0x6a4b, 0x4f1e, { 0xa6, 0xb3, 0x0a, 0x9a, 0x0d, 0xfd, 0x00, 0x01 } };

struct Command {
    const wchar_t* tool;
    const wchar_t* label;
};

constexpr std::array<Command, 9> Commands = {{
    { L"merge", L"合并 PDF" },
    { L"split", L"拆分 PDF" },
    { L"extract", L"提取页面" },
    { L"rotate", L"旋转页面" },
    { L"optimize", L"优化 PDF" },
    { L"watermark", L"添加水印" },
    { L"encrypt", L"加密 PDF" },
    { L"decrypt", L"解密 PDF" },
    { L"extracttext", L"提取文本" }
}};

class AvaPdfContextMenu final : public IContextMenu {
public:
    static LONG _serverReferences;

    AvaPdfContextMenu() : _references(1) { InterlockedIncrement(&_serverReferences); }

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** object) override {
        if (object == nullptr) return E_POINTER;
        *object = nullptr;
        if (riid == IID_IUnknown || riid == IID_IContextMenu) {
            *object = static_cast<IContextMenu*>(this);
            AddRef();
            return S_OK;
        }
        return E_NOINTERFACE;
    }

    ULONG STDMETHODCALLTYPE AddRef() override {
        InterlockedIncrement(&_serverReferences);
        return static_cast<ULONG>(InterlockedIncrement(&_references));
    }

    ULONG STDMETHODCALLTYPE Release() override {
        InterlockedDecrement(&_serverReferences);
        const auto remaining = static_cast<ULONG>(InterlockedDecrement(&_references));
        if (remaining == 0) delete this;
        return remaining;
    }

    HRESULT STDMETHODCALLTYPE QueryContextMenu(
        HMENU menu, UINT indexMenu, UINT firstCommand, UINT lastCommand, UINT flags) override {
        UNREFERENCED_PARAMETER(lastCommand);
        UNREFERENCED_PARAMETER(flags);
        const auto submenu = CreatePopupMenu();
        if (submenu == nullptr) return E_FAIL;

        for (UINT index = 0; index < Commands.size(); ++index) {
            MENUITEMINFOW item = {};
            item.cbSize = sizeof(item);
            item.fMask = MIIM_FTYPE | MIIM_STRING | MIIM_ID;
            item.fType = MFT_STRING;
            item.wID = firstCommand + index;
            item.dwTypeData = const_cast<wchar_t*>(Commands[index].label);
            item.cch = static_cast<UINT>(wcslen(Commands[index].label));
            if (!InsertMenuItemW(submenu, index, TRUE, &item)) return E_FAIL;
        }

        MENUITEMINFOW root = {};
        root.cbSize = sizeof(root);
        root.fMask = MIIM_FTYPE | MIIM_STRING | MIIM_SUBMENU;
        root.fType = MFT_STRING;
        root.hSubMenu = submenu;
        root.dwTypeData = const_cast<wchar_t*>(L"AvaPDF");
        root.cch = 6;
        return InsertMenuItemW(menu, indexMenu, TRUE, &root) ? S_OK : E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE InvokeCommand(LPCMINVOKECOMMANDINFO info) override {
        if (info == nullptr || info->lpVerb == nullptr) return E_POINTER;
        const auto index = FindCommand(info->lpVerb);
        if (index < 0) return E_INVALIDARG;

        wchar_t localAppData[MAX_PATH] = {};
        if (GetEnvironmentVariableW(L"LOCALAPPDATA", localAppData, MAX_PATH) == 0) return E_FAIL;
        std::wstringstream executable;
        executable << localAppData << L"\\Programs\\AvaPDF\\AvaPDF.Desktop.exe";
        const auto document = Utf8ToWide(info->lpDirectory);
        std::wstringstream arguments;
        arguments << L"--tool " << Commands[index].tool << L" --file \""
                  << document << L"\"";

        const auto result = ShellExecuteW(
            info->hwnd, L"open", executable.str().c_str(), arguments.str().c_str(),
            document.c_str(), SW_SHOWNORMAL);
        return reinterpret_cast<INT_PTR>(result) > 32 ? S_OK : E_FAIL;
    }

    HRESULT STDMETHODCALLTYPE GetCommandString(
        UINT_PTR, UINT, UINT* reserved, LPSTR name, UINT maximum) override {
        UNREFERENCED_PARAMETER(reserved);
        if (maximum > 0) name[0] = '\0';
        return S_OK;
    }

    static std::wstring Utf8ToWide(LPCSTR text) {
        if (text == nullptr) return L"";
        const auto length = MultiByteToWideChar(CP_UTF8, 0, text, -1, nullptr, 0);
        if (length <= 0) return L"";
        std::wstring result(static_cast<size_t>(length), L'\0');
        MultiByteToWideChar(CP_UTF8, 0, text, -1, result.data(), length);
        result.resize(static_cast<size_t>(length - 1));
        return result;
    }

    static int FindCommand(LPCSTR verb) {
        for (int index = 0; index < static_cast<int>(Commands.size()); ++index) {
            char tool[32] = {};
            const auto length = WideCharToMultiByte(CP_UTF8, 0, Commands[index].tool, -1, tool, 32, nullptr, nullptr);
            if (length > 0 && _stricmp(verb, tool) == 0) return index;
        }
        return -1;
    }

    LONG _references;
};

LONG AvaPdfContextMenu::_serverReferences = 0;

class AvaPdfClassFactory final : public IClassFactory {
public:
    static LONG _serverReferences;

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** object) override {
        if (object == nullptr) return E_POINTER;
        *object = nullptr;
        if (riid == IID_IUnknown || riid == IID_IClassFactory) {
            *object = static_cast<IClassFactory*>(this);
            AddRef();
            return S_OK;
        }
        return E_NOINTERFACE;
    }

    ULONG STDMETHODCALLTYPE AddRef() override {
        InterlockedIncrement(&_serverReferences);
        return static_cast<ULONG>(InterlockedIncrement(&_references));
    }

    ULONG STDMETHODCALLTYPE Release() override {
        InterlockedDecrement(&_serverReferences);
        const auto remaining = static_cast<ULONG>(InterlockedDecrement(&_references));
        if (remaining == 0) delete this;
        return remaining;
    }

    HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown* outer, REFIID riid, void** object) override {
        if (outer != nullptr) return CLASS_E_NOAGGREGATION;
        auto* menu = new (std::nothrow) AvaPdfContextMenu();
        if (menu == nullptr) return E_OUTOFMEMORY;
        const auto result = menu->QueryInterface(riid, object);
        menu->Release();
        return result;
    }

    HRESULT STDMETHODCALLTYPE LockServer(BOOL lock) override {
        if (lock) InterlockedIncrement(&_serverReferences);
        else InterlockedDecrement(&_serverReferences);
        return S_OK;
    }

private:
    LONG _references = 1;
};

LONG AvaPdfClassFactory::_serverReferences = 0;
}

extern "C" HRESULT STDAPICALLTYPE DllGetClassObject(
    REFCLSID classId, REFIID riid, void** object) {
    if (classId != CLSID_AvaPdfContextMenu) return CLASS_E_CLASSNOTAVAILABLE;
    auto* factory = new (std::nothrow) AvaPdfClassFactory();
    if (factory == nullptr) return E_OUTOFMEMORY;
    const auto result = factory->QueryInterface(riid, object);
    factory->Release();
    return result;
}

extern "C" HRESULT STDAPICALLTYPE DllCanUnloadNow() {
    return AvaPdfContextMenu::_serverReferences == 0 && AvaPdfClassFactory::_serverReferences == 0
        ? S_OK
        : S_FALSE;
}
