﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using FluentFTP;
using System.Net.Sockets;
using System.Timers;

namespace ILEditor.Classes
{
    class IBMi
    {
        public static Config CurrentSystem;
        private static FtpClient Client;

        public readonly static Dictionary<string, string> FTPCodeMessages = new Dictionary<string, string>()
        {
            { "425", "Not able to open data connection. This might mean that your system is blocking either: FTP, port 20 or port 21. Please allow these through the Windows Firewall. Check the Welcome screen for a 'Getting an FTP error?' and follow the instructions." },
            { "426", "Connection closed; transfer aborted. The file may be locked." },
            { "426T", "Member was saved but characters have been truncated as record length has been reached." },
            { "426L", "Member was not saved due to a possible lock." },
            { "426F", "Member was not found. Perhaps it was deleted." },
            { "530", "Configuration username and password incorrect." }
        };

        public static void HandleError(string Code, string Message)
        {
            string ErrorMessageText = "";
            switch (Code)
            {
                case "200":
                    ErrorMessageText = "425";
                    break;

                case "425":
                case "426":
                case "530":
                case "550":
                    ErrorMessageText = Code;

                    switch (Code)
                    {
                        case "426":
                            if (Message.Contains("truncated"))
                                ErrorMessageText = "426T";

                            else if (Message.Contains("Unable to open or create"))
                                ErrorMessageText = "426L";

                            else if (Message.Contains("not found"))
                                ErrorMessageText = "426F";

                            break;
                        case "550":
                            if (Message.Contains("not created in"))
                                ErrorMessageText = "550NC";
                            break;
                    }

                    break;
            }

            if (FTPCodeMessages.ContainsKey(ErrorMessageText))
                MessageBox.Show(FTPCodeMessages[ErrorMessageText], "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static FtpDataConnectionType GetFtpDataConnectionType(string Type)
        {
            if (Enum.TryParse(Type, out FtpDataConnectionType result))
                return result;
            else
                return FtpDataConnectionType.AutoActive;
        }

        public static bool IsConnected()
        {
            if (Client != null)
                return Client.IsConnected;
            else
                return false;
        }
        public static string FTPFile = "";
        public static bool Connect(bool OfflineMode = false, string promptedPassword = "")
        {
            string[] remoteSystem;
            bool result = false;
            try
            {
                FTPFile = IBMiUtils.GetLocalFile("QTEMP", "FTPLOG", DateTime.Now.ToString("MMddTHHmm"), "txt");
                FtpTrace.AddListener(new TextWriterTraceListener(FTPFile));
                FtpTrace.LogUserName = false;   // hide FTP user names
                FtpTrace.LogPassword = false;   // hide FTP passwords
                FtpTrace.LogIP = false; 	// hide FTP server IP addresses

                string password = "";

                remoteSystem = CurrentSystem.GetValue("system").Split(':');

                if (promptedPassword == "")
                    password = Password.Decode(CurrentSystem.GetValue("password"));
                else
                    password = promptedPassword;

                Client = new FtpClient(remoteSystem[0], CurrentSystem.GetValue("username"), password);

                if (OfflineMode == false)
                {
                    Client.UploadDataType = FtpDataType.Binary;     //ymurata1967 ASCIIからBINARYに変更
                    Client.DownloadDataType = FtpDataType.Binary;   //ymurata1967 ASCIIからBINARYに変更

                    //FTPES is configurable
                    if (IBMi.CurrentSystem.GetValue("useFTPES") == "true")
                        Client.EncryptionMode = FtpEncryptionMode.Explicit;

                    //Client.DataConnectionType = FtpDataConnectionType.AutoPassive; //THIS IS THE DEFAULT VALUE
                    Client.DataConnectionType = GetFtpDataConnectionType(CurrentSystem.GetValue("transferMode"));
                    Client.SocketKeepAlive = true;

                    if (remoteSystem.Length == 2)
                        Client.Port = int.Parse(remoteSystem[1]);

                    Client.ConnectTimeout = 5000;
                    Client.Connect();

                    //Change the user library list on connection
                    RemoteCommand($"CHGLIBL LIBL({ CurrentSystem.GetValue("datalibl").Replace(',', ' ')}) CURLIB({ CurrentSystem.GetValue("curlib") })");
                    RemoteCommand($"MKDIR DIR('{JpUtils.GetRootTmpDir()}')");   //ymurata1967 ワークフォルダ作成
                    RemoteCommand($"MKDIR DIR('{JpUtils.GetTmpDir()}')");       //ymurata1967 ワークフォルダ作成

                    System.Timers.Timer timer = new System.Timers.Timer();
                    timer.Interval = 60000;
                    timer.Elapsed += new ElapsedEventHandler(KeepAliveFunc);
                    timer.Start();
                }

                result = true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to connect to " + CurrentSystem.GetValue("system") + " - " + e.Message, "Cannot Connect", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return result;
        }

        public static void Disconnect()
        {
            if (Client.IsConnected)
            {
                Client.Disconnect();
            }
        }

        private static void KeepAliveFunc(object sender, ElapsedEventArgs e)
        {
            bool showError = !Client.IsConnected;
            if (Client.IsConnected)
            {
                try {
                    Client.Execute("NOOP");
                    showError = false;
                }
                catch
                {
                    showError = true;
                }
            }

            if (showError)
                Editor.TheEditor.SetStatus("Warning! You lost connection " + CurrentSystem.GetValue("system") + "!");
        }

        public static string GetSystem()
        {
            if (Client != null)
                if (Client.IsConnected)
                    return Client.SystemType;
                else
                    return "";
            else
                return "";
        }

        //Returns false if successful
        public static bool DownloadFile(string Local, string Remote)
        {
            bool Result = false;
            try
            {
                if (Client.IsConnected)
                {
                    //---- ymurata1967 メンバーはtrimする----//
                    //ダウンロードファイル名でメンバーかそれ以外かを判断。
                    RemoteSource SourceFile;
                    SourceFile = new RemoteSource("", Remote);
                    if (SourceFile.GetName() == "DOWNMBR")
                    {
                        //FTPで落とすダウンロード先のファイル名をファイル名+.tmpとする。
                        String tempLocal = Local + ".tmp";
                        string line;
                        string writeStr = "";
                        Result = !Client.DownloadFile(tempLocal, Remote, true);
                        StreamReader sr = new StreamReader(tempLocal, Program.Encoding);   //ダウンロードしたファイルをSJISで開く
                        while ((line = sr.ReadLine()) != null)
                        {
                            writeStr += line.TrimEnd() + Environment.NewLine;   //Trimと改行をセット
                        }
                        sr.Close();
                        File.WriteAllText(Local, writeStr, Program.Encoding);  //writeStrを書き出し（Localがエディタに表示される）
                        File.Delete(tempLocal);//テンポラリファイルを削除
                     //---- ymurata1967 ----//
                    }
                    else
                    {
                        Result = !Client.DownloadFile(Local, Remote, true);
                    }
                }
                else
                    return true; //error
            }
            catch (Exception e)
            {
                if (e.InnerException is FtpCommandException)
                {
                    FtpCommandException err = e.InnerException as FtpCommandException;
                    HandleError(err.CompletionCode, err.Message);
                }
                Result = true;
            }

            return Result;
        }

        //Returns true if successful
        public static bool UploadFile(string Local, string Remote)
        {
            if (Client.IsConnected)
            {
                //バイナリでアップロードするファイル名の取得 ymurata1967 
                String tempRemote = JpUtils.GetUpTmpFileName();
                return Client.UploadFile(Local, tempRemote, FtpExists.Overwrite);
            }
            else
                return false;
        }

        //Returns true if successful
        public static bool RemoteCommand(string Command, bool ShowError = true)
        {
            if (Client.IsConnected)
            {
                string inputCmd = "RCMD " + Command;
                //IF THIS CRASHES CLIENT DISCONNECTS!!!
                FtpReply reply = Client.Execute(inputCmd);

                if (ShowError)
                    HandleError(reply.Code, reply.ErrorMessage);

                return reply.Success;
            }
            else
            {
                return false;
            }
        }

        public static string RemoteCommandResponse(string Command)
        {
            if (Client.IsConnected)
            {
                string inputCmd = "RCMD " + Command;
                FtpReply reply = Client.Execute(inputCmd);

                if (reply.Success)
                    return "";
                else
                    return reply.ErrorMessage;
            }
            else
            {
                return "Not connected.";
            }
        }

        //Returns true if successful
        public static bool RunCommands(string[] Commands)
        {
            bool result = true;
            if (Client.IsConnected)
            {
                foreach (string Command in Commands)
                {
                    if (RemoteCommand(Command) == false)
                        result = false;
                }
            }
            else
            {
                result = false;
            }

            return result;
        }

        public static bool FileExists(string remoteFile)
        {
            return Client.FileExists(remoteFile);
        }
        public static bool DirExists(string remoteDir)
        {
            try
            {
                return Client.DirectoryExists(remoteDir);
            }
            catch (Exception ex)
            {
                Editor.TheEditor.SetStatus(ex.Message + " - please try again.");
                return false;
            }
        }
        public static FtpListItem[] GetListing(string remoteDir)
        {
            return Client.GetListing(remoteDir);
        }

        public static string RenameDir(string remoteDir, string newName)
        {
            string[] pieces = remoteDir.Split('/');
            pieces[pieces.Length - 1] = newName;
            newName = String.Join("/", pieces);

            if (Client.MoveDirectory(remoteDir, String.Join("/", pieces)))
                return newName;
            else
                return remoteDir;
        }
        public static string RenameFile(string remoteFile, string newName)
        {
            string[] pieces = remoteFile.Split('/');
            pieces[pieces.Length - 1] = newName;
            newName = String.Join("/", pieces);

            if (Client.MoveFile(remoteFile, newName))
                return newName;
            else
                return remoteFile;
        }

        public static void DeleteDir(string remoteDir)
        {
            Client.DeleteDirectory(remoteDir, FtpListOption.AllFiles);
        }

        public static void DeleteFile(string remoteFile)
        {
            Client.DeleteFile(remoteFile);
        }

        public static void SetWorkingDir(string RemoteDir)
        {
            Client.SetWorkingDirectory(RemoteDir);
        }
        public static void CreateDirecory(string RemoteDir)
        {
            Client.CreateDirectory(RemoteDir);
        }
        public static void UploadFiles(string RemoteDir, string[] Files)
        {
            Client.UploadFiles(Files, RemoteDir, FtpExists.Overwrite, true);
        }
    }
}
