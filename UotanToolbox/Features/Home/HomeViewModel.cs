﻿using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Microsoft.VisualBasic;
using ReactiveUI;
using SukiUI.Controls;
using SukiUI.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UotanToolbox.Common;
using UotanToolbox.Features.Components;

namespace UotanToolbox.Features.Home;

public partial class HomeViewModel : MainPageBase
{
    [ObservableProperty]
    private string _progressDisk = "0", _memLevel = "0", _status = "--", _bLStatus = "--",
    _vABStatus = "--", _codeName = "--", _vNDKVersion = "--", _cPUCode = "--",
    _powerOnTime = "--", _deviceBrand = "--", _deviceModel = "--", _androidSDK = "--",
    _cPUABI = "--", _displayHW = "--", _density = "--", _boardID = "--", _platform = "--",
    _compile = "--", _kernel = "--", _selectedSimpleContent = null, _diskType = "--",
    _batteryLevel = "0", _batteryInfo = "--", _useMem = "--", _diskInfo = "--";
    [ObservableProperty] private bool _IsConnecting;
    [ObservableProperty] private bool _commonDevicesList;
    [ObservableProperty] private static AvaloniaList<string> _simpleContent;

    public IAvaloniaReadOnlyList<MainPageBase> DemoPages { get; }

    [ObservableProperty] private bool _animationsEnabled;
    [ObservableProperty] private MainPageBase _activePage;
    [ObservableProperty] private bool _windowLocked = false;

    private static string GetTranslation(string key) => FeaturesHelper.GetTranslation(key);
    public HomeViewModel() : base(GetTranslation("Sidebar_HomePage"), MaterialIconKind.HomeOutline, int.MinValue)
    {
        _ = CheckEnvironment();
        _ = CheckDeviceList();
        this.WhenAnyValue(x => x.SelectedSimpleContent)
            .Subscribe(option =>
            {
                if (option != null && option != Global.thisdevice && SimpleContent != null && SimpleContent.Count != 0)
                {
                    Global.thisdevice = option;
                    _ = ConnectCore();
                }
            });
    }

    public async Task CheckEnvironment()
    {
        string filepath1 = "";
        string filepath2 = "";
        if (Global.System == "Windows")
        {
            filepath1 = Path.Combine(Global.bin_path, "platform-tools", "adb.exe");
            filepath2 = Path.Combine(Global.bin_path, "platform-tools", "fastboot.exe");
        }
        else
        {
            filepath1 = Path.Combine(Global.bin_path, "platform-tools", "adb");
            filepath2 = Path.Combine(Global.bin_path, "platform-tools", "fastboot");
        }
        if (!File.Exists(filepath1) || !File.Exists(filepath2))
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await SukiHost.ShowDialogAsync(new ErrorDialog(GetTranslation("Home_Missing")));
                Process.GetCurrentProcess().Kill();
            });
        }
    }

    public async Task<bool> GetDevicesList()
    {
        string[] devices = await GetDevicesInfo.DevicesList();
        if (devices.Length != 0)
        {
            Global.deviceslist = new AvaloniaList<string>(devices);
            SimpleContent = Global.deviceslist;
            if (SelectedSimpleContent == null || !string.Join("", SimpleContent).Contains(SelectedSimpleContent))
            {
                if (Global.thisdevice != null && Global.deviceslist.Contains(Global.thisdevice))
                {
                    SelectedSimpleContent = Global.thisdevice;
                }
                else
                {
                    SelectedSimpleContent = SimpleContent.First();
                }
            }
            return true;
        }
        else
        {
            SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
            return false;
        }
    }

    public async Task ConnectCore()
    {
        IsConnecting = true;
        MainViewModel sukiViewModel = GlobalData.MainViewModelInstance;
        Dictionary<string, string> DevicesInfo = await GetDevicesInfo.DevicesInfo(Global.thisdevice);
        Status = sukiViewModel.Status = DevicesInfo["Status"];
        BLStatus = sukiViewModel.BLStatus = DevicesInfo["BLStatus"];
        VABStatus = sukiViewModel.VABStatus = DevicesInfo["VABStatus"];
        CodeName = sukiViewModel.CodeName = DevicesInfo["CodeName"];
        VNDKVersion = DevicesInfo["VNDKVersion"];
        CPUCode = DevicesInfo["CPUCode"];
        PowerOnTime = DevicesInfo["PowerOnTime"];
        DeviceBrand = DevicesInfo["DeviceBrand"];
        DeviceModel = DevicesInfo["DeviceModel"];
        AndroidSDK = DevicesInfo["AndroidSDK"];
        CPUABI = DevicesInfo["CPUABI"];
        DisplayHW = DevicesInfo["DisplayHW"];
        Density = DevicesInfo["Density"];
        DiskType = DevicesInfo["DiskType"];
        BoardID = DevicesInfo["BoardID"];
        Platform = DevicesInfo["Platform"];
        Compile = DevicesInfo["Compile"];
        Kernel = DevicesInfo["Kernel"];
        BatteryLevel = DevicesInfo["BatteryLevel"];
        BatteryInfo = DevicesInfo["BatteryInfo"];
        MemLevel = DevicesInfo["MemLevel"];
        UseMem = DevicesInfo["UseMem"];
        DiskInfo = DevicesInfo["DiskInfo"];
        ProgressDisk = DevicesInfo["ProgressDisk"];
        IsConnecting = false;
    }

    public async Task<bool> ListChecker()
    {
        if (Global.checkdevice)
        {
            string[] devices = await GetDevicesInfo.DevicesList();
            if (devices.Length != 0)
            {
                var tempDeviceslist = new AvaloniaList<string>(devices);
                if (Global.deviceslist != null)
                {
                    if (Global.deviceslist.SequenceEqual(tempDeviceslist) != true)
                        return true;
                }
                else if (Global.deviceslist == null)
                    return true;
            }
            else
            {
                if (Global.deviceslist != null && Global.deviceslist.Count != 0)
                {
                    Global.deviceslist.Clear();
                    Global.thisdevice = null;
                    SimpleContent = null;
                    IsConnecting = false;
                    await SukiHost.ShowToast(GetTranslation("Home_Prompt"), GetTranslation("Home_Disconnected"), NotificationType.Warning);
                    MainViewModel sukiViewModel = GlobalData.MainViewModelInstance;
                    Status = sukiViewModel.Status = BLStatus = sukiViewModel.BLStatus = VABStatus = sukiViewModel.VABStatus = CodeName = sukiViewModel.CodeName = "--";
                    VNDKVersion = CPUCode = PowerOnTime = DeviceBrand = DeviceModel = AndroidSDK = CPUABI = DisplayHW = Density = DiskType = BoardID = Platform = Compile = Kernel = BatteryInfo = UseMem = DiskInfo = "--";
                    BatteryLevel = MemLevel = ProgressDisk = "0";
                }
            }
        }
        return false;
    }

    public async Task CheckDeviceList()
    {
        while (true)
        {
            if (await ListChecker() == true)
            {
                CommonDevicesList = true;
                await GetDevicesList();
                CommonDevicesList = false;
            }
            await Task.Delay(1000);
        }
    }

    [RelayCommand]
    public async Task FreshDeviceList()
    {
        AvaloniaList<string> OldDeviceList = Global.deviceslist;
        if (await GetDevicesList() && Global.thisdevice != null && string.Join("", Global.deviceslist).Contains(Global.thisdevice))
        {
            if (OldDeviceList != Global.deviceslist)
            {
                CommonDevicesList = true;
                await ConnectCore();
                CommonDevicesList = false;
            }
        }
    }

    private async Task SystemControl(string shell)
    {
        if (await GetDevicesInfo.SetDevicesInfoLittle())
        {
            MainViewModel sukiViewModel = GlobalData.MainViewModelInstance;
            if (sukiViewModel.Status == GetTranslation("Home_System"))
            {
                await CallExternalProgram.ADB($"-s {Global.thisdevice} {shell}");
            }
            else
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_OpenADB")), allowBackgroundClose: true);
            }
        }
        else
        {
            SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
        }
    }

    private async Task ADBControl(string shell)
    {
        if (await GetDevicesInfo.SetDevicesInfoLittle())
        {
            MainViewModel sukiViewModel = GlobalData.MainViewModelInstance;
            if (sukiViewModel.Status == GetTranslation("Home_System") || sukiViewModel.Status == GetTranslation("Home_Recovery") || sukiViewModel.Status == GetTranslation("Home_Sideload"))
            {
                await CallExternalProgram.ADB($"-s {Global.thisdevice} {shell}");
            }
            else
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_EnterRecOrOpenADB")), allowBackgroundClose: true);
            }
        }
        else
        {
            SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
        }
    }

    private async Task FastbootControl(string shell)
    {
        if (await GetDevicesInfo.SetDevicesInfoLittle())
        {
            MainViewModel sukiViewModel = GlobalData.MainViewModelInstance;
            if (sukiViewModel.Status == GetTranslation("Home_Fastboot") || sukiViewModel.Status == GetTranslation("Home_Fastbootd"))
            {
                await CallExternalProgram.Fastboot($"-s {Global.thisdevice} {shell}");
            }
            else
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_EnterFastboot")), allowBackgroundClose: true);
            }
        }
        else
        {
            SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
        }
    }


    [RelayCommand]
    public async Task Back() => await SystemControl("shell input keyevent 4");

    [RelayCommand]
    public async Task Home() => await SystemControl("shell input keyevent 3");

    [RelayCommand]
    public async Task Mul() => await SystemControl("shell input keyevent 187");

    [RelayCommand]
    public async Task Lock() => await SystemControl("shell input keyevent 26");

    [RelayCommand]
    public async Task VolU() => await SystemControl("shell input keyevent 24");

    [RelayCommand]
    public async Task VolD() => await SystemControl("shell input keyevent 25");

    [RelayCommand]
    public async Task Mute() => await SystemControl("shell input keyevent 164");

    [RelayCommand]
    public async Task SC()
    {
        string pngname = String.Format($"{DateAndTime.Now:yyyy-MM-dd_HH-mm-ss}");
        await SystemControl($"shell /system/bin/screencap -p /sdcard/{pngname}.png");
        await SukiHost.ShowToast(GetTranslation("Home_Succeeded"), $"{GetTranslation("Home_Saved")} {pngname}.png {GetTranslation("Home_ToStorage")}", NotificationType.Success);
    }

    [RelayCommand]
    public async Task AReboot() => await ADBControl("reboot");

    [RelayCommand]
    public async Task ARRec() => await ADBControl("reboot recovery");

    [RelayCommand]
    public async Task ARSide()
    {
        if (await GetDevicesInfo.SetDevicesInfoLittle())
        {
            MainViewModel sukiViewModel = GlobalData.MainViewModelInstance;
            if (sukiViewModel.Status == GetTranslation("Home_System") || sukiViewModel.Status == GetTranslation("Home_Recovery") || sukiViewModel.Status == GetTranslation("Home_Sideload"))
            {
                string output = await CallExternalProgram.ADB($"-s {Global.thisdevice} shell twrp sideload");
                if (output.Contains("not found"))
                {
                    await CallExternalProgram.ADB($"-s {Global.thisdevice} reboot sideload");
                }
            }
            else
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_EnterRecOrOpenADB")), allowBackgroundClose: true);
            }
        }
        else
        {
            SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
        }
    }

    [RelayCommand]
    public async Task ARBoot() => await ADBControl("reboot bootloader");

    [RelayCommand]
    public async Task ARFast() => await ADBControl("reboot fastboot");

    [RelayCommand]
    public async Task AREDL() => await ADBControl("reboot edl");

    [RelayCommand]
    public async Task FReboot() => await FastbootControl("reboot");

    [RelayCommand]
    public async Task FRRec()
    {
        if (await GetDevicesInfo.SetDevicesInfoLittle())
        {
            MainViewModel sukiViewModel = GlobalData.MainViewModelInstance;
            if (sukiViewModel.Status == GetTranslation("Home_Fastboot") || sukiViewModel.Status == GetTranslation("Home_Fastbootd"))
            {
                string output = await CallExternalProgram.Fastboot($"-s {Global.thisdevice} oem reboot-recovery");
                if (output.Contains("unknown command"))
                {
                    await CallExternalProgram.Fastboot($"-s {Global.thisdevice} flash misc {Global.runpath}/Image/misc.img");
                    await CallExternalProgram.Fastboot($"-s {Global.thisdevice} reboot");
                }
                else
                {
                    await CallExternalProgram.Fastboot($"-s {Global.thisdevice} reboot recovery");
                }
            }
            else
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_EnterFastboot")), allowBackgroundClose: true);
            }
        }
        else
        {
            SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
        }
    }

    [RelayCommand]
    public async Task FRShut()
    {
        if (await GetDevicesInfo.SetDevicesInfoLittle())
        {
            MainViewModel sukiViewModel = GlobalData.MainViewModelInstance;
            if (sukiViewModel.Status == GetTranslation("Home_Fastboot"))
            {
                string output = await CallExternalProgram.Fastboot($"-s {Global.thisdevice} oem poweroff");
                if (output.Contains("unknown command"))
                {
                    SukiHost.ShowDialog(new PureDialog(GetTranslation("Home_NotSupported")), allowBackgroundClose: true);
                }
                else
                {
                    SukiHost.ShowDialog(new PureDialog(GetTranslation("Home_Successful")), allowBackgroundClose: true);
                }
            }
            else
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_EnterFastboot")), allowBackgroundClose: true);
            }
        }
        else
        {
            SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
        }
    }

    [RelayCommand]
    public async Task FRBoot() => await FastbootControl("reboot-bootloader");

    [RelayCommand]
    public async Task FRFast() => await FastbootControl("reboot-fastboot");

    [RelayCommand]
    public async Task FREDL() => await FastbootControl("oem edl");
}