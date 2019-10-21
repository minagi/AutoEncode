using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using PostRec.Properties;
using System.Threading;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PostRec
{
    class Program
    {
        /// <summary>
        /// とにかくArgumentを入れて、実行する
        /// </summary>
        /// <param name="ExeName"></param>
        /// <param name="Args"></param>
        void Exec(string ExeName, string Args)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = ExeName;
            p.StartInfo.Arguments = Args;
            p.Start();
            p.Refresh();
            p.PriorityClass = ProcessPriorityClass.Idle;
            while (p.HasExited == false)
            {
                Thread.Sleep(100);
            }
            p.WaitForExit();
        }

        /// <summary>
        /// StandardErrorを出力する。
        /// </summary>
        /// <param name="ExeName"></param>
        /// <param name="Args"></param>
        /// <returns></returns>
        string ExecRedirect(string ExeName, string Args)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = ExeName;
            p.StartInfo.Arguments = Args;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            p.Refresh();
            p.PriorityClass = ProcessPriorityClass.Idle;
            return p.StandardError.ReadToEnd();
        }

        /// <summary>
        /// 分類を行う。
        /// </summary>
        /// <param name="InputFileName"></param>
        /// <param name="OutputDir"></param>
        /// <param name="ChannelName"></param>
        /// <returns></returns>
        string DoClassify(string InputFileName, string OutputDir, out string ChannelName)
        {

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("*****************************");
            Console.WriteLine("   出力先ディレクトリの作成");
            Console.WriteLine("*****************************");

            string ClassifyName = "";
            string outname = "";
            bool DefaultEncode = false;

            // しょぼいカレンダーで整形済みファイル名
            // xxxxx (CHA).ts やxxxxx (CHA)_dec.ts
            Match m = Regex.Match(Path.GetFileName(InputFileName), "(.*)\\s\\((.*)\\)(_dec|)\\.ts");
            ChannelName = "";

            if (m.Success)
            {
                ClassifyName = m.Groups[1].Value;
                ChannelName = m.Groups[2].Value;
                outname = m.Groups[1].Value + "("+m.Groups[2].Value+")";
                // ClassifyName 局名までのファイル名
                // ChannelName  局名
                // 
                //m.Groups[0].Value ファイル名全体
                //m.Groups[1].Value 局名の手前まで
                //m.Groups[2].Value 局名
                Console.WriteLine("Program       : " + ClassifyName);
                Console.WriteLine("Channel       : " + ChannelName);
                Console.WriteLine("OutputFileName: " + outname + ".mp4");
            }
            else
            {
                DefaultEncode = true;
                Console.WriteLine("ファイル名の解釈に失敗");
            }

            /// 出力先があれば分類情報を作る
            if (OutputDir != "" && DefaultEncode == false)
            {

                /// 分類名
                ClassifyName = Regex.Replace(ClassifyName, "\\[.+?\\]", "");
                ClassifyName = Regex.Replace(ClassifyName, "＜.*?＞", "");
                ClassifyName = Regex.Replace(ClassifyName, "アニメ[ 　]+", "");
                ClassifyName = Regex.Replace(ClassifyName, ".mp4", "");
                ClassifyName = Regex.Replace(ClassifyName, ".ts", "");
                ClassifyName = ClassifyName.Trim();

                string b = Regex.Match(ClassifyName, "(^.*?)[ 　第「（｢\\[［『‘“【＃#～]").Groups[1].Value;
                if (b != "") ClassifyName = b;

                /// 分類先フォルダを作り、出力先を決める
                /// 一応、フォルダのアクセスタイムを決める。
                string finaldir = Path.Combine(OutputDir, ClassifyName);
                try
                {
                    Directory.CreateDirectory(finaldir);
                    Directory.SetLastAccessTime(finaldir, DateTime.Now);
                    Directory.SetLastWriteTime(finaldir, DateTime.Now);
                    Directory.SetCreationTime(finaldir, DateTime.Now);
                    Console.WriteLine("OutputDir     : " + finaldir);
                }
                catch { }


                return Path.Combine(OutputDir, ClassifyName, Path.GetFileNameWithoutExtension(outname) + ".mp4");
            }
            else
            {
                ChannelName = "";
                return Path.ChangeExtension(InputFileName, ".mp4");
            }

        }


        /// <summary>
        /// AUCを実行
        /// </summary>
        object Auc(string command, string arg)
        {
            Type WshShell;
            WshShell = Type.GetTypeFromProgID("Wscript.Shell");
            Object obj = Activator.CreateInstance(WshShell);

            Object type = 2;
            Object wait = true;

            return WshShell.InvokeMember(
                "Run",
                BindingFlags.InvokeMethod,
                null,
                obj,
                new Object[] { Path.Combine(Settings.Default.AUCDir, "auc_" + command + ".exe ") + arg, type, wait }
            );
        }

        /// <summary>
        /// テキストファイルを全部読み込み
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        string ReadFile(string filename)
        {
            try
            {
                using (StreamReader sr = new StreamReader(filename, Encoding.GetEncoding("shift-jis")))
                {
                    return sr.ReadToEnd();
                }
            }
            catch { return ""; }
        }

        /// <summary>
        /// ファイルをテキストで保存
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="filestring"></param>
        void WriteFile(string filename, string filestring)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filename, false, Encoding.GetEncoding("shift-jis")))
                {
                    sw.Write(filestring);
                }
            }
            catch { }
        }


        /// <summary>
        /// GOPリスト出力ウィンドウ検索用
        /// </summary>
        /// <param name="parentHandle"></param>
        /// <param name="childAfter"></param>
        /// <param name="className"></param>
        /// <param name="windowTitle"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        /// <summary>
        /// logo.lgdのファイル名・番組名
        /// </summary>
        /// string[] ChannelArray = { "東海テレビ０１１", "中京テレビ１", "ＣＢＣ", "メ～テレ", "テレビ愛知１", "ＢＳ－ＴＢＳ", "ＢＳ１１", };


        public void MakeDefaultAVS(string AVSFile, string FilterTemplatePath, string M2VFile, string WavFile)
        {
            /// -------------------------------------------------------------------
            /// 3-2. DefaultのAVS
            /// 
            /// -------------------------------------------------------------------
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("*****************************");
            Console.WriteLine("   デフォルトのAVS出力");
            Console.WriteLine("*****************************");

            string template = ReadFile(FilterTemplatePath);
            template = template.Replace("%m2v", M2VFile);
            template = template.Replace("%wav", WavFile);
            WriteFile(AVSFile, template);
            Console.WriteLine("出力: " + AVSFile);

        }


        /// <summary>
        /// メインプログラム
        /// </summary>
        /// <param name="args"></param>
        public void Run(string[] args)
        {
            try
            {
                string OrgName = args[0];
                string OutputDir = Settings.Default.OutputDir;
                string OutputFileName = "";
                string InputFileName = OrgName;
                string ChannelName = "";

                string M2VFile = Path.ChangeExtension(InputFileName, ".m2v");
                string WavFile = Path.ChangeExtension(InputFileName, ".wav");
                string AVSFile = Path.ChangeExtension(InputFileName, ".avs");

                string BonTsDemuxPath = Settings.Default.BonTsDemuxPath; // @"D:\OnlineSoft\PT2\BonTsDemux\BonTsDemux.exe";
                //string S0ServicePath = Settings.Default.S0ServicePath; // @"O:\PT2\EpgDataCap_Bon\EpgTimerBon\BonDriver_Spinel_PT-S0(Spinel：PT_0／S0).ChSet2.txt";
                //string T0ServicePath = Settings.Default.T0ServicePath;//  @"O:\PT2\EpgDataCap_Bon\EpgTimerBon\BonDriver_Spinel_PT-T0(Spinel：PT_0／T0).ChSet2.txt";
                string ffmpegPath = Settings.Default.ffmpegPath; // @"D:\OnlineSoft\PT2\ffmpeg\ffmpeg.exe";
                string LogoDir = Settings.Default.LogoDir; // @"D:\OnlineSoft\AviUtil\otherfile\lgd";

                string AVS2YUVPath = Settings.Default.AVS2YUVPath;// @"D:\OnlineSoft\AviUtil\otherfile\avs2yuv\avs2yuv.exe";
                string M2VVFPPath = Settings.Default.M2VVFPPath; // @"D:\OnlineSoft\AviUtil\otherfile\logoGuillo\m2v.vfp";
                string LogoGuilloPath = Settings.Default.LogoGuilloPath; // @"D:\OnlineSoft\AviUtil\otherfile\logoGuillo\logoGuillo.exe";
                string AviutlPath = Settings.Default.AviutlPath; // @"D:\OnlineSoft\AviUtil\aviutl.exe";

                string FilterTemplatePath = Settings.Default.FilterTemplatePath; // @"D:\OnlineSoft\AviUtil\otherfile\txt\filtertemplate.txt";
                string PreFilterPath = Settings.Default.PreFilterPath;// @"D:\OnlineSoft\AviUtil\otherfile\txt\prefilter.txt";
                string PostFilterDelogoPath = Settings.Default.PostFilterDelogoPath; // @"D:\OnlineSoft\AviUtil\otherfile\txt\postfilterDelogo.txt";
                string PostFilterSizePath = Settings.Default.PostFilterSizePath; // @"D:\OnlineSoft\AviUtil\otherfile\txt\postfilterSize.txt";
                string PostFilterAnimePath = Settings.Default.PostFilterAnimePath; // @"D:\OnlineSoft\AviUtil\otherfile\txt\postfilterAnime.txt";

                string AviutilProfileNum = Settings.Default.AviutilProfileNum;
                string AviutilPlugoutNum = Settings.Default.AviutilPlugoutNum;
                
                string LogoPath = "";


                Console.WriteLine("");
                Console.WriteLine("********************************************************************");
                Console.WriteLine("    PostRec");
                Console.WriteLine("********************************************************************");
                Console.WriteLine("Start: " + DateTime.Now);
                Console.WriteLine("");
                Console.WriteLine("InputFile: " + InputFileName);
                Console.WriteLine("OutputDir: " + OutputDir);
               

                if (Path.GetExtension(InputFileName).ToLower() != ".ts")
                {
                    Console.WriteLine("tsファイルではない");
                    return;
                }

                //string ProgramTxt = ReadFile(InputFileName + ".program.txt");
                OutputFileName = DoClassify(InputFileName, OutputDir, out ChannelName);

                /// 録画したTSなら
                if (ChannelName != "")
                {
                    //string[] S0ServiceName = ReadFile(S0ServicePath).Split('\n');
                    //string[] T0ServiceName = ReadFile(T0ServicePath).Split('\n');

                    /// -------------------------------------------------------------------
                    /// 1. TSをm2vとwavにdemux
                    /// 
                    /// サービス番号を検索して、そのサービスだけ取り出す。
                    /// 
                    /// EPGTimer_BonのChSet2.txtをSとT両方で読み込み、
                    /// サービス番号は左から4番目(0 order, 3)にあるので、サービス名と一致するサービス番号を取り出す。
                    /// -------------------------------------------------------------------
                    Console.WriteLine("＜録画tsモード＞");
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("*****************************");
                    Console.WriteLine("    BonTSDemuxによる分離");
                    Console.WriteLine("*****************************");

                    /// サービス番号を検索する
                    /// 1番はじめにみつかったサービス名のIDを用いる。
                    //int ServiceNumber = 0;
                    /*foreach (string s in S0ServiceName)
                    {
                        if (s.Contains(ChannelName))
                        {
                            ServiceNumber = int.Parse(s.Split('\t')[7]);
                            break;
                        }
                    }
                    foreach (string s in T0ServiceName)
                    {
                        if (s.Contains(ChannelName))
                        {
                            ServiceNumber = int.Parse(s.Split('\t')[7]);
                            break;
                        }
                    }*/

                    /// サービス名に基づいてBonTsDemux行う。
                    //string servicearg = "";
                    // if (ServiceNumber != 0) servicearg = " -srv " + ServiceNumber.ToString();
                    // Console.WriteLine("Service ID = " + ServiceNumber);

                    /// m2vがあればなにもしない。
                    if (!File.Exists(M2VFile))
                    {
                        Console.WriteLine("Exec : " + BonTsDemuxPath + " -nogui -i " + "\"" + InputFileName + "\"");
                        Exec(BonTsDemuxPath, "-nogui -i " + "\"" + InputFileName + "\"");
                        // Console.WriteLine("Exec : " + BonTsDemuxPath + " -nogui -i " + "\"" + InputFileName + "\"" + servicearg);
                        // Exec(BonTsDemuxPath, "-nogui -i " + "\"" + InputFileName + "\"" + servicearg);
                    }
                    else
                    {
                        Console.WriteLine("m2vが存在するのでスキップ");
                    }

                    Console.WriteLine("BonTSDemux 完了 " + DateTime.Now);

                    /// -------------------------------------------------------------------
                    /// 2. ffmpegで出力解像度の決定
                    /// 
                    /// 結構、SAR4:3だったりSAR1:1だったり1440x1080だったり1920x1080だったり480x480だったり変な形式も多いので、
                    /// Width = w * sarx / sarh
                    /// Height = h
                    /// ということでSAR1:1になるように計算し直す。
                    /// また、計算結果1920x1080になったら、1280x720にダウンスケールするようにした。
                    /// -------------------------------------------------------------------
                    string ffmpegret = ExecRedirect(ffmpegPath, "-i \"" + M2VFile + "\"");

                    Match mm = Regex.Match(ffmpegret, @"(\d+)x(\d+) \[SAR (\d+)\:(\d+)");

                    /// デフォルトの大きさ
                    int x = 1280, y = 720;

                    /// ffmpegがうまく出力できていなかったら、とりあえず1280x720を使う。
                    if (mm.Success)
                    {
                        int mx = int.Parse(mm.Groups[1].Value);
                        int my = int.Parse(mm.Groups[2].Value);
                        int sarx = int.Parse(mm.Groups[3].Value);
                        int sary = int.Parse(mm.Groups[4].Value);
                        x = (int)((float)sarx / (float)sary * mx);
                        y = my;

                        /// ハイビジョンは720iに落とす
                        if (x == 1920 && y == 1080) { x = 1280; y = 720; }
                        if (x == 853 && y == 480) { x = 864; y = 480; }
                    }


                    bool IsAnime = false;
                    bool IsDelogo = false;

                    string[] logos = Directory.GetFiles(LogoDir, "*.lgd");
                    for (int i = 0; i < logos.Length; i++)
                    {
                        logos[i] = Path.GetFileNameWithoutExtension(logos[i]);
                    }

                    /// -------------------------------------------------------------------
                    /// 3. 暫定的AVS出力
                    /// 
                    /// CMカット情報、ファイル情報だけが書き込まれたAVSを出力する。
                    /// -------------------------------------------------------------------
                    if (logos.Contains(ChannelName))
                    {

                        /// -------------------------------------------------------------------
                        /// 3-1. trim情報をlogoguilloで取得
                        /// 
                        /// -------------------------------------------------------------------
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine("*****************************");
                        Console.WriteLine("   logoGuilloによるCMカット");
                        Console.WriteLine("*****************************");

                        LogoPath = Path.Combine(LogoDir, ChannelName + ".lgd");

                        string logoguilloarg = "-video " + "\"" + M2VFile + "\"" +
                            " -lgd " + "\"" + LogoPath + "\"" +
                            " -avs2x " + "\"" + AVS2YUVPath + "\"" +
                            " -avsPlg " + "\"" + M2VVFPPath + "\"" +
                            " -prm " + "\"" + LogoPath + ".autoTune.param" + "\"" +
                            " -out " + "\"" + AVSFile + "\"" +
                            " -outFmt avs " +
                            " -delFstBlk 20.0 -delLstBlk 5.0";

                        if (!File.Exists(AVSFile))
                        {
                            Console.WriteLine("Exec : " + LogoGuilloPath + logoguilloarg);
                            Console.WriteLine();
                            Exec(LogoGuilloPath, logoguilloarg);
                        }
                        else
                        {
                            Console.WriteLine("avsが存在するのでスキップ");
                        }

                        Console.WriteLine("logoGuillo 完了 " + DateTime.Now);

                        try
                        {
                            FileInfo fi = new FileInfo(AVSFile);
                            if (fi.Length == 0)
                            {
                                Console.WriteLine("***********ＡＶＳが出力されなかったのでデフォルト処理*************");
                                MakeDefaultAVS(AVSFile, FilterTemplatePath, M2VFile, WavFile);
                            }
                        }
                        catch { }

                        IsDelogo = true;

                    }
                    // ロゴがない場合はデフォルトのAVS処理
                    else
                    {
                        MakeDefaultAVS(AVSFile, FilterTemplatePath, M2VFile, WavFile);
                        IsDelogo = false;
                    }


                    /// -------------------------------------------------------------------
                    /// 4. .program.txtからジャンル情報を取得
                    /// 
                    /// アニメでないなら、アニメフラグをオフにする。
                    /// -------------------------------------------------------------------
                    //Console.WriteLine();
                    //Console.WriteLine();
                    //Console.WriteLine("*****************************");
                    //Console.WriteLine("    ジャンル情報取得");
                    //Console.WriteLine("*****************************");

                    IsAnime = true;

                    /*if (args[1] == "no-anime"){ // 非アニメ指定されてるならfalseにしておく
                        IsAnime = false;
                    }*/

                    //try
                    //{
                    //    Match m = Regex.Match(ProgramTxt, "ジャンル : .*? : ", RegexOptions.Singleline);
                    //    if (m.Success)
                    //    {
                    //        Console.WriteLine(m.Value);
                    //        if (!m.Value.Contains("アニメ") || m.Value.Contains("ドラマ") || m.Value.Contains("バラエティ"))
                    //        {
                    //            IsAnime = false;
                    //        }
                    //    }
                    //}
                    //catch { }




                    /// -------------------------------------------------------------------
                    /// 5. .avsの微調整
                    /// 
                    /// Delogoできるならロゴ除去を。
                    /// サイズ変更はすべてに。
                    /// アニメならアニメ用フィルタをかける。
                    /// -------------------------------------------------------------------
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("*****************************");
                    Console.WriteLine("   AVSファイルの書き換え");
                    Console.WriteLine("*****************************");
                    string AVSText = ReadFile(AVSFile);

                    string PreFilter = ReadFile(PreFilterPath);
                    string PostFilterDelogo = ReadFile(PostFilterDelogoPath);
                    string PostFilterSize = ReadFile(PostFilterSizePath);
                    string PostFilterAnime = ReadFile(PostFilterAnimePath);


                    AVSText = AVSText.Replace("#REPKEY_1#", PreFilter);

                    /// %lはロゴのパスに変換される。
                    PostFilterDelogo = PostFilterDelogo.Replace("%l", LogoPath);

                    /// %x, %yはリサイズ後のファイルサイズに変換される。
                    PostFilterSize = PostFilterSize.Replace("%x", x.ToString()).Replace("%y", y.ToString());

                    /// ロゴ削除のフィルタ
                    if (IsDelogo)
                    {
                        Console.WriteLine("+ ロゴ除去フィルタ　: " + LogoPath);
                        AVSText = AVSText.Replace("#REPKEY_30#", PostFilterDelogo);
                    }

                    /// リサイズフィルタ1
                    if (AviutilPlugoutNum == "1")
                    {
                        Console.WriteLine("+ リサイズフィルタ　： " + x.ToString() + "x" + y.ToString());
                        AVSText = AVSText.Replace("#REPKEY_31#", PostFilterSize);
                    }

                    /// アニメフィルタ
                    if (IsAnime)
                    {
                        Console.WriteLine("+ アニメ向けフィルタ： Warpsharp + Edge強調");
                        AVSText = AVSText.Replace("#REPKEY_32#", PostFilterAnime);
                    }

                    /// 出力
                    WriteFile(AVSFile, AVSText);
                    Console.WriteLine("AVSファイル出力: " + AVSFile);
                    Console.WriteLine("--------------------");
                    Console.WriteLine("AVSファイル内容");
                    Console.WriteLine("--------------------");
                    Console.WriteLine(AVSText);
                    Console.WriteLine("----------------------------------------------------------------------------");

                    /// -------------------------------------------------------------------
                    /// 6. aviutlで出力
                    /// 
                    /// 本来なら、x264とNeroAACEncとmp4boxなんかでやるべき何だけど
                    /// avs2wavがうまくいかなかったのでaviutlに頼ることに。
                    /// -------------------------------------------------------------------

                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("*****************************");
                    Console.WriteLine("    Aviutlで出力");
                    Console.WriteLine("*****************************");


                    /// とにかく新しく実行する
                    object ret = Auc("exec", "\"" + AviutlPath + "\"");
                    int hwnd = (int)ret;
                    if (hwnd == 0) return;


                    /// 
                    if (File.Exists(Path.ChangeExtension(InputFileName, ".gl")))
                    {
                        Auc("open", hwnd.ToString() + " \"" + AVSFile + "\"");
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Auc("open", hwnd.ToString() + " \"" + AVSFile + "\"");
                        Thread.Sleep(5000);
                        while ((int)FindWindowEx(IntPtr.Zero, IntPtr.Zero, "#32770", "MPEG-2 VIDEO VFAPI Plug-In : 情報") != 0)
                        {
                            Thread.Sleep(1000);
                        }
                    }

                    //int prof = 0;
                    Auc("setprof", hwnd.ToString() + " " + AviutilProfileNum);
                    Thread.Sleep(1000);

                    Console.WriteLine("mp4エンコードを開始 " + DateTime.Now);
                    Console.WriteLine("出力プラグインNo.: " + AviutilPlugoutNum);
                    Auc("plugout", hwnd.ToString() + " " + AviutilPlugoutNum + " \"" + OutputFileName + "\"");
                    Thread.Sleep(1000);

                    Auc("wait", hwnd.ToString());

                    Auc("exit", hwnd.ToString());
                    Thread.Sleep(1000);


                    if (File.Exists(OutputFileName))
                    {
                        FileInfo fi = new FileInfo(OutputFileName);
                        /// 10MB以上なら、たぶん成功してる
                        if (fi.Length > 1024 * 1024 * 10)
                        {
                            Console.WriteLine("mp4エンコード成功、ファイルを移動します " + DateTime.Now);
                            /// 中間ファイルを削除し、ファイルを移動させる。
                            string[] files = Directory.GetFiles(Path.GetDirectoryName(InputFileName), Path.GetFileNameWithoutExtension(InputFileName) + "*");
                            foreach (string s in files)
                            {
                                try
                                {
                                    /// とりあえずtsだけ移動先に動かして、残りは削除
                                    if (s == InputFileName)
                                    {
                                        File.Move(s, Path.Combine(Settings.Default.ConvertedFilePath, Path.GetFileName(s)));
                                    }
                                    else
                                        File.Delete(s);
                                }
                                catch { }
                            }
                        }
                        else {
                            Console.WriteLine("出力ファイルサイズが10MB以下のため、エンコードに失敗した可能性があります");
                            Console.WriteLine("FileSize: " + fi.Length);

                            string[] files = Directory.GetFiles(Path.GetDirectoryName(InputFileName), Path.GetFileNameWithoutExtension(InputFileName) + "*");
                            foreach (string s in files)
                            {
                                try
                                {
                                    /// とりあえずtsだけ移動先に動かして、残りは削除
                                    if (s == InputFileName)
                                    {
                                        File.Move(s, Path.Combine(Settings.Default.ErrorFilePath, Path.GetFileName(s)));
                                    }
                                    else
                                        File.Delete(s);
                                }
                                catch { }
                            }

                        }
                        Console.WriteLine("OutputFileName: " + OutputFileName);
                    }
                }
                else
                {
                    //Console.WriteLine("＜通常エンコードモード＞");
                    //Console.WriteLine("HandBrakeでエンコードを開始します。");
                    /// CMカットができないなら
                    /// デフォルトのエンコード
                    //string EncoderArgs = Settings.Default.DefaultEncoderArgs.Replace("%1", InputFileName);
                    //EncoderArgs = EncoderArgs.Replace("%2", OutputFileName);
                    //Exec(Settings.Default.DefaultEncoderPath, EncoderArgs);
                    /// 変換後のファイルを移動
                    //if (Settings.Default.ConvertedFilePath != "")
                    //    File.Move(InputFileName, Path.Combine(Settings.Default.ConvertedFilePath, Path.GetFileName(InputFileName)));
                }
            }
            catch (Exception e)
            {
                using (StreamWriter sw = new StreamWriter(Settings.Default.LogFileName, true, Encoding.GetEncoding("shift-jis")))
                {
                    sw.WriteLine(DateTime.Now.ToString());
                    sw.WriteLine(e.Message);
                    sw.WriteLine(e.InnerException);
                    sw.WriteLine(e.StackTrace);
                    sw.WriteLine("------------");
                }

            }
        }



        /// <summary>
        /// Args 
        /// [0] input file
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "help" || args[0] == "/?" || args[0] == "/h")
            {
                Console.WriteLine("Please enter a Input file name.");
                Console.WriteLine("Usage: PostRec.exe <inputfile> [option]");
                Console.WriteLine(" [Options]");
                Console.WriteLine("   help                   : Display help");
                Console.WriteLine("   get-config             : Display all config");
                Console.WriteLine("   reset-config           : Reset all config");
                Console.WriteLine("   set-CONFIG [VALUE]     : config setting");
                Console.WriteLine("   no-anime               : disable filters for Animation program");
                Console.WriteLine("   -CONFIG-");
                Console.WriteLine("   set-BonTsDemuxPath");
                Console.WriteLine("   set-ffmpegPath");
                Console.WriteLine("   set-OutputDir");
                Console.WriteLine("   set-LogoDir");
                Console.WriteLine("   set-AVS2YUVPath");
                Console.WriteLine("   set-M2VVFPPath");
                Console.WriteLine("   set-LogoGuilloPath");
                Console.WriteLine("   set-AviutlPath");
                Console.WriteLine("   set-FilterTemplatePath");
                Console.WriteLine("   set-PreFilterPath");
                Console.WriteLine("   set-PostFilterDelogoPath");
                Console.WriteLine("   set-PostFilterSizePath");
                Console.WriteLine("   set-PostFilterAnimePath");
                Console.WriteLine("   set-AUCDir");
                Console.WriteLine("   set-ConvertedFilePath");
                Console.WriteLine("   set-LogFileName");
                return;
            }
            else if (args[0] == "reset-config")
            {
                Settings.Default.Reset();
                Console.WriteLine("Reset all config.");
                return;
            }
            else if (args[0] == "set-BonTsDemuxPath")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("BonTsDemuxPath:" + Settings.Default.BonTsDemuxPath);
                return;
            }
            else if (args[0] == "set-ffmpegPath")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("ffmpegPath:" + Settings.Default.ffmpegPath);
                return;
            }
            else if (args[0] == "set-OutputDir")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("OutputDir:" + Settings.Default.OutputDir);
                return;
            }
            else if (args[0] == "set-LogoDir")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("LogoDir:" + Settings.Default.LogoDir);
                return;
            }
            else if (args[0] == "set-AVS2YUVPath")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("AVS2YUVPath:" + Settings.Default.AVS2YUVPath);
                return;
            }
            else if (args[0] == "set-M2VVFPPath")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("M2VVFPPath:" + Settings.Default.M2VVFPPath);
                return;
            }
            else if (args[0] == "set-LogoGuilloPath")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("LogoGuilloPath:" + Settings.Default.LogoGuilloPath);
                return;
            }
            else if (args[0] == "set-AviutlPath")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("AviutlPath:" + Settings.Default.AviutlPath);
                return;
            }
            else if (args[0] == "set-FilterTemplatePath")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("FilterTemplatePath:" + Settings.Default.FilterTemplatePath);
                return;
            }
            else if (args[0] == "set-PreFilterPath")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("PreFilterPath:" + Settings.Default.PreFilterPath);
                return;
            }
            else if (args[0] == "set-PostFilterDelogoPath")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("PostFilterDelogoPath:" + Settings.Default.PostFilterDelogoPath);
                return;
            }
            else if (args[0] == "set-PostFilterSizePath")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("PostFilterSizePath:" + Settings.Default.PostFilterSizePath);
                return;
            }
            else if (args[0] == "set-PostFilterAnimePath")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("PostFilterAnimePath:" + Settings.Default.PostFilterAnimePath);
                return;
            }
            else if (args[0] == "set-AUCDir")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("AUCDir:" + Settings.Default.AUCDir);
                return;
            }
            else if (args[0] == "set-ConvertedFilePath")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("ConvertedFilePath:" + Settings.Default.ConvertedFilePath);
                return;
            }
            else if (args[0] == "set-logfilename")
            {
                //Settings.Default.LogFileName = args[1];
                Settings.Default.Save();
                Console.WriteLine("LogFileName:" + Settings.Default.LogFileName);
                return;
            }
            else if (args[0] == "get-config")
            {
                Console.WriteLine("BonTsDemuxPath:       " + Settings.Default.BonTsDemuxPath);
                Console.WriteLine("ffmpegPath:           " + Settings.Default.ffmpegPath);
                Console.WriteLine("OutputDir:            " + Settings.Default.OutputDir);
                Console.WriteLine("LogoDir:              " + Settings.Default.LogoDir);
                Console.WriteLine("AVS2YUVPath:          " + Settings.Default.AVS2YUVPath);
                Console.WriteLine("M2VVFPPath:           " + Settings.Default.M2VVFPPath);
                Console.WriteLine("LogoGuilloPath:       " + Settings.Default.LogoGuilloPath);
                Console.WriteLine("AviutlPath:           " + Settings.Default.AviutlPath);
                Console.WriteLine("FilterTemplatePath:   " + Settings.Default.FilterTemplatePath);
                Console.WriteLine("PreFilterPath:        " + Settings.Default.PreFilterPath);
                Console.WriteLine("PostFilterDelogoPath: " + Settings.Default.PostFilterDelogoPath);
                Console.WriteLine("PostFilterSizePath:   " + Settings.Default.PostFilterSizePath);
                Console.WriteLine("PostFilterAnimePath:  " + Settings.Default.PostFilterAnimePath);
                Console.WriteLine("AUCDir:               " + Settings.Default.AUCDir);
                Console.WriteLine("ConvertedFilePath:    " + Settings.Default.ConvertedFilePath);
                Console.WriteLine("LogFileName:          " + Settings.Default.LogFileName);
                return;
            }
            
            Program p = new Program();
            p.Run(args);
        }
    }
}
