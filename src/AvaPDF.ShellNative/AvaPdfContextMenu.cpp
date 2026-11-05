#include <windows.h>
#include <shobjidl.h>
#include <shlwapi.h>
#include <string>
#include <vector>
#include <algorithm>
#include <sstream>

namespace {
constexpr GUID CLSID_AvaPdfContextMenu =
    { 0x6f4a17d2, 0x6a4b, 0x4f1e, { 0xa6, 0xb3, 0x0a, 0x9a, 0x0d, 0xfd, 0x00, 0x01 } };

enum class ShellAction {
    More,
    ExtractText,
    Merge,
    Convert,
    Images
};

struct Command {
    ShellAction Action;
    std::wstring Label;
};

bool HasExtension(const std::wstring& path, const wchar_t* extension) {
    if (path.size() < wcslen(extension)) return false;
    return _wcsicmp(path.c_str() + path.size() - wcslen(extension), extension) == 0;
}

bool IsPdf(const std::wstring& path) { return HasExtension(path, L".pdf"); }

bool IsImage(const std::wstring& path) {
    static constexpr const wchar_t* extensions[] = {
        L".jpg", L".jpeg", L".png", L".bmp", L".gif", L".webp", L".tif", L".tiff", L".svg"
    };
    for (const auto extension : extensions) {
        if (HasExtension(path, extension)) return true;
    }
    return false;
}

bool IsConvertibleDocument(const std::wstring& path) {
    static constexpr const wchar_t* extensions[] = {
        L".doc", L".docx", L".docm", L".dotx", L".dotm",
        L".xls", L".xlsx", L".xlsm", L".xltx", L".xltm",
        L".ppt", L".pptx", L".pptm", L".md", L".markdown",
        L".html", L".htm", L".rtf", L".txt"
    };
    for (const auto extension : extensions) {
        if (HasExtension(path, extension)) return true;
    }
    return false;
}

std::wstring JsonEscape(const std::wstring& value) {
    std::wstring escaped;
    escaped.reserve(value.size() + 16);
    for (const auto character : value) {
        switch (character) {
        case L'\\': escaped += L"\\\\"; break;
        case L'"': escaped += L"\\\""; break;
        case L'\b': escaped += L"\\b"; break;
        case L'\f': escaped += L"\\f"; break;
        case L'\n': escaped += L"\\n"; break;
        case L'\r': escaped += L"\\r"; break;
        case L'\t': escaped += L"\\t"; break;
        default:
            if (character < 0x20) {
                wchar_t buffer[7] = {};
                swprintf_s(buffer, L"\\u%04x", static_cast<unsigned>(character));
                escaped += buffer;
            }
            else {
                escaped += character;
            }
            break;
        }
    }
    return escaped;
}

std::string Utf8FromWide(const std::wstring& value) {
    if (value.empty()) return {};
    const auto length = WideCharToMultiByte(CP_UTF8, 0, value.data(), static_cast<int>(value.size()), nullptr, 0, nullptr, nullptr);
    if (length <= 0) return {};
    std::string result(static_cast<size_t>(length), '\0');
    WideCharToMultiByte(CP_UTF8, 0, value.data(), static_cast<int>(value.size()), result.data(), length, nullptr, nullptr);
    return result;
}

class AvaPdfContextMenu final : public IContextMenu, public IShellExtInit {
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
        if (riid == IID_IShellExtInit) {
            *object = static_cast<IShellExtInit*>(this);
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

    HRESULT STDMETHODCALLTYPE Initialize(PCIDLIST_ABSOLUTE, IDataObject* data, HKEY) override {
        _documents.clear();
        if (data == nullptr) return E_INVALIDARG;

        FORMATETC format = { CF_HDROP, nullptr, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };
        STGMEDIUM medium = {};
        const auto result = data->GetData(&format, &medium);
        if (FAILED(result)) return result;

        const auto drop = static_cast<HDROP>(medium.hGlobal);
        const auto count = DragQueryFileW(drop, 0xFFFFFFFF, nullptr, 0);
        for (UINT index = 0; index < count; ++index) {
            const auto length = DragQueryFileW(drop, index, nullptr, 0);
            std::wstring path(length, L'\0');
            if (DragQueryFileW(drop, index, path.data(), length + 1) != 0) {
                _documents.push_back(path);
            }
        }
        ReleaseStgMedium(&medium);
        std::sort(_documents.begin(), _documents.end(), [](const std::wstring& left, const std::wstring& right) {
            return StrCmpLogicalW(left.c_str(), right.c_str()) < 0;
        });
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE QueryContextMenu(
        HMENU menu, UINT indexMenu, UINT firstCommand, UINT lastCommand, UINT flags) override {
        UNREFERENCED_PARAMETER(lastCommand);
        if ((flags & CMF_DEFAULTONLY) != 0) return S_OK;

        BuildCommands();
        if (_commands.empty()) return S_OK;

        const auto submenu = CreatePopupMenu();
        if (submenu == nullptr) return E_FAIL;
        for (UINT index = 0; index < _commands.size(); ++index) {
            MENUITEMINFOW item = {};
            item.cbSize = sizeof(item);
            item.fMask = MIIM_FTYPE | MIIM_STRING | MIIM_ID;
            item.fType = MFT_STRING;
            item.wID = firstCommand + index;
            item.dwTypeData = _commands[index].Label.data();
            item.cch = static_cast<UINT>(_commands[index].Label.size());
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
        if (_documents.empty() && info->lpDirectory != nullptr) {
            _documents.push_back(Utf8ToWide(info->lpDirectory));
        }
        BuildCommands();
        const auto index = FindCommand(info->lpVerb);
        if (index < 0) return E_INVALIDARG;

        wchar_t registeredExecutable[MAX_PATH] = {};
        DWORD executableBytes = sizeof(registeredExecutable);
        const auto registryResult = RegGetValueW(
            HKEY_CURRENT_USER,
            L"Software\\Classes\\CLSID\\{6F4A17D2-6A4B-4F1E-A6B3-0A9A0DFD0001}",
            L"AppExecutable", RRF_RT_REG_SZ, nullptr, registeredExecutable, &executableBytes);
        std::wstring executable;
        if (registryResult == ERROR_SUCCESS) {
            executable = registeredExecutable;
        }
        else {
            wchar_t localAppData[MAX_PATH] = {};
            if (GetEnvironmentVariableW(L"LOCALAPPDATA", localAppData, MAX_PATH) == 0) return E_FAIL;
            std::wstringstream fallback;
            fallback << localAppData << L"\\Programs\\AvaPDF\\AvaPDF.Desktop.exe";
            executable = fallback.str();
        }

        const auto job = WriteJob(_commands[index].Action);
        if (job.empty()) return E_FAIL;
        std::wstringstream arguments;
        arguments << L"--job \"" << job << L"\"";
        auto workingDirectory = _documents[0];
        const auto separator = workingDirectory.find_last_of(L"\\/");
        if (separator != std::wstring::npos) workingDirectory.erase(separator);
        const auto result = ShellExecuteW(
            info->hwnd, L"open", executable.c_str(), arguments.str().c_str(),
            workingDirectory.c_str(), SW_SHOWNORMAL);
        if (reinterpret_cast<INT_PTR>(result) <= 32) {
            DeleteFileW(job.c_str());
            return E_FAIL;
        }
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetCommandString(
        UINT_PTR, UINT, UINT* reserved, LPSTR name, UINT maximum) override {
        UNREFERENCED_PARAMETER(reserved);
        if (maximum > 0) name[0] = '\0';
        return S_OK;
    }

private:
    static std::wstring Utf8ToWide(LPCSTR text) {
        if (text == nullptr) return L"";
        const auto length = MultiByteToWideChar(CP_UTF8, 0, text, -1, nullptr, 0);
        if (length <= 0) return L"";
        std::wstring result(static_cast<size_t>(length), L'\0');
        MultiByteToWideChar(CP_UTF8, 0, text, -1, result.data(), length);
        result.resize(static_cast<size_t>(length - 1));
        return result;
    }

    void BuildCommands() {
        _commands.clear();
        if (_documents.empty()) return;

        const auto allPdf = std::all_of(_documents.begin(), _documents.end(), IsPdf);
        const auto allImages = std::all_of(_documents.begin(), _documents.end(), IsImage);
        const auto allConvertible = std::all_of(_documents.begin(), _documents.end(), [](const std::wstring& path) {
            return IsImage(path) || IsConvertibleDocument(path);
        });
        const auto allSupported = std::all_of(_documents.begin(), _documents.end(), [](const std::wstring& path) {
            return IsPdf(path) || IsImage(path) || IsConvertibleDocument(path);
        });
        if (!allSupported) return;

        if (allPdf) {
            if (_documents.size() == 1) _commands.push_back({ ShellAction::ExtractText, L"提取文本" });
            else _commands.push_back({ ShellAction::Merge, L"合并 PDF" });
        }
        else if (allImages) {
            _commands.push_back({ ShellAction::Images, _documents.size() == 1 ? L"转换为 PDF" : L"合并为 PDF" });
        }
        else if (allConvertible) {
            _commands.push_back({ ShellAction::Convert, _documents.size() == 1 ? L"转换为 PDF" : L"批量转换为 PDF" });
        }
        _commands.push_back({ ShellAction::More, L"更多工具…" });
    }

    int FindCommand(LPCSTR verb) const {
        if (IS_INTRESOURCE(verb)) {
            const auto index = static_cast<int>(LOWORD(verb));
            return index < static_cast<int>(_commands.size()) ? index : -1;
        }
        for (int index = 0; index < static_cast<int>(_commands.size()); ++index) {
            const auto name = ActionName(_commands[index].Action);
            if (_stricmp(verb, name) == 0) return index;
        }
        return -1;
    }

    static const char* ActionName(ShellAction action) {
        switch (action) {
        case ShellAction::ExtractText: return "extracttext";
        case ShellAction::Merge: return "merge";
        case ShellAction::Convert: return "convert";
        case ShellAction::Images: return "images";
        default: return "more";
        }
    }

    std::wstring WriteJob(ShellAction action) const {
        wchar_t temp[MAX_PATH] = {};
        if (GetTempPathW(MAX_PATH, temp) == 0) return {};
        std::wstring jobDirectory = std::wstring(temp) + L"AvaPDF\\Jobs";
        const auto root = std::wstring(temp) + L"AvaPDF";
        if (!CreateDirectoryW(root.c_str(), nullptr) && GetLastError() != ERROR_ALREADY_EXISTS) return {};
        if (!CreateDirectoryW(jobDirectory.c_str(), nullptr) && GetLastError() != ERROR_ALREADY_EXISTS) return {};

        GUID guid = {};
        if (FAILED(CoCreateGuid(&guid))) return {};
        wchar_t guidText[64] = {};
        if (StringFromGUID2(guid, guidText, static_cast<int>(_countof(guidText))) == 0) return {};
        const auto path = jobDirectory + L"\\AvaPDF-" + guidText + L".json";

        std::wstring json = L"{\"tool\":";
        switch (action) {
        case ShellAction::ExtractText: json += L"\"extracttext\""; break;
        case ShellAction::Merge: json += L"\"merge\""; break;
        case ShellAction::Images: json += L"\"images\""; break;
        default: json += L"null"; break;
        }
        json += L",\"autoRun\":";
        json += action == ShellAction::ExtractText || action == ShellAction::Convert
            || (action == ShellAction::Images && _documents.size() == 1) ? L"true" : L"false";
        json += L",\"files\":[";
        for (size_t index = 0; index < _documents.size(); ++index) {
            if (index > 0) json += L",";
            json += L"\"" + JsonEscape(_documents[index]) + L"\"";
        }
        json += L"]}";

        const auto utf8 = Utf8FromWide(json);
        const auto file = CreateFileW(path.c_str(), GENERIC_WRITE, 0, nullptr, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (file == INVALID_HANDLE_VALUE) return {};
        DWORD written = 0;
        const auto success = WriteFile(file, utf8.data(), static_cast<DWORD>(utf8.size()), &written, nullptr)
            && written == utf8.size();
        CloseHandle(file);
        if (!success) {
            DeleteFileW(path.c_str());
            return {};
        }
        return path;
    }

    LONG _references;
    std::vector<std::wstring> _documents;
    std::vector<Command> _commands;
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
