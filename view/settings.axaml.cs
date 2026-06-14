using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using static LECIA.Plugin;
using System.Text.RegularExpressions;


namespace LECIA.view
{
    [SettingsPageInfo(
        "LECIA.Settings",
        "LECIA设置",
        SettingsPageCategory.External
    )]

    /// <summary>
    /// settings.axaml 的交互逻辑
    /// </summary>
    public partial class settings : SettingsPageBase
    {
        //ini RW
        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, System.Text.StringBuilder retVal, int size, string filePath);

        private void vINIWRITE(string sSection, string sKey, string sValue, string sPath)
        {
            WritePrivateProfileString(sSection, sKey, sValue, sPath);
        }

        private string sINIREAD(string sSection, string sKey, string sPath)
        {
            System.Text.StringBuilder temp = new System.Text.StringBuilder(1024);
            try
            {
                GetPrivateProfileString(sSection, sKey, "null", temp, 255, sPath);
            }
            catch
            {
                return "null";
            }
            return temp.ToString();
        }

        private bool bCHECKCOMSTRING(string sInput)
        {
            if (string.IsNullOrEmpty(sInput))
                return false;
            return Regex.IsMatch(sInput, @"^COM\d+$", RegexOptions.IgnoreCase);
        }

        private int iCHECKRETURNINT(string sInputInt)
        {
            if (Regex.IsMatch(sInputInt, @"^[0-9]\d*$"))
                return int.Parse(sInputInt);
            return -1;
        }

        private bool bFULLCHECK()
        {
            try
            {
                int iDataTarget = iCHECKRETURNINT(SETTING_DATATARGET.Text);
                if (iDataTarget == 0 || iDataTarget == 2)
                {
                    if (!bCHECKCOMSTRING(SETTING_COMPORT.Text))
                    {
                        _ = ClassIsland.Core.Controls.CommonTaskDialogs.ShowDialog("提示", "COM口格式错误!");
                        return false;
                    }
                    int iBaundrate = iCHECKRETURNINT(SETTING_BAUNDRATE.Text);
                    if (iBaundrate == -1)
                    {
                        _ = ClassIsland.Core.Controls.CommonTaskDialogs.ShowDialog("提示", "波特率错误，请输入正整数!");
                        return false;
                    }
                }
                else if (iDataTarget == 1 || iDataTarget == 3)
                {
                    string sIP = SETTING_UDPIP.Text;
                    if (string.IsNullOrEmpty(sIP))
                    {
                        _ = ClassIsland.Core.Controls.CommonTaskDialogs.ShowDialog("提示", "IP错误!");
                        return false;
                    }
                    int iPort = iCHECKRETURNINT(SETTING_UDPPORT.Text);
                    if (iPort == -1 || iPort > 65535 || iPort <= 0)
                    {
                        _ = ClassIsland.Core.Controls.CommonTaskDialogs.ShowDialog("提示", "UDP端口错误");
                        return false;
                    }
                }
                else
                {
                    _ = ClassIsland.Core.Controls.CommonTaskDialogs.ShowDialog("提示", "不合法的数据目标");
                    return false;
                }

                int iTempDelay = iCHECKRETURNINT(SETTING_DELAYTIME.Text);
                if (iTempDelay == -1)
                {
                    _ = ClassIsland.Core.Controls.CommonTaskDialogs.ShowDialog("提示", "延时错误，请输入正整数!");
                    return false;
                }

                if (string.IsNullOrEmpty(SETTING_DATAFORMAT.Text))
                {
                    _ = ClassIsland.Core.Controls.CommonTaskDialogs.ShowDialog("提示", "数据格式为空!");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _ = ClassIsland.Core.Controls.CommonTaskDialogs.ShowDialog("提示", $"发生了错误\n{ex.Message}");
                return false;
            }
        }

        public bool bKeepWorking { get; set; }
        private DispatcherTimer? dtThreadMessageTimer;

        public settings()
        {
            InitializeComponent();
            if (GlobalVars.bThreadStarted)
            {
                MENU_STARTSW.Content = "立即停止";
            }

            DataContext = this;

            // 加载设置到UI
            SETTING_AUTOSTARTSW.IsChecked = GlobalVars.sSettings.bAutoStart;
            SETTING_COMPORT.Text = GlobalVars.sSettings.sComPort;
            SETTING_BAUNDRATE.Text = GlobalVars.sSettings.iBaundRate.ToString();
            SETTING_DATAFORMAT.Text = GlobalVars.sSettings.sMainDataFormat;
            SETTING_DELAYTIME.Text = GlobalVars.sSettings.iDelay.ToString();
            SETTING_DATATARGET.Text = GlobalVars.sSettings.iDataTarget.ToString();
            SETTING_UDPIP.Text = GlobalVars.sSettings.sUDPNetIP;
            SETTING_UDPPORT.Text = GlobalVars.sSettings.iUDPNetPort.ToString();
        }

        private void vTIMERTICK(object? sender, EventArgs e)
        {
            if (TEXTBOX_THREADMESSAGE.Text != GlobalVars.sThreadMessage)
            {
                TEXTBOX_THREADMESSAGE.Text = GlobalVars.sThreadMessage;
            }
        }

        private void vSETDATA()
        {
            GlobalVars.sSettings.bAutoStart = SETTING_AUTOSTARTSW.IsChecked ?? false;
            GlobalVars.sSettings.iDataTarget = int.Parse(SETTING_DATATARGET.Text ?? "0");
            if (GlobalVars.sSettings.iDataTarget == 0 || GlobalVars.sSettings.iDataTarget == 2)
            {
                GlobalVars.sSettings.sComPort = SETTING_COMPORT.Text ?? "COM1";
                GlobalVars.sSettings.iBaundRate = int.Parse(SETTING_BAUNDRATE.Text ?? "115200");
            }
            else if (GlobalVars.sSettings.iDataTarget == 1 || GlobalVars.sSettings.iDataTarget == 3)
            {
                GlobalVars.sSettings.sUDPNetIP = SETTING_UDPIP.Text ?? "";
                GlobalVars.sSettings.iUDPNetPort = int.Parse(SETTING_UDPPORT.Text ?? "12345");
            }
            else
            {
                return;
            }

            GlobalVars.sSettings.sMainDataFormat = SETTING_DATAFORMAT.Text ?? "";
            GlobalVars.sSettings.iDelay = int.Parse(SETTING_DELAYTIME.Text ?? "200");
        }

        private void MENU_SAVE_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (bFULLCHECK())
                {
                    vSETDATA();

                    if (GlobalVars.sSettings.bAutoStart)
                        vINIWRITE("mainconfig", "AutoStart", "1", GlobalVars.sConfigPath);
                    else
                        vINIWRITE("mainconfig", "AutoStart", "0", GlobalVars.sConfigPath);

                    vINIWRITE("mainconfig", "datatarget", GlobalVars.sSettings.iDataTarget.ToString(), GlobalVars.sConfigPath);
                    vINIWRITE("mainconfig", "comport", GlobalVars.sSettings.sComPort, GlobalVars.sConfigPath);
                    vINIWRITE("mainconfig", "baundrate", GlobalVars.sSettings.iBaundRate.ToString(), GlobalVars.sConfigPath);
                    vINIWRITE("mainconfig", "maindataformat", GlobalVars.sSettings.sMainDataFormat, GlobalVars.sConfigPath);
                    vINIWRITE("mainconfig", "delay", GlobalVars.sSettings.iDelay.ToString(), GlobalVars.sConfigPath);
                    vINIWRITE("mainconfig", "udpnetport", GlobalVars.sSettings.iUDPNetPort.ToString(), GlobalVars.sConfigPath);
                    vINIWRITE("mainconfig", "udpnetIP", GlobalVars.sSettings.sUDPNetIP, GlobalVars.sConfigPath);
                    _ = ClassIsland.Core.Controls.CommonTaskDialogs.ShowDialog("提示", "设置已保存!");
                }
            }
            catch (Exception ex)
            {
                _ = ClassIsland.Core.Controls.CommonTaskDialogs.ShowDialog("提示", $"发生了错误\n{ex.Message}");
                return;
            }
        }

        private void MENU_STARTSW_Click(object? sender, RoutedEventArgs e)
        {
            if (GlobalVars.bThreadStarted)
            {
                GlobalVars.bKeepWorking = false;
                dtThreadMessageTimer?.Stop();
                MENU_STARTSW.Content = "立即启动";
            }
            else
            {
                try
                {
                    if (bFULLCHECK())
                    {
                        vSETDATA();
                        GlobalVars.bKeepWorking = true;
                        Task.Run(() =>
                        {
                            var plugin = new LECIA.Plugin();
                            plugin.vMAINLOOP();
                        });
                        dtThreadMessageTimer = new DispatcherTimer();
                        dtThreadMessageTimer.Interval = TimeSpan.FromMilliseconds(20);
                        dtThreadMessageTimer.Tick += vTIMERTICK;
                        dtThreadMessageTimer.Start();
                    }
                    else
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _ = ClassIsland.Core.Controls.CommonTaskDialogs.ShowDialog("提示", $"启动时发生了错误\n{ex.Message}");
                    return;
                }

                MENU_STARTSW.Content = "立即停止";
            }
            GlobalVars.bThreadStarted = !GlobalVars.bThreadStarted;
        }

        private void vTEXTBOX_FORMAT_CHANGED(object? sender, TextChangedEventArgs e)
        {
            string input = SETTING_DATAFORMAT.Text ?? "";
            string result = sCHANGESHOWPREVIEW(input);
            TEXTBOX_FORMATSPREVIEW.Text = result;
        }

        private string sCHANGESHOWPREVIEW(string sInput)
        {
            sInput = sInput.Replace("{NextPointTime}", "12:34:56.8173949");
            sInput = sInput.Replace("{ClassLeftTime}", "02:11:44.8173949");
            sInput = sInput.Replace("{BreakingLeftTime}", "11:11:22.8122249");
            sInput = sInput.Replace("{CurrentSubjectName}", "通用技术");
            sInput = sInput.Replace("{CurrentClassPlan}", "自习,自习,数学,通用技术,周测,班会,美术,政治");
            return sInput;
        }
    }
}
