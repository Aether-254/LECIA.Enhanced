using System.IO;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ClassIsland.Core.Controls;
using System.Diagnostics;
using ClassIsland.Core;
using System.IO.Ports;
using Avalonia.Threading;
using ClassIsland.Shared.Models.Profile;
using dotnetCampus.Ipc.CompilerServices.Attributes;
using ClassIsland.Shared.IPC;
using ClassIsland.Shared.IPC.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls.LessonsControls;
using ClassIsland.Shared;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using LECIA.view;
using System.Net;
using System.Net.Sockets;

namespace LECIA
{
    /*
         /$$       /$$$$$$$$  /$$$$$$  /$$$$$$  /$$$$$$ 
        | $$      | $$_____/ /$$__  $$|_  $$_/ /$$__  $$
        | $$      | $$      | $$  \__/  | $$  | $$  \ $$
        | $$      | $$$$$   | $$        | $$  | $$$$$$$$
        | $$      | $$__/   | $$        | $$  | $$__  $$
        | $$      | $$      | $$    $$  | $$  | $$  | $$
        | $$$$$$$$| $$$$$$$$|  $$$$$$/ /$$$$$$| $$  | $$
        |________/|________/ \______/ |______/|__/  |__/
     */
    //L     E      C     I      A      .     Enhanced
    //LECIA Enable Class Island Anywhere + Enhanced!

    [PluginEntrance]
    public class Plugin : PluginBase
    {

        //ini RW
        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, System.Text.StringBuilder retVal, int size, string filePath);
        /// <summary>
        /// д��INI
        /// ���ýڵ����ƣ�������ֵ��·��
        /// </summary>
        /// <param name="sSection">���ýڵ�����</param>
        /// <param name="sKey">����</param>
        /// <param name="sValue">���õļ�ֵ</param>
        /// <param name="sPath">·��</param>
        private void vINIWRITE(string sSection, string sKey, string sValue, string sPath)
        {
            WritePrivateProfileString(sSection, sKey, sValue, sPath);
        }

        /// <summary>
        /// ��ȡINI
        /// ���ýڵ����ƣ�������·��
        /// ÿ�δ�ini�ж�ȡ1024�ֽ�
        /// </summary>
        /// <param name="sSection">���ýڵ�����</param>
        /// <param name="sKey">����</param>
        /// <param name="sPath">·��</param>
        /// <returns></returns>
        private string sINIREAD(string sSection, string sKey, string sPath)
        {
            System.Text.StringBuilder temp = new System.Text.StringBuilder(1024);
            try
            {
                GetPrivateProfileString(sSection, sKey, "null", temp, 255, sPath);
            }
            catch (Exception ex)
            {
                return "null";
            }
            return temp.ToString();
        }


        
        //public static bool bKeepWorking { get; set; } = true;
        public static Thread thrMainLoop ;
        private static SerialPort spSerialPort = null;
        public ILessonsService lsLessonsService { get; set; }
        public static ClassPlan cpCurrentClassPlan;
        public static Subject sNextClassSubject;
        public static TimeSpan tsTimeToNextPoint;
        //public bool bThreadStarted { get; set; } = false;
        //public LECIA.view.settings Settings { get; set; } = new();

        public IProfileService psProfile { get; set; }
        public override void Initialize(HostBuilderContext context, IServiceCollection services)
        {
            GlobalVars.sConfigPath = PluginConfigFolder;
            if(string.IsNullOrEmpty(GlobalVars.sConfigPath))
            {
                throw new Exception("LECIA: Cannot Get Config Path.");
            }
            GlobalVars.sConfigPath += "\\config.ini";
            Console.WriteLine($"LECIA: Config Path: {GlobalVars.sConfigPath}");
            string sTemp = "";
            
            //ע���˳�
            var vAPP = AppBase.Current;
            vAPP.AppStopping += (sender, args) => {
                vONSHUTDOWN();
            };

            //��ʼ����ȡ�����ļ�
            
            {
                //autostart      0=disable 1=enable
                sTemp = sINIREAD("mainconfig", "autostart", GlobalVars.sConfigPath);
                if (sTemp != "null")
                {
                    if (sTemp == "0")
                    {
                        GlobalVars.sSettings.bAutoStart = false;
                    }
                    else if (sTemp == "1")
                    {
                        GlobalVars.sSettings.bAutoStart = true;
                    }
                    else
                    {
                        vINIWRITE("mainconfig", "autostart", "0", GlobalVars.sConfigPath);
                    }
                }
                else
                {
                    vINIWRITE("mainconfig", "autostart", "0", GlobalVars.sConfigPath);
                }
                Console.WriteLine($"LECIA: Load Config -> autostart: {GlobalVars.sSettings.bAutoStart}");

                //datatarget
                sTemp = sINIREAD("mainconfig", "datatarget", GlobalVars.sConfigPath);
                if (sTemp != "null")
                {
                    GlobalVars.sSettings.iDataTarget = int.Parse(sTemp);
                }
                else
                {
                    vINIWRITE("mainconfig", "datatarget", "0", GlobalVars.sConfigPath);
                }
                Console.WriteLine($"LECIA: Load Config -> datatarget: {GlobalVars.sSettings.iDataTarget}");

                //comport
                sTemp = sINIREAD("mainconfig", "comport", GlobalVars.sConfigPath);
                if (sTemp != "null")
                {
                    GlobalVars.sSettings.sComPort = sTemp;
                }
                else
                {
                    vINIWRITE("mainconfig", "comport", "COM1", GlobalVars.sConfigPath);
                }
                Console.WriteLine($"LECIA: Load Config -> comport: {GlobalVars.sSettings.sComPort}");

                //BaundRate
                sTemp = sINIREAD("mainconfig", "baundrate", GlobalVars.sConfigPath);
                if (sTemp != "null")
                {
                    GlobalVars.sSettings.iBaundRate = int.Parse(sTemp);
                }
                else
                {
                    vINIWRITE("mainconfig", "baundrate", "0", GlobalVars.sConfigPath);
                }
                Console.WriteLine($"LECIA: Load Config -> BaundRate: {GlobalVars.sSettings.iBaundRate}");

                //maindataformat
                sTemp = sINIREAD("mainconfig", "maindataformat", GlobalVars.sConfigPath);
                if (sTemp != "null")
                {
                    GlobalVars.sSettings.sMainDataFormat = sTemp;
                }
                else
                {
                    vINIWRITE("mainconfig", "maindataformat", "no config", GlobalVars.sConfigPath);
                }
                Console.WriteLine($"LECIA: Load Config -> maindataformat: {GlobalVars.sSettings.sMainDataFormat}");

                //delay
                sTemp = sINIREAD("mainconfig", "delay", GlobalVars.sConfigPath);
                if (sTemp != "null")
                {
                    GlobalVars.sSettings.iDelay = int.Parse(sTemp);
                }
                else
                {
                    vINIWRITE("mainconfig", "delay", "0", GlobalVars.sConfigPath);
                }
                Console.WriteLine($"LECIA: Load Config -> delay: {GlobalVars.sSettings.iDelay}");

                //netip
                sTemp = sINIREAD("mainconfig", "udpnetIP", GlobalVars.sConfigPath);
                if (sTemp != "null")
                {
                    GlobalVars.sSettings.sUDPNetIP = sTemp;
                }
                else
                {
                    vINIWRITE("mainconfig", "udpnetIP", "", GlobalVars.sConfigPath);
                }
                Console.WriteLine($"LECIA: Load Config -> maindataformat: {GlobalVars.sSettings.sUDPNetIP}");

                //iUDPNetPort
                sTemp = sINIREAD("mainconfig", "udpnetport", GlobalVars.sConfigPath);
                if (sTemp != "null")
                {
                    GlobalVars.sSettings.iUDPNetPort = int.Parse(sTemp);
                }
                else
                {
                    vINIWRITE("mainconfig", "udpnetport", "12345", GlobalVars.sConfigPath);
                }
                Console.WriteLine($"LECIA: Load Config -> delay: {GlobalVars.sSettings.iUDPNetPort}");
            }

            if (GlobalVars.sSettings.bAutoStart == true)
            {
                GlobalVars.bThreadStarted = true;
                thrMainLoop = new(vMAINLOOP);
                vAPP.AppStarted += (sender, args) => {
                    thrMainLoop.Start();
                };
            }
            services.AddSettingsPage<settings>();
            return;
        }



        /// <summary>
        /// ��ʼ������
        /// </summary>
        private static void vINITSERIALPORT()
        {
            //try
            //{
            if (spSerialPort != null)
            {
                spSerialPort.Dispose();
            }

            spSerialPort = new SerialPort(
                GlobalVars.sSettings.sComPort,
                GlobalVars.sSettings.iBaundRate,
                Parity.None,
                8,
                StopBits.One);

            spSerialPort.Open();
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine($"LECIA: Init Comport error: {e.Message}");
            //    Thread.Sleep(200);
            //}
        }

        public string sGETSUBJECTNAMEBYGUID(Guid guid)
        {
            if (psProfile.Profile.Subjects == null)
                return null;

            if (psProfile.Profile.Subjects.TryGetValue(guid, out Subject subject))
                return subject?.Name;

            return null; 
        }



        /// <summary>
        /// ����JSON��ʽ����
        /// </summary>
        private string sBUILDJSON()
        {
            var profile = psProfile.Profile;
            var subjects = profile.Subjects;
            var timeLayouts = profile.TimeLayouts;
            var classPlans = profile.ClassPlans;
            var today = DateTime.Today;

            // ���� Subjects ӳ��: Guid -> int id
            var subjectIdMap = new Dictionary<Guid, int>();
            var subjectList = new List<object>();
            int sid = 0;
            foreach (var kv in subjects)
            {
                var s = kv.Value;
                subjectIdMap[kv.Key] = sid;
                subjectList.Add(new
                {
                    id = sid,
                    name = s.Name ?? "",
                    short_name = s.Initial ?? "",
                    teacher = (string.IsNullOrEmpty(s.TeacherName) ? null : s.TeacherName),
                    is_outside = s.IsOutDoor
                });
                sid++;
            }

            // ���� TimeLayouts ӳ��: Guid -> int id
            var tlIdMap = new Dictionary<Guid, int>();
            var timetableList = new List<object>();
            int tid = 0;
            foreach (var kv in timeLayouts)
            {
                var tl = kv.Value;
                tlIdMap[kv.Key] = tid;

                var classes = new List<object>();
                int cid = 0;
                foreach (var item in tl.Layouts)
                {
                    long startUnix = new DateTimeOffset(
                        today.Year, today.Month, today.Day,
                        item.StartTime.Hours, item.StartTime.Minutes, item.StartTime.Seconds,
                        TimeSpan.Zero).ToUnixTimeSeconds();

                    long endUnix = new DateTimeOffset(
                        today.Year, today.Month, today.Day,
                        item.EndTime.Hours, item.EndTime.Minutes, item.EndTime.Seconds,
                        TimeSpan.Zero).ToUnixTimeSeconds();

                    if (item.TimeType == 1) // �μ���Ϣ
                    {
                        classes.Add(new
                        {
                            is_break = true,
                            name = item.BreakNameText,
                            start = startUnix,
                            end = endUnix
                        });
                    }
                    else // �Ͽ�����
                    {
                        // ���Ի�ȡ��ʱ����Ĭ�Ͽ�Ŀ��
                        string? teacherName = null;
                        if (item.DefaultClassId != Guid.Empty && subjects.TryGetValue(item.DefaultClassId, out var defSubject))
                        {
                            teacherName = string.IsNullOrEmpty(defSubject.TeacherName) ? null : defSubject.TeacherName;
                        }

                        classes.Add(new
                        {
                            id = cid,
                            name = item.BreakNameText, // TimeLayout �е�ʱ���������"��һ��"��
                            start = startUnix,
                            end = endUnix,
                            teacher = teacherName,
                            is_outside = false
                        });
                        cid++;
                    }
                }

                timetableList.Add(new
                {
                    id = tid,
                    name = tl.Name ?? "",
                    classes = classes
                });
                tid++;
            }

            // 构建本周 Days（周一 ~ 周日）
            var dayList = new List<object>();
            // 计算本周一
            int daysSinceMonday = ((int)today.DayOfWeek == 0) ? 6 : (int)today.DayOfWeek - 1;
            var monday = today.AddDays(-daysSinceMonday);

            for (int d = 0; d < 7; d++)
            {
                var date = monday.AddDays(d);
                var weekday = (int)date.DayOfWeek;
                var cp = lsLessonsService.GetClassPlanByDate(date, out _);
                if (cp == null) continue;

                int? cpTimetableId = null;
                if (cp.TimeLayoutId != Guid.Empty && tlIdMap.TryGetValue(cp.TimeLayoutId, out var mappedTid))
                    cpTimetableId = mappedTid;

                var classIds = new List<object>();
                foreach (var ci in cp.Classes)
                {
                    if (ci.SubjectId != Guid.Empty && subjectIdMap.TryGetValue(ci.SubjectId, out var mappedSid))
                        classIds.Add(new { id = mappedSid });
                }

                object? extendedLayer = null;
                if (cp.IsOverlay && cp.OverlaySourceId != null)
                {
                    if (classPlans.TryGetValue(cp.OverlaySourceId.Value, out var sourceCp))
                    {
                        foreach (var ci in cp.Classes)
                        {
                            var originalCi = sourceCp.Classes.FirstOrDefault(c =>
                                ci.CurrentTimeLayoutItem != null &&
                                c.CurrentTimeLayoutItem != null &&
                                c.CurrentTimeLayoutItem == ci.CurrentTimeLayoutItem);

                            if (originalCi != null && ci.SubjectId != originalCi.SubjectId)
                            {
                                int? origSid = null;
                                int? newSid = null;
                                if (originalCi.SubjectId != Guid.Empty && subjectIdMap.TryGetValue(originalCi.SubjectId, out var os))
                                    origSid = os;
                                if (ci.SubjectId != Guid.Empty && subjectIdMap.TryGetValue(ci.SubjectId, out var ns))
                                    newSid = ns;

                                if (origSid != null && newSid != null)
                                {
                                    extendedLayer = new
                                    {
                                        changed_class_id = origSid.Value,
                                        changed_class = new { id = newSid.Value }
                                    };
                                    break;
                                }
                            }
                        }
                    }
                }

                var dayObj = new Dictionary<string, object?>
                {
                    { "weekday", weekday },
                    { "timetable_id", cpTimetableId ?? 0 },
                    { "classes", classIds }
                };
                if (extendedLayer != null)
                    dayObj["extended_layer"] = extendedLayer;

                dayList.Add(dayObj);
            }
            var json = JsonSerializer.Serialize(new
            {
                days = dayList,
                subjects = subjectList,
                timetables = timetableList
            }, new JsonSerializerOptions { WriteIndented = false });

            return json;
        }


        public void vMAINLOOP()
        {
            lsLessonsService = IAppHost.GetService<ILessonsService>();
            psProfile = IAppHost.GetService<IProfileService>();
            DateTime dtTargetDate = DateTime.Today;
            Guid? sGuid;
            string sMessage = "";
            UdpClient ucUDPClientSender = new UdpClient();
            //if ����
            if (GlobalVars.sSettings.iDataTarget == 0 || GlobalVars.sSettings.iDataTarget == 2)
            {
                //��Ҫ�ȹر�Serial �������ò�����
                try
                {
                    if (spSerialPort != null)
                    {
                        spSerialPort.Dispose();
                    }
                }
                catch
                {
                    //ignore
                }
            }

            while (GlobalVars.bKeepWorking)
            {
                dtTargetDate = DateTime.Today;

                //����������
                //sMessage = "NextPointTime: {NextPointTime}    ClassLeftTime: {ClassLeftTime}    " +
                //    "BreakingLeftTime: {BreakingLeftTime}    CurrentSubjectName:{CurrentSubjectName}    CurrentClassPlan:{CurrentClassPlan}";
                sMessage = GlobalVars.sSettings.sMainDataFormat;
                try
                {
                    //��ȡ���տα�
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        sNextClassSubject = lsLessonsService.NextClassSubject;
                        cpCurrentClassPlan = lsLessonsService.GetClassPlanByDate(dtTargetDate, out sGuid);
                    });
                    // JSONģʽ
                    if (GlobalVars.sSettings.iDataTarget >= 2)
                    {
                        string sJson = sBUILDJSON();
                        if (string.IsNullOrEmpty(sJson))
                        {
                            Thread.Sleep(GlobalVars.sSettings.iDelay);
                            continue;
                        }

                        // UDP JSON
                        if (GlobalVars.sSettings.iDataTarget == 3)
                        {
                            IPEndPoint iep = new IPEndPoint(IPAddress.Parse(GlobalVars.sSettings.sUDPNetIP), GlobalVars.sSettings.iUDPNetPort);
                            byte[] bData = Encoding.UTF8.GetBytes(sJson);
                            ucUDPClientSender.Send(bData, bData.Length, iep);
                            GlobalVars.sThreadMessage = "JSON�ѷ���(UDP)";
                            Thread.Sleep(GlobalVars.sSettings.iDelay);
                            continue;
                        }

                        // COM JSON
                        if (GlobalVars.sSettings.iDataTarget == 2)
                        {
                            if (spSerialPort == null || !spSerialPort.IsOpen)
                            {
                                vINITSERIALPORT();
                                continue;
                            }
                            byte[] bData = Encoding.UTF8.GetBytes(sJson + "\n");
                            spSerialPort.Write(bData, 0, bData.Length);
                            GlobalVars.sThreadMessage = "JSON�ѷ���(COM)";
                            Thread.Sleep(GlobalVars.sSettings.iDelay);
                            continue;
                        }
                    }

                    // �ı�ģʽ - ֻ���пα�ʱ����
                    if (cpCurrentClassPlan != null)
                    {
                        /*
                         * ����Ϊ�ؼ��֣� (��Сд���еģ�
                         * {NextPointTime}              �����¸�ʱ����ʣ��ʱ�䣨����ΪZero��
                         * {ClassLeftTime}              �����Ͽε�ʱ�䣨����ΪZero��
                         * {BreakingLeftTime}           �����¿ε�ʱ�䣨����ΪZero��
                         * {CurrentSubjectName}         ��ǰ�Ŀγ���  �����޿γ�ʱΪ���޿γ̡���
                         * {CurrentClassPlan}           ��ǰ�Ŀα�
                         */

                        //��ȡ�¸�ʱ���
                        if (lsLessonsService.OnBreakingTimeLeftTime != TimeSpan.Zero)
                        {
                            tsTimeToNextPoint = lsLessonsService.OnBreakingTimeLeftTime;
                        }
                        else if (lsLessonsService.OnClassLeftTime != TimeSpan.Zero)
                        {
                            tsTimeToNextPoint = lsLessonsService.OnClassLeftTime;
                        }
                        else
                        {
                            tsTimeToNextPoint = TimeSpan.Zero;
                        }
                        sMessage = sMessage.Replace("{NextPointTime}", tsTimeToNextPoint.ToString());


                        sMessage = sMessage.Replace("{ClassLeftTime}", lsLessonsService.OnClassLeftTime.ToString());


                        sMessage = sMessage.Replace("{BreakingLeftTime}", lsLessonsService.OnBreakingTimeLeftTime.ToString());

                        string sCurrentSubjectNameTemp = "";
                        if(sNextClassSubject.AttachedObjects.Count == 0)
                        {
                            sCurrentSubjectNameTemp = "�޿γ�";
                        }
                        else
                        {
                            sCurrentSubjectNameTemp = sNextClassSubject.Name;
                        }
                        sMessage = sMessage.Replace("{CurrentSubjectName}", sCurrentSubjectNameTemp);

                        
                        string sClassPlan = "";
                        for (int i = 0; i < cpCurrentClassPlan.Classes.Count; i++)
                        {
                            sClassPlan += sGETSUBJECTNAMEBYGUID(cpCurrentClassPlan.Classes[i].SubjectId);
                            if(i < cpCurrentClassPlan.Classes.Count - 1)
                            {
                                sClassPlan += ",";
                            }
                        }
                        if (!string.IsNullOrEmpty(sClassPlan))
                        {
                            sMessage = sMessage.Replace("{CurrentClassPlan}", sClassPlan);
                        }






                        //�����ݷ���
                        if(!(
                            !string.IsNullOrEmpty(sMessage)
                            ))
                        {
                            continue;
                        }


                        //UDP
                        if (GlobalVars.sSettings.iDataTarget == 1)
                        {
                            IPEndPoint iep = new IPEndPoint(IPAddress.Parse(GlobalVars.sSettings.sUDPNetIP), GlobalVars.sSettings.iUDPNetPort);
                            byte[] bData = Encoding.UTF8.GetBytes($"{sMessage}");
                            ucUDPClientSender.Send(bData, bData.Length, iep);
                            continue;
                        }

                        //COM
                        if (GlobalVars.sSettings.iDataTarget == 0)
                        {
                            //�س�ʼ��
                            if (spSerialPort == null || !spSerialPort.IsOpen)
                            {
                                vINITSERIALPORT();
                                continue;
                            }



                            byte[] bData = Encoding.UTF8.GetBytes($"{sMessage}\n");
                            spSerialPort.Write(bData, 0, bData.Length);
                            continue;
                        }
                    }
                    else
                    {
                        //�س�ʼ��
                        if (spSerialPort == null || !spSerialPort.IsOpen)
                        {
                            vINITSERIALPORT();
                            continue;
                        }
                        byte[] bData = Encoding.UTF8.GetBytes("noclass\n");
                        spSerialPort.Write(bData, 0, bData.Length);
                    }

                    GlobalVars.sThreadMessage = "�ɹ���";
                    Thread.Sleep(GlobalVars.sSettings.iDelay);
                }
                catch (Exception ex)
                {
                    //��Ҫ�������³�ʼ��
                    Console.WriteLine($"LECIA: ERROR: {ex.Message}\n{ex.ToString()}");

                    GlobalVars.sThreadMessage = $"LECIA: ERROR: {ex.Message}\n{ex.ToString()}";
                    Thread.Sleep(500);
                }
            }
        }

        public static void vONSHUTDOWN()
        {
            if(spSerialPort != null)
            {
                if (spSerialPort.IsOpen)
                {
                    spSerialPort.Close();
                    Console.WriteLine($"LECIA: Closed the Serial");
                }
            }

            Console.WriteLine($"LECIA: Exiting");
            GlobalVars.bKeepWorking = false;
            Thread.Sleep(500);
            return;
        }


    }
}
