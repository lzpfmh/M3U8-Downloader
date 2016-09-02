using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;

namespace M3U8_Downloader
{
    public partial class Form1 : Form
    {
        
        [DllImport("kernel32.dll")]
        static extern bool GenerateConsoleCtrlEvent(int dwCtrlEvent, int dwProcessGroupId);
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();
        [DllImport("user32.dll")]
        public static extern bool FlashWindow(IntPtr hWnd,bool bInvert );


        int ffmpegid =0;
        int SkinId = 0;//默认为Light模式


        public Form1()
        {
            InitializeComponent();
            Init();
            Control.CheckForIllegalCrossThreadCalls = false;  //禁止编译器对跨线程访问做检查
        }

        private void textBox_Adress_DragEnter(object sender, DragEventArgs e)
        {

            e.Effect = DragDropEffects.All;
        }

        private void textBox_Adress_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }
        private void textBox_Adress_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                //获取拖拽的文件地址
                var filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
                var hz = filenames[0].LastIndexOf('.') + 1;
                var houzhui = filenames[0].Substring(hz);//文件后缀名
                if (houzhui == "m3u8"||houzhui == "mkv"||houzhui == "avi"||houzhui == "mp4"||houzhui == "ts"||houzhui == "flv"||houzhui == "f4v"||
                    houzhui == "wmv"||houzhui == "wm"||houzhui == "mpeg"||houzhui == "mpg"||houzhui == "m4v"||houzhui == "3gp"||houzhui == "rm"||
                    houzhui == "rmvb" || houzhui == "mov" || houzhui == "qt" || houzhui == "m2ts" || houzhui == "m3u" || houzhui == "mts" || houzhui == "txt") //只允许拖入部分文件
                {
                    e.Effect = DragDropEffects.All;
                    string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
                    textBox_Adress.Text = path; //将获取到的完整路径赋值到textBox1
                }
                
            }        
            
        }

        private void button_Quit_Click(object sender, EventArgs e)
        {
            SaveSettings();
            if (Process.GetProcessesByName("ffmpeg").Length != 0)
            {
                if (MessageBox.Show("已启动下载进程，确认退出吗", "请确认您的操作", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                {
                    Stop();
                    Dispose();
                    Application.Exit();
                }
                else
                { 
                }
            }
            else
            {
                Dispose();
                Application.Exit();
            }
            
        }

        private void button_ChangePath_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox_DownloadPath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button_OpenPath_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", textBox_DownloadPath.Text);
        }

        private void linkLabel_Stop_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Stop();
        }

        private void textBox_Info_TextChanged(object sender, EventArgs e)
        {

            Regex regex = new Regex(@"(\d\d[.:]){3}\d\d", RegexOptions.Compiled | RegexOptions.Singleline);//取视频时长以及Time属性
            label5.Text = "[总时长：" + regex.Match(textBox_Info.Text).Value + "]";
            var time = regex.Matches(textBox_Info.Text);
            if (time.Count > 0)
            { label6.Text = "[已下载：" + time.OfType<Match>().Last() + "]"; }
            Regex fps = new Regex(@"(\S+)\sfps", RegexOptions.Compiled | RegexOptions.Singleline);//取视频帧数
            Regex resolution = new Regex(@"\d{2,}x\d{2,}", RegexOptions.Compiled | RegexOptions.Singleline);//取视频分辨率
            label7.Text = "[视频信息：" + resolution.Match(textBox_Info.Text).Value + "，" + fps.Match(textBox_Info.Text).Value + "]";
            if (time.Count > 0)
            {
                Double All = Convert.ToDouble(Convert.ToDouble(label5.Text.Substring(5, 2)) * 60 * 60 + Convert.ToDouble(label5.Text.Substring(8, 2)) * 60
                + Convert.ToDouble(label5.Text.Substring(11, 2)) + Convert.ToDouble(label5.Text.Substring(14, 2)) / 100);
                Double Downloaded = Convert.ToDouble(Convert.ToDouble(label6.Text.Substring(5, 2)) * 60 * 60 + Convert.ToDouble(label6.Text.Substring(8, 2)) * 60
                + Convert.ToDouble(label6.Text.Substring(11, 2)) + Convert.ToDouble(label6.Text.Substring(14, 2)) / 100);

                Double Progress = (Downloaded / All) * 100;

                if (Progress > 100)  //防止进度条超过百分之百
                {
                    Progress = 100;
                }
                ProgressBar.Value = Convert.ToInt32(Progress); 

                this.Text = "M3U8 Downloader  by：nilaoda [0.1.0]" +  "     已完成：" + String.Format("{0:F}", Progress) + "%";
            }
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(@"Tools\ffmpeg.exe"))  //判断程序目录有无ffmpeg.exe
            {
            }
            else
            {
                MessageBox.Show("没有找到Tools\\ffmpeg.exe", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Dispose();
                Application.Exit();
            }
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show("已运行程序！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);   //设置程序为无法多开
                Dispose();
                Application.Exit();
            }

            if (File.Exists(@"Tools\Settings.xml"))  //判断程序目录有无配置文件，并读取文件
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(@"Tools\Settings.xml");    //加载Xml文件  
                XmlNodeList topM = doc.SelectNodes("Settings");
                foreach (XmlElement element in topM)
                {
                    if (element.GetElementsByTagName("Skin")[0].InnerText == "1") { SkinId = 0; }
                    if (element.GetElementsByTagName("Skin")[0].InnerText == "0") { SkinId = 1; }
                    Skin();
                    textBox_DownloadPath.Text = element.GetElementsByTagName("DownPath")[0].InnerText;
                    if (element.GetElementsByTagName("ExtendName")[0].InnerText == "MP4") { radioButton1.Checked = true; }
                    if (element.GetElementsByTagName("ExtendName")[0].InnerText == "MKV") { radioButton2.Checked = true; }
                    if (element.GetElementsByTagName("ExtendName")[0].InnerText == "TS") { radioButton3.Checked = true; }
                    if (element.GetElementsByTagName("ExtendName")[0].InnerText == "FLV") { radioButton4.Checked = true; }
                }
            }
            else  //若无配置文件，获取当前程序运行路径，即为默认下载路径
            {
                string lujing = System.Environment.CurrentDirectory;
                textBox_DownloadPath.Text = lujing;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
            if (Process.GetProcessesByName("ffmpeg").Length != 0)
            {
                if (MessageBox.Show("已启动下载进程，确认退出吗", "请确认您的操作", MessageBoxButtons.YesNo,MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                {
                    Stop();
                    Dispose();
                    Application.Exit();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        private void textBox_Adress_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null)
                return;
            if (e.KeyChar == (char)1)       // Ctrl-A 相当于输入了AscII=1的控制字符
            {
                textBox.SelectAll();
                e.Handled = true;      // 不再发出“噔”的声音
            }
        }

        private void 嗅探工具ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exist_Run(@"Tools\HttpFileMonitor.exe");
        }
        private void 视频转码ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("你可以把视频文件拖到m3u8地址那里哦~", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void 生成日志ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String LogName = "日志-" + System.DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss") + ".txt";
            StreamWriter log = new StreamWriter(LogName);
            log.WriteLine("━━━━━━━━━━━━━━\r\n"
                + "■M3U8 Downloader 用户日志\r\n\r\n"
                + "■" + System.DateTime.Now.ToString("F") + "\r\n\r\n"
                + "■输入：" + textBox_Adress.Text + "\r\n\r\n"
                + "■输出：" + textBox_DownloadPath.Text + "\\" + textBox_Name.Text + houzhui.Text + "\r\n\r\n"
                + "■FFmpeg命令：ffmpeg " + Command.Text + "\r\n"
                + "━━━━━━━━━━━━━━"
                + "\r\n\r\n"
                + textBox_Info.Text);
            log.Close();
            MessageBox.Show("日志已生成！", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void 换肤ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Skin();
        }
        private void 获取FFmpegToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://ffmpeg.zeranoe.com/builds/");
        }
        private void 获取新版本ToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            Process.Start("http://pan.baidu.com/s/1dF4uDuL");
        }

        private void 视频合并ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exist_Run(@"Tools\FFmpeg_Joiner.exe");
        }

        

       
        }
    }


namespace M3U8_Downloader
{
    // 1.定义委托  
    public delegate void DelReadStdOutput(string result);
    public delegate void DelReadErrOutput(string result);

    public partial class Form1 : Form
    {
        // 2.定义委托事件  
        public event DelReadStdOutput ReadStdOutput;
        public event DelReadErrOutput ReadErrOutput;


        private void button_Download_Click(object sender, EventArgs e)
        {
            生成日志ToolStripMenuItem.Enabled = true;

            if (!Directory.Exists(textBox_DownloadPath.Text))//若文件夹不存在则新建文件夹   
            {
                Directory.CreateDirectory(textBox_DownloadPath.Text); //新建文件夹   
            }  

            else
            {
                Download();
                linkLabel_Stop.Visible = true;
                label5.Visible = true;
                label6.Visible = true;
                label7.Visible = true;
            }
            
        }

        private void Exist_Run(string FileName)
        {
            if (File.Exists(FileName))  //判断有无某文件
            {
                Process.Start(FileName);
            }
            else
            {
                MessageBox.Show("没有找到" + FileName, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Skin()
        {
            if (SkinId == 1)
            {
                this.BackColor = Color.FromArgb(230, 230, 230);
                label1.ForeColor = Color.Black;
                label2.ForeColor = Color.Black;
                label3.ForeColor = Color.Black;
                label4.ForeColor = Color.Black;
                label5.ForeColor = Color.Black;
                label6.ForeColor = Color.Black;
                label7.ForeColor = Color.Black;
                radioButton4.ForeColor = Color.Black;
                radioButton3.ForeColor = Color.Black;
                radioButton2.ForeColor = Color.Black;
                radioButton1.ForeColor = Color.Black;
                linkLabel_Stop.LinkColor = Color.Black;
                menuStrip1.BackColor = Color.FromArgb(240, 240, 240);
                textBox_Adress.BackColor = Color.FromArgb(240, 240, 240);
                textBox_Adress.ForeColor = Color.Black;
                textBox_Name.BackColor = Color.FromArgb(240, 240, 240);
                textBox_Name.ForeColor = Color.Black;
                textBox_DownloadPath.BackColor = Color.FromArgb(240, 240, 240);
                textBox_DownloadPath.ForeColor = Color.Black;
                textBox_Info.BackColor = Color.FromArgb(240, 240, 240);
                textBox_Info.ForeColor = Color.Black;
                button_ChangePath.BackColor = Color.FromArgb(204, 204, 204);
                button_ChangePath.ForeColor = Color.Black;
                button_OpenPath.BackColor = Color.FromArgb(204, 204, 204);
                button_OpenPath.ForeColor = Color.Black;
                button_Download.BackColor = Color.FromArgb(204, 204, 204);
                button_Download.ForeColor = Color.Black;
                button_Quit.BackColor = Color.FromArgb(204, 204, 204);
                button_Quit.ForeColor = Color.Black;
                嗅探工具ToolStripMenuItem.ForeColor = Color.Black;
                视频转码ToolStripMenuItem.ForeColor = Color.Black;
                生成日志ToolStripMenuItem.ForeColor = Color.Black;
                换肤ToolStripMenuItem1.ForeColor = Color.Black;
                获取FFmpegToolStripMenuItem.ForeColor = Color.Black;
                获取新版本ToolStripMenuItem.ForeColor = Color.Black;
                视频合并ToolStripMenuItem.ForeColor = Color.Black;
                SkinId = 0; //记录皮肤模式
            }
            else
            {
                this.BackColor = Color.FromArgb(83, 83, 83);
                label1.ForeColor = Color.FromArgb(245, 245, 245);
                label2.ForeColor = Color.FromArgb(245, 245, 245);
                label3.ForeColor = Color.FromArgb(245, 245, 245);
                label4.ForeColor = Color.FromArgb(245, 245, 245);
                label5.ForeColor = Color.FromArgb(245, 245, 245);
                label6.ForeColor = Color.FromArgb(245, 245, 245);
                label7.ForeColor = Color.FromArgb(245, 245, 245);
                radioButton4.ForeColor = Color.FromArgb(245, 245, 245);
                radioButton3.ForeColor = Color.FromArgb(245, 245, 245);
                radioButton2.ForeColor = Color.FromArgb(245, 245, 245);
                radioButton1.ForeColor = Color.FromArgb(245, 245, 245);
                linkLabel_Stop.LinkColor = Color.FromArgb(245, 245, 245);
                menuStrip1.BackColor = Color.FromArgb(58, 58, 58);
                textBox_Adress.BackColor = Color.FromArgb(58, 58, 58);
                textBox_Adress.ForeColor = Color.FromArgb(245, 245, 245);
                textBox_Name.BackColor = Color.FromArgb(58, 58, 58);
                textBox_Name.ForeColor = Color.FromArgb(245, 245, 245);
                textBox_DownloadPath.BackColor = Color.FromArgb(58, 58, 58);
                textBox_DownloadPath.ForeColor = Color.FromArgb(245, 245, 245);
                textBox_Info.BackColor = Color.FromArgb(58, 58, 58);
                textBox_Info.ForeColor = Color.FromArgb(245, 245, 245);
                button_ChangePath.BackColor = Color.FromArgb(102, 102, 102);
                button_ChangePath.ForeColor = Color.FromArgb(245, 245, 245);
                button_OpenPath.BackColor = Color.FromArgb(102, 102, 102);
                button_OpenPath.ForeColor = Color.FromArgb(245, 245, 245);
                button_Download.BackColor = Color.FromArgb(102, 102, 102);
                button_Download.ForeColor = Color.FromArgb(245, 245, 245);
                button_Quit.BackColor = Color.FromArgb(102, 102, 102);
                button_Quit.ForeColor = Color.FromArgb(245, 245, 245);
                嗅探工具ToolStripMenuItem.ForeColor = Color.FromArgb(245, 245, 245);
                视频转码ToolStripMenuItem.ForeColor = Color.FromArgb(245, 245, 245);
                生成日志ToolStripMenuItem.ForeColor = Color.FromArgb(245, 245, 245);
                换肤ToolStripMenuItem1.ForeColor = Color.FromArgb(245, 245, 245);
                获取FFmpegToolStripMenuItem.ForeColor = Color.FromArgb(245, 245, 245);
                获取新版本ToolStripMenuItem.ForeColor = Color.FromArgb(245, 245, 245);
                视频合并ToolStripMenuItem.ForeColor = Color.FromArgb(245, 245, 245);
                SkinId = 1; //记录皮肤模式
            }
            
        }

        private void SaveSettings()
        {
            string ExtendName = "";
            if (radioButton1.Checked == true) { ExtendName = "MP4"; }
            if (radioButton2.Checked == true) { ExtendName = "MKV"; }
            if (radioButton3.Checked == true) { ExtendName = "TS"; }
            if (radioButton4.Checked == true) { ExtendName = "FLV"; }

            XmlTextWriter xml = new XmlTextWriter(@"Tools\Settings.xml", Encoding.UTF8);
            xml.Formatting = Formatting.Indented;
            xml.WriteStartDocument();
            xml.WriteStartElement("Settings");

            xml.WriteStartElement("Skin"); xml.WriteCData(SkinId.ToString()); xml.WriteEndElement();
            xml.WriteStartElement("DownPath"); xml.WriteCData(textBox_DownloadPath.Text); xml.WriteEndElement();
            xml.WriteStartElement("ExtendName"); xml.WriteCData(ExtendName); xml.WriteEndElement();

            xml.WriteEndElement();
            xml.WriteEndDocument();
            xml.Flush();
            xml.Close();
        }

        private void Download()
        {
            textBox_Info.Text = "";
            if (radioButton1.Checked == true)
            {
                houzhui.Text = ".mp4";
                Command.Text = " -i " + "\"" + textBox_Adress.Text + "\"" + " -c copy -y -bsf:a aac_adtstoasc " + "\"" + textBox_DownloadPath.Text + "\\" + textBox_Name.Text + ".mp4" + "\"";
                // 启动进程执行相应命令,此例中以执行ffmpeg.exe为例  
                RealAction("Tools\\ffmpeg.exe", Command.Text);
            }
            if (radioButton2.Checked == true)
            {
                houzhui.Text = ".mkv";
                Command.Text = " -i " + "\"" + textBox_Adress.Text + "\"" + " -c copy -y -bsf:a aac_adtstoasc " + "\"" + textBox_DownloadPath.Text + "\\" + textBox_Name.Text + ".mkv" + "\"";
                RealAction("Tools\\ffmpeg.exe", Command.Text);
            }
            if (radioButton3.Checked == true)
            {
                houzhui.Text = ".ts";
                Command.Text = " -i " + "\"" + textBox_Adress.Text + "\"" + " -c copy -y -f mpegts " + "\"" + textBox_DownloadPath.Text + "\\" + textBox_Name.Text + ".ts" + "\"";
                RealAction("Tools\\ffmpeg.exe", Command.Text);
            }
            if (radioButton4.Checked == true)
            {
                houzhui.Text = ".flv";
                Command.Text = " -i " + "\"" + textBox_Adress.Text + "\"" + " -c copy -y -f f4v -bsf:a aac_adtstoasc " + "\"" + textBox_DownloadPath.Text + "\\" + textBox_Name.Text + ".flv" + "\"";
                RealAction("Tools\\ffmpeg.exe", Command.Text);
            }
        }

        private void RealAction(string StartFileName, string StartFileArg)
        {
            Process CmdProcess = new Process();
            CmdProcess.StartInfo.FileName = StartFileName;      // 命令  
            CmdProcess.StartInfo.Arguments = StartFileArg;      // 参数  

            CmdProcess.StartInfo.CreateNoWindow = true;         // 不创建新窗口  
            CmdProcess.StartInfo.UseShellExecute = false;
            CmdProcess.StartInfo.RedirectStandardInput = true;  // 重定向输入  
            CmdProcess.StartInfo.RedirectStandardOutput = true; // 重定向标准输出  
            CmdProcess.StartInfo.RedirectStandardError = true;  // 重定向错误输出  
            //CmdProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;  

            CmdProcess.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            CmdProcess.ErrorDataReceived += new DataReceivedEventHandler(p_ErrorDataReceived);

            CmdProcess.EnableRaisingEvents = true;                      // 启用Exited事件  
            CmdProcess.Exited += new EventHandler(CmdProcess_Exited);   // 注册进程结束事件  

            CmdProcess.Start();
            ffmpegid = CmdProcess.Id;//获取ffmpeg.exe的进程ID
            CmdProcess.BeginOutputReadLine();
            CmdProcess.BeginErrorReadLine();

            // 如果打开注释，则以同步方式执行命令，此例子中用Exited事件异步执行。  
            // CmdProcess.WaitForExit();       

        }

        public void Stop()
        {
            AttachConsole(ffmpegid);
            SetConsoleCtrlHandler(IntPtr.Zero, true);
            GenerateConsoleCtrlEvent(0, 0);
            FreeConsole();
        }

        //以下为实现异步输出CMD信息

        private void Init()
        {
            //3.将相应函数注册到委托事件中  
            ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
            ReadErrOutput += new DelReadErrOutput(ReadErrOutputAction);
        }

        private void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                // 4. 异步调用，需要invoke  
                this.Invoke(ReadStdOutput, new object[] { e.Data });
            }
        }

        private void p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                this.Invoke(ReadErrOutput, new object[] { e.Data });
            }
        }

        private void ReadStdOutputAction(string result)
        {
            this.textBox_Info.AppendText(result + "\r\n");
        }

        private void ReadErrOutputAction(string result)
        {
            this.textBox_Info.AppendText(result + "\r\n");
        }

        private void CmdProcess_Exited(object sender, EventArgs e)
        {
            FlashWindow(this.Handle, true);
            MessageBox.Show("命令执行结束！", "M3U8 Downloader", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);  // 执行结束后触发
        }  
    }
}
