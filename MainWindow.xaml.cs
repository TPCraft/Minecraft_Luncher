using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TPCraftLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //计算文件MD5
        private string FileMd5(string Path)
        {
            FileStream FileStream = new FileStream(Path, System.IO.FileMode.Open);
            MD5 MD5CryptoServiceProvider = new MD5CryptoServiceProvider();
            byte[] Byte = MD5CryptoServiceProvider.ComputeHash(FileStream);
            FileStream.Close();
            StringBuilder Md5 = new StringBuilder();
            for (int I = 0; I < Byte.Length; I++)
            {
                Md5.Append(Byte[I].ToString("x2"));
            }
            return Md5.ToString();
        }

        //服务器配置
        private static JObject Config;

        //检查文件序数
        private static List<int> CheckFileNumber = new List<int>();
        private static List<int> CheckFileNumber_View = new List<int>();
        //下载文件序数
        private static List<int> DownloadFileNumber = new List<int>();
        private static List<int> DownloadFileNumber_View = new List<int>();
        //修改文件序数
        private static List<int> ChangeFileNumber = new List<int>();
        private static List<int> ChangeFileNumber_View = new List<int>();
        //下载成功数量
        private static int DownloadSuccessNumber = 0;
        //下载失败数量
        private static int DownloadFailNumber = 0;
        //更改数量
        private static int ChangeNumber = 0;

        //计数器
        private async void Timer()
        {
            while (true)
            {
                try
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        Label_CheckFile.Content = "(" + CheckFileNumber_View.Count + " / " + ((JArray)Config["Data"]).Count + ")";
                        ProgressBar_CheckFile.Value = CheckFileNumber_View.Count;
                        Label_DownloadFile.Content = "(" + (DownloadSuccessNumber + DownloadFailNumber) + " / " + DownloadFileNumber_View.Count + ") (成功: " + DownloadSuccessNumber + " 失败: " + DownloadFailNumber + ")";
                        ProgressBar_DownloadFile.Value = DownloadSuccessNumber + DownloadFailNumber;
                        Label_ChangeFile.Content = "(" + ChangeNumber + " / " + ChangeFileNumber_View.Count + ")";
                        ProgressBar_ChangeFile.Value = ChangeNumber;
                    }));
                } catch { }
                await Task.Delay(1);
            }
        }

        //连接服务器
        private async void ConnectServer()
        {
            Dispatcher.Invoke(new Action(delegate
            {
                Label_Status.Content = "连接服务器......";
            }));
            WebClient WebClient = new WebClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {
                StreamReader GetConfig = new StreamReader(WebClient.OpenRead("https://api.tpcraft.cn/Tpcraft/Game/Minecraft/Update"), Encoding.UTF8);
                Config = (JObject)JsonConvert.DeserializeObject(GetConfig.ReadToEnd());
                GetConfig.Close();
                if ((bool)Config["Status"] == true)
                {
                    Dispatcher.Invoke(new Action(async delegate
                    {
                        Label_Status.Content = "服务器连接成功。";
                    }));
                    await Task.Delay(1000);
                    Thread Thread = new Thread(new ThreadStart(CheckFile));
                    Thread.Start();
                    Thread Thread_Timer = new Thread(new ThreadStart(Timer));
                    Thread_Timer.Start();
                }
            }
            catch
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    Label_Status.Content = "服务器连接失败，请联系管理员。";
                }));
            }
        }

        //检查客户端资源
        private async void CheckFile()
        {
            Dispatcher.Invoke(new Action(delegate
            {
                Label_Status.Content = "检查客户端资源......";
                Dialog.IsOpen = true;
                ProgressBar_CheckFile.IsIndeterminate = false;
                ProgressBar_CheckFile.Maximum = ((JArray)Config["Data"]).Count;
            }));
            for (int I = 0; ((JArray)Config["Data"]).Count > I; I++)
            {
                if ((string)Config["Data"][I]["Action"] == "Add")
                {
                    if (File.Exists((string)Config["Data"][I]["Path"] + "\\" + (string)Config["Data"][I]["Filename"]))
                    {
                        if (FileMd5((string)Config["Data"][I]["Path"] + "\\" + (string)Config["Data"][I]["Filename"]) != (string)Config["Data"][I]["Md5"])
                        {
                            DownloadFileNumber.Add(I);
                            DownloadFileNumber_View.Add(I);
                        }
                    }
                    else
                    {
                        DownloadFileNumber.Add(I);
                        DownloadFileNumber_View.Add(I);
                    }
                }
                else if ((string)Config["Data"][I]["Action"] == "Delete")
                {
                    ChangeFileNumber.Add(I);
                    ChangeFileNumber_View.Add(I);
                }
                CheckFileNumber.Add(I);
                CheckFileNumber_View.Add(I);
                await Task.Delay(1);
            }
            Dispatcher.Invoke(new Action(delegate
            {
                ProgressBar_DownloadFile.IsIndeterminate = false;
                ProgressBar_DownloadFile.Maximum = DownloadFileNumber.Count;
            }));
            Thread Thread_DownloadFile = new Thread(new ThreadStart(DownloadFile));
            Thread_DownloadFile.Start();
        }

        //下载客户端资源
        private async void DownloadFile()
        {
            for (int I = 0; DownloadFileNumber.Count > I; I++)
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    Label_DownloadFileInfo.Content = (string)Config["Data"][DownloadFileNumber[I]]["Path"] + "/" + (string)Config["Data"][DownloadFileNumber[I]]["Filename"];
                }));
                Directory.CreateDirectory((string)Config["Data"][DownloadFileNumber[I]]["Path"]);
                using (var Web = new WebClient())
                {
                    Web.DownloadFile((string)Config["Data"][DownloadFileNumber[I]]["Download"], (string)Config["Data"][DownloadFileNumber[I]]["Path"] + "/" + (string)Config["Data"][DownloadFileNumber[I]]["Filename"]);
                }
                if (File.Exists((string)Config["Data"][DownloadFileNumber[I]]["Path"] + "/" + (string)Config["Data"][DownloadFileNumber[I]]["Filename"]))
                {
                    if (FileMd5((string)Config["Data"][DownloadFileNumber[I]]["Path"] + "/" + (string)Config["Data"][DownloadFileNumber[I]]["Filename"]) == (string)Config["Data"][DownloadFileNumber[I]]["Md5"])
                    {
                        DownloadSuccessNumber++;
                    } 
                    else
                    {
                        DownloadFailNumber++;
                    }
                }
                else
                {
                    DownloadFailNumber++;
                }
            }
            Dispatcher.Invoke(new Action(delegate
            {
                ProgressBar_ChangeFile.IsIndeterminate = false;
                ProgressBar_ChangeFile.Maximum = ChangeFileNumber.Count;
                Label_DownloadFileInfo.Content = "";
            }));
            Thread Thread_ChangeFile = new Thread(new ThreadStart(ChangeFile));
            Thread_ChangeFile.Start();
        }

        //更改客户端资源
        private async void ChangeFile()
        {
            for (int I = 0; ChangeFileNumber.Count > I; I++)
            {
                if (File.Exists((string)Config["Data"][ChangeFileNumber[I]]["Path"] + "/" + (string)Config["Data"][ChangeFileNumber[I]]["Filename"]))
                {
                    File.Delete((string)Config["Data"][ChangeFileNumber[I]]["Path"] + "/" + (string)Config["Data"][ChangeFileNumber[I]]["Filename"]);
                }
                ChangeNumber++;
                await Task.Delay(100);
            } 
            Dispatcher.Invoke(new Action(async delegate
            {
                Label_Status.Content = "检查客户端资源完成。";
                Dialog.IsOpen = false;
                await Task.Delay(1000);
                Label_Status.Content = "启动游戏......";
            }));
            await Task.Delay(1000);
            Process.Start("PCL.exe");
            Environment.Exit(0);
        }

        /*
         * 窗口加载事件
         */
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1000);
            Thread Thread_ConnectServer = new Thread(new ThreadStart(ConnectServer));
            Thread_ConnectServer.Start();
        }

        /*
         * 窗口关闭事件
         */
        private async void Window_Closed(object sender, EventArgs e)
        {
            await Task.Delay(1000);
            Environment.Exit(0);
        }
    }
}
