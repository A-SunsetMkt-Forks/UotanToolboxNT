﻿using SukiUI.Controls;
using System.IO;
using UotanToolbox.Features.Components;

namespace UotanToolbox.Common
{
    //修补FRP文件的代码，工具箱暂未启用此功能！-zicai
    public class FrpPatcher
    {
        private readonly string _filePath;
        private readonly string _function;
        public FrpPatcher(string filePath, string function)
        {
            _filePath = filePath;
            _function = function.ToLower();
        }
        public bool Run()
        {
            byte target = 0x02;
            if (_function == "oemunlockon")
            {
                target = 0x01;
            }
            if (_function == "oemunlockoff")
            {
                target = 0x00;
            }
            if (_function != "oemunlockon" && _function != "oemunlockoff")
            {
                SukiHost.ShowDialog(new ErrorDialog("{%c_e%}参数错误{%c_i%}{\n}"));
                return false;
            }
            if (!File.Exists(_filePath))
            {
                SukiHost.ShowDialog(new ErrorDialog("{%c_e%}找不到{_filePath}{%c_i%}{\n}"));
                return false;
            }
            byte[] fileBytes = File.ReadAllBytes(_filePath);
            byte lastByte = fileBytes[fileBytes.Length - 1];
            if (lastByte != 0x00 && lastByte != 0x01)
            {
                SukiHost.ShowDialog(new ErrorDialog("frp文件末尾1字节16进制数值不是00或01"));
                return false;
            }
            if (lastByte == target)
            {
                return true;
            }
            try
            {
                byte[] bytes = File.ReadAllBytes(_filePath);
                bytes[bytes.Length - 1] = target;
                File.WriteAllBytes(_filePath, bytes);
                SukiHost.ShowDialog(new PureDialog("frp文件修补成功"), allowBackgroundClose: true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}