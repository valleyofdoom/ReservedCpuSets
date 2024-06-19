# ReservedCpuSets

[![Downloads](https://img.shields.io/github/downloads/valleyofdoom/ReservedCpuSets/total.svg)](https://github.com/valleyofdoom/ReservedCpuSets/releases)

![program-screenshot.png](/assets/img/program-screenshot.png)

This is an extension of the [WindowsIoT CSP Soft Real-Time Performance](https://learn.microsoft.com/en-us/windows/iot/iot-enterprise/soft-real-time/soft-real-time-device#use-mdm-bridge-wmi-provider-to-configure-the-windowsiot-csp) configuration and assumes you have read the documentation. ``SetRTCores`` essentially prevents interrupts and tasks from being scheduled on reserved cores so that you can isolate real-time applications from user and kernel-level disturbances by configuring affinity policies.

> [!IMPORTANT]
> Unexpected behavior occurs when a process affinity is set to reserved and unreserved CPUs. Ensure to set the affinity to either reserved or unreserved CPUs, not a combination of both.

## Usage

- Launch the program and select the CPUs you wish to reserve and save changes

- After a restart, verify whether the configuration is working as expected by assessing per-core usage while placing load on the CPU with a program such as [CpuStres](https://learn.microsoft.com/en-us/sysinternals/downloads/cpustres). The reserved cores should be underutilized compared to the unreserved cores

## Why a Separate Program?

This program aims to circumvent the limitations of the [PowerShell script](https://learn.microsoft.com/en-us/windows/iot/iot-enterprise/soft-real-time/soft-real-time-device#use-mdm-bridge-wmi-provider-to-configure-the-windowsiot-csp) in the documentation by:

1. Allowing customization of the bitmask instead of the configuration being limited to reserving the last N consecutive cores

2. Revert the changes

3. Adding support for earlier Windows 10 versions

## How It Works

Upon inspection of system changes while configuring ``SetRTCores`` to 11 with the PowerShell script, the registry key below was created. After a few minutes of reverse engineering, the explanation for the value is relatively simple (see examples).

```
[HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\kernel]
"ReservedCpuSets"=hex(3):FE,0F,00,00,00,00,00,00
```

Adding support for earlier versions of Windows 10 involves utilizing [NtSetSystemInformation](https://learn.microsoft.com/en-us/windows/win32/sysinfo/ntsetsysteminformation) as demonstrated in [CpuSet](https://github.com/zodiacon/WindowsInternals/tree/master/CpuSet) from Windows Internals.

### Example 1

``111111111110`` corresponds to the bitmask for reserving the last 11 cores on a 12 core system. Converting the bitmask to hexal little endian results in ``FFE``. Converting that value to hexal big endian results in ``FE0F``. Hence, the registry value of ``FE,0F,00,00,00,00,00,00``.

### Example 2

``11100000`` corresponds to the bitmask for reserving the last 3 cores on an 8 core system. Converting the bitmask to hexal little endian results in ``E0``. Converting that value to hexal big endian results in ``E0``. Hence, the registry value of ``E0,00,00,00,00,00,00,00``.

Since the registry value originates from an affinity bitmask, I have confirmed that it can be customized instead of the configuration being limited to the last N consecutive cores. Below is an example of ``10101010``.

![custom-bitmask.png](/assets/img/custom-bitmask.png)

## Reverting the Changes

The documentation does not provide information as to how the value can be reverted to default. Since we are aware that a registry value is changed and does not exist by default, we can determine whether deleting the registry entry reflects in a local kernel debugger such as WinDbg by reading ``KiReservedcpusets``.

### Default

```
Write-Host $obj.SetRTCores // 0

lkd> dd KiReservedcpusets L1
fffff807`216fdc10  00000000
```

### Setting SetRTCores to 3

```
Write-Host $obj.SetRTCores // 3

lkd> dd KiReservedcpusets L1
fffff807`216fdc10  000000e0
```

### Deleting the ``ReservedCpuSets`` registry entry

```
Write-Host $obj.SetRTCores // 0

lkd> dd KiReservedcpusets L1
fffff807`216fdc10  00000000
```

In the program, reverting the changes is equivalent to unchecking all CPUs then saving changes.
