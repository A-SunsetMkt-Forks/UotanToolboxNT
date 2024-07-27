﻿using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using SukiUI.Controls;
using SukiUI.Enums;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UotanToolbox.Common;
using UotanToolbox.Features.Components;

namespace UotanToolbox.Features.Appmgr;

public partial class AppmgrViewModel : MainPageBase
{
    [ObservableProperty]
    private ObservableCollection<ApplicationInfo> applications = [];
    [ObservableProperty]
    private bool isBusy = false, hasItems = false;
    [ObservableProperty]
    private bool isSystemAppDisplayed = false, isInstalling = false;
    [ObservableProperty]
    private string _apkFile;
    private static string GetTranslation(string key) => FeaturesHelper.GetTranslation(key);
    public AppmgrViewModel() : base(GetTranslation("Sidebar_Appmgr"), MaterialIconKind.ViewGridPlusOutline, -700)
    {
    }

    [RelayCommand]
    public async Task Connect()
    {
        HasItems = false;
        MainViewModel sukiViewModel = GlobalData.MainViewModelInstance;
        IsBusy = true;
        await Task.Run(async () =>
        {
            if (!await GetDevicesInfo.SetDevicesInfoLittle())
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
                });
                IsBusy = false; return;
            }
            string fullApplicationsList;
            if (!IsSystemAppDisplayed)
                fullApplicationsList = await CallExternalProgram.ADB($"-s {Global.thisdevice} shell pm list packages -3");
            else
                fullApplicationsList = await CallExternalProgram.ADB($"-s {Global.thisdevice} shell pm list packages");
            if (fullApplicationsList.Contains("cannot connect to daemon"))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_DeviceFailedToConnect")), allowBackgroundClose: true);
                });
                IsBusy = false; return;
            }
            if (!(sukiViewModel.Status == GetTranslation("Home_System")))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SukiHost.ShowDialog(new PureDialog(GetTranslation("Appmgr_PleaseExecuteInSystem")), allowBackgroundClose: true);
                });
                IsBusy = false; return;
            }
            var lines = fullApplicationsList.Split(separatorArray, StringSplitOptions.RemoveEmptyEntries);
            HasItems = lines.Length > 0;
            var applicationInfosTasks = lines.Select(async line =>
            {
                var packageName = ExtractPackageName(line);
                if (string.IsNullOrEmpty(packageName)) return null;
                var combinedOutput = await CallExternalProgram.ADB($"-s {Global.thisdevice} shell dumpsys package {packageName}");
                var splitOutput = combinedOutput.Split('\n', ' ');
                var otherInfo = GetVersionName(splitOutput) + " | " + GetInstalledDate(splitOutput) + " | " + GetSdkVersion(splitOutput);
                return new ApplicationInfo { Name = packageName, OtherInfo = otherInfo };
            });
            ApplicationInfo[] allApplicationInfos = await Task.WhenAll(applicationInfosTasks);
            var applicationInfos = allApplicationInfos.Where(info => info != null)
                                                     .OrderByDescending(app => app.Size)
                                                     .ThenBy(app => app.Name)
                                                     .ToList();
            Applications = new ObservableCollection<ApplicationInfo>(applicationInfos);
            IsBusy = false;
        });


        static string ExtractPackageName(string line)
        {
            var parts = line.Split(':');
            if (parts.Length < 2) return null;
            var packageNamePart = parts[1];
            var packageNameStartIndex = packageNamePart.LastIndexOf('/') + 1;
            return packageNameStartIndex < packageNamePart.Length
                ? packageNamePart.Substring(packageNameStartIndex)
                : null;
        }
    }

    [RelayCommand]
    public async Task InstallApk()
    {
        IsInstalling = true;
        if (!string.IsNullOrEmpty(ApkFile))
        {
            if (!await GetDevicesInfo.SetDevicesInfoLittle())
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
                });
                IsInstalling = false; return;
            }
            var fileArray = ApkFile.Split("|||");
            for (int i = 0; i < fileArray.Length; i++)
            {
                if (!string.IsNullOrEmpty(fileArray[i]))
                {
                    string output = await CallExternalProgram.ADB($"-s {Global.thisdevice} install -r \"{fileArray[i]}\"");
                    if (output.Contains("Success"))
                    {
                        await SukiHost.ShowToast(GetTranslation("Common_InstallSuccess"), "o(*≧▽≦)ツ", NotificationType.Success);
                    }
                    else
                    {
                        await SukiHost.ShowToast(GetTranslation("Common_InstallFailed"), $"\r\n{output}", NotificationType.Error);
                    }
                }
            }
        }
        else
        {
            SukiHost.ShowDialog(new PureDialog(GetTranslation("Appmgr_NoApkFileSelected")), allowBackgroundClose: true);
        }
        IsInstalling = false;
    }

    [RelayCommand]
    public async Task RunApp()
    {
        await Task.Run(async () =>
        {
            IsBusy = true;
            if (!await GetDevicesInfo.SetDevicesInfoLittle())
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
                });
                IsBusy = false; return;
            }
            if (SelectedApplication() != "")
                await CallExternalProgram.ADB($"-s {Global.thisdevice} shell monkey -p {SelectedApplication()} 1");
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SukiHost.ShowDialog(new PureDialog(GetTranslation("Appmgr_AppIsNotSelected")), allowBackgroundClose: true);
                });
            }

            IsBusy = false;
        });
    }

    [RelayCommand]
    public async Task DisableApp()
    {
        await Task.Run(async () =>
        {
            IsBusy = true;
            if (!await GetDevicesInfo.SetDevicesInfoLittle())
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
                });
                IsBusy = false; return;
            }
            if (SelectedApplication() != "")
                await CallExternalProgram.ADB($"-s {Global.thisdevice} shell pm disable {SelectedApplication()}");
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SukiHost.ShowDialog(new PureDialog(GetTranslation("Appmgr_AppIsNotSelected")), allowBackgroundClose: true);
                });
            }
            IsBusy = false;
        });
    }

    [RelayCommand]
    public async Task EnableApp()
    {
        IsBusy = true;
        if (!await GetDevicesInfo.SetDevicesInfoLittle())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
            });
            IsBusy = false; return;
        }
        var selectedApp = SelectedApplication();
        if (!string.IsNullOrEmpty(selectedApp))
            await CallExternalProgram.ADB($"-s {Global.thisdevice} shell pm enable {selectedApp}");
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Appmgr_AppIsNotSelected")), allowBackgroundClose: true);
            });
        }
        IsBusy = false;
    }
    [RelayCommand]
    public async Task UninstallApp()
    {
        IsBusy = true;
        if (!await GetDevicesInfo.SetDevicesInfoLittle())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
            });
            IsBusy = false; return;
        }
        var selectedApp = SelectedApplication();
        if (!string.IsNullOrEmpty(selectedApp))
        {
            await CallExternalProgram.ADB($"-s {Global.thisdevice} shell pm uninstall {selectedApp}");
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Appmgr_AppIsNotSelected")), allowBackgroundClose: true);
            });
        }
        IsBusy = false;
    }

    [RelayCommand]
    public async Task UninstallAppWithData()
    {
        IsBusy = true;
        if (!await GetDevicesInfo.SetDevicesInfoLittle())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
            });
            IsBusy = false; return;
        }
        var selectedApp = SelectedApplication();
        if (!string.IsNullOrEmpty(selectedApp))
        {
            // Note: This command may vary depending on the requirements and platform specifics.
            // The following is a general example and may not work as is.
            await CallExternalProgram.ADB($"-s {Global.thisdevice} shell pm uninstall -k {selectedApp}");
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Appmgr_AppIsNotSelected")), allowBackgroundClose: true);
            });
        }
        IsBusy = false;
    }

    public string SelectedApplication()
    {
        return Applications.FirstOrDefault(app => app.IsSelected)?.Name ?? "";
    }

    [RelayCommand]
    public async Task ExtractInstaller()
    {
        IsBusy = true;
        if (!await GetDevicesInfo.SetDevicesInfoLittle())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
            });
            IsBusy = false; return;
        }
        var selectedApp = SelectedApplication();
        if (!string.IsNullOrEmpty(selectedApp))
        {
            // Get the apk file of the selected app, and save it to the user's desktop.
            var apkFile = await CallExternalProgram.ADB($"-s {Global.thisdevice} shell pm path {selectedApp}");
            apkFile = apkFile[(apkFile.IndexOf(':') + 1)..].Trim();
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            await CallExternalProgram.ADB($"-s {Global.thisdevice} pull {apkFile} {desktopPath}");
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Appmgr_AppIsNotSelected")), allowBackgroundClose: true);
            });
        }
        IsBusy = false;
    }

    [RelayCommand]
    public async Task ClearApp()
    {
        IsBusy = true;
        if (!await GetDevicesInfo.SetDevicesInfoLittle())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
            });
            IsBusy = false; return;
        }
        var selectedApp = SelectedApplication();
        if (!string.IsNullOrEmpty(selectedApp))
        {
            await CallExternalProgram.ADB($"-s {Global.thisdevice} shell pm clear {selectedApp}");
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Appmgr_AppIsNotSelected")), allowBackgroundClose: true);
            });
        }
        IsBusy = false;
    }

    [RelayCommand]
    public async Task ForceStopApp()
    {
        IsBusy = true;
        if (!await GetDevicesInfo.SetDevicesInfoLittle())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
            });
            IsBusy = false; return;
        }
        var selectedApp = SelectedApplication();
        if (!string.IsNullOrEmpty(selectedApp))
        {
            await CallExternalProgram.ADB($"-s {Global.thisdevice} shell am force-stop {selectedApp}");
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Appmgr_AppIsNotSelected")), allowBackgroundClose: true);
            });
        }
        IsBusy = false;
    }

    [RelayCommand]
    public async Task ActivateApp()
    {
        IsBusy = true; // Assuming this sets a flag that indicates the operation is in progress.
        if (!await GetDevicesInfo.SetDevicesInfoLittle())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Common_NotConnected")), allowBackgroundClose: true);
            });
            IsBusy = false; return;
        }
        var selectedApp = SelectedApplication();
        if (string.IsNullOrEmpty(selectedApp))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SukiHost.ShowDialog(new PureDialog(GetTranslation("Appmgr_AppIsNotSelected")), allowBackgroundClose: true);
            });
            return;
        }
        string focus_name, package_name;
        string dumpsys = await CallExternalProgram.ADB($"-s {Global.thisdevice} shell \"dumpsys window | grep mCurrentFocus\"");
        string text = await FeaturesHelper.ActiveApp(dumpsys);
        await SukiHost.ShowToast(GetTranslation("Appmgr_AppActivactor"), $"\r\n{text}", NotificationType.Info);
        IsBusy = false;
    }

    private static readonly char[] separatorArray = ['\r', '\n'];

    private static string GetInstalledDate(string[] lines)
    {
        var installedDateLine = lines.FirstOrDefault(x => x.Contains("lastUpdateTime"));
        if (installedDateLine != null)
        {
            var installedDate = installedDateLine[(installedDateLine.IndexOf('=') + 1)..].Trim();
            return installedDate;
        }
        return GetTranslation("Appmgr_UnknownTime");
    }

    private static string GetSdkVersion(string[] lines)
    {
        var sdkVersion = lines.FirstOrDefault(x => x.Contains("targetSdk"));
        if (sdkVersion != null)
        {
            var installedDate = "SDK" + sdkVersion[(sdkVersion.IndexOf('=') + 1)..].Trim();
            return installedDate;
        }
        return GetTranslation("Appmgr_UnknownSDKVersion");
    }

    private static string GetVersionName(string[] lines)
    {
        var versionName = lines.FirstOrDefault(x => x.Contains("versionName"));
        if (versionName != null)
        {
            var installedDate = versionName[(versionName.IndexOf('=') + 1)..].Trim();
            return installedDate;
        }
        return GetTranslation("Appmgr_UnknownAppVersion");
    }
}

public partial class ApplicationInfo : ObservableObject
{
    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string size;

    [ObservableProperty]
    private string otherInfo;
}