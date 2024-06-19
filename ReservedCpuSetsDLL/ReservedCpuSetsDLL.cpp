#include <iostream>
#include <Windows.h>
#include <string>

typedef enum _SYSTEM_INFORMATION_CLASS {
    SystemAllowedCpuSetsInformation = 168,
    SystemCpuSetInformation = 175,
    SystemCpuSetTagInformation = 176,
} SYSTEM_INFORMATION_CLASS;

extern "C" NTSTATUS NTAPI NtSetSystemInformation(_In_ SYSTEM_INFORMATION_CLASS SystemInformationClass, _In_reads_bytes_opt_(SystemInformationLength) PVOID SystemInformation, _In_ ULONG SystemInformationLength);

template<typename T>
int ParseCPUSet(T* pSet, const char* text, int& count) {
    bool last = false;

    for (int i = 0; !last && i < 32; i++) {
        auto comma = strchr(text, ',');

        if (comma == text) {
            return 1;
        }

        if (comma == nullptr) {
            comma = text + strlen(text);
            last = true;
        }

        size_t len = comma - text;
        bool hex = false;

        if (len > 2 && (::strncmp(text, "0x", 2) == 0 || ::strncmp(text, "0X", 2) == 0)) {
            hex = true;
        }

        int value = std::stoi(std::string(text + (hex ? 2 : 0), comma), nullptr, hex ? 16 : 10);
        pSet[i] = value;
        text = comma + 1;
        count = i + 1;
    }

    return 0;
}

int SetPrivilege(HANDLE hToken, PCTSTR lpszPrivilege, bool bEnablePrivilege) {
    TOKEN_PRIVILEGES tp;
    LUID luid;

    if (!::LookupPrivilegeValue(nullptr, lpszPrivilege, &luid)) {
        return 1;
    }

    tp.PrivilegeCount = 1;
    tp.Privileges[0].Luid = luid;
    tp.Privileges[0].Attributes = bEnablePrivilege ? SE_PRIVILEGE_ENABLED : 0;

    // enable the privilege or disable all privileges.

    if (!::AdjustTokenPrivileges(hToken, false, &tp, sizeof(TOKEN_PRIVILEGES), nullptr, nullptr)) {
        return 1;
    }

    if (::GetLastError() == ERROR_NOT_ALL_ASSIGNED) {
        return 1;
    }

    return 0;
}

extern "C" __declspec(dllexport) int SetSystemCpuSet(unsigned long mask) {
    std::string str = std::to_string(mask);
    const char* affinity = str.c_str();

    HANDLE hToken = nullptr;
    ::OpenProcessToken(::GetCurrentProcess(), TOKEN_ALL_ACCESS, &hToken);

    if (SetPrivilege(hToken, SE_INC_BASE_PRIORITY_NAME, 1)) {
        std::cerr << "Access denied. Try running from an elevated command prompt.\n";
        return 1;
    }
    ::CloseHandle(hToken);

    ULONG cpuSet[64];
    ULONG64 systemCpuSet[20];
    int count = 0;

    if (ParseCPUSet(systemCpuSet, affinity, count)) {
        std::cerr << "System CPU set list error\n";
        return 1;
    }

    auto pSet = systemCpuSet;
    if (count == 1 && cpuSet[0] == 0) {
        pSet = nullptr;
        count = 0;
    }

    if (::NtSetSystemInformation(SystemAllowedCpuSetsInformation, pSet, count * sizeof(ULONG64))) {
        std::cerr << "Failed to change system CPU set\n";
        return 1;
    }

    return 0;
}
