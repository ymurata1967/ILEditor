using ILEditor.UserTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ILEditor.Classes
{
    class IBMiUtils
    {
        private static List<string> QTEMPListing = new List<string>();
        //This method is used to determine whether a file in QTEMP needs to be deleted
        //If it's already exists in QTEMP, we delete it - otherwise we delete it next time
        public static void UsingQTEMPFiles(string[] Objects)
        {
            foreach (string Object in Objects) 
                if (QTEMPListing.Contains(Object))
                    IBMi.RemoteCommand("DLTOBJ OBJ(QTEMP/" + Object + ") OBJTYPE(*FILE)", false);
                else
                    QTEMPListing.Add(Object);
        }

        public static Boolean IsValueObjectName(string Name)
        {
            if (Name.Trim() == "")
                return false;

            if (Name.Length > 10)
                return false;

            return true;
        }

        private static string GetCurrentSystem() => IBMi.CurrentSystem.GetValue("system").Split(':')[0];

        public static BindingEntry[] GetBindingDirectory(string Lib, string Obj)
        {
            if (IBMi.IsConnected())
            {
                BindingEntry Entry;
                List<BindingEntry> Entries = new List<BindingEntry>();
                if (Lib == "*CURLIB") Lib = IBMi.CurrentSystem.GetValue("curlib");
                
                UsingQTEMPFiles(new[] { "BNDDIR", "BNDDATA" });

                //ymurata1967 Start
                IBMi.RemoteCommand("RUNSQL SQL('CREATE TABLE QTEMP/BNDDATA (A GRAPHIC(198) CCSID 1200 NOT NULL)') COMMIT(*NONE)");
                IBMi.RemoteCommand("DSPBNDDIR BNDDIR(" + Lib + "/" + Obj + ") OUTPUT(*OUTFILE) OUTFILE(QTEMP/BNDDIR)");
                IBMi.RemoteCommand("RUNSQL SQL('INSERT INTO QTEMP/BNDDATA (SELECT TRIM(BNOBNM)||'',''||TRIM(BNOBTP)||'',''||TRIM(BNOLNM)||'',''||TRIM(BNOACT)||'',''||BNODAT||'',''||BNOTIM FROM QTEMP/BNDDIR ORDER BY BNOBNM)') COMMIT(*NONE)");
                IBMi.RemoteCommand("CPYTOIMPF FROMFILE(QTEMP/BNDDATA) TOSTMF('" + JpUtils.GetDwFileName() + "') MBROPT(*REPLACE) STMFCCSID(943) RCDDLM(*CRLF) DTAFMT(*FIXED) RMVBLANK(*TRAILING)");
                string file = DownloadMember("QTEMP", "BNDDATA", "BNDDATA", JpUtils.GetDwFileName());
                //ymurata1967 End
                if (file != "")
                {
                    //ymurata1967 ファイルの中身が無い場合はnullを返却するように改修
                    string[] lines = File.ReadAllLines(file, Program.Encoding);
                    if (lines.Length == 0)
                    {
                        return null;
                    }

                    foreach (string line in lines)
                    {
                        if (line.Trim() != "")
                        {
                            Entry = new BindingEntry();
                            //ymurata1967 Start
                            string[] arr = line.Split(',');
                            Entry.BindingLib = Lib;
                            Entry.BindingObj = Obj;
                            Entry.Name = arr[0];
                            Entry.Type = arr[1];
                            Entry.Library = arr[2];
                            Entry.Activation = arr[3];
                            Entry.CreationDate = arr[4].Trim();
                            Entry.CreationTime = arr[5].Trim();
                            //ymurata1967 End
                            Entries.Add(Entry);
                        }
                    }
                }
                else
                {
                    return null;
                }

                return Entries.ToArray();
            }
            else
            {
                return null;
            }
        }

        public static ILEObject[] GetObjectList(string Lib, string Types = "*PGM *SRVPGM *MODULE")
        {
            if (IBMi.IsConnected())
            {
                ILEObject Object;
                List<ILEObject> Objects = new List<ILEObject>();
                if (Lib == "*CURLIB") Lib = IBMi.CurrentSystem.GetValue("curlib");

                string FileA = 'O' + Lib, FileB = "T" + Lib;

                if (FileA.Length > 10)
                    FileA = FileA.Substring(0, 10);
                if (FileB.Length > 10)
                    FileB = FileB.Substring(0, 10);
                
                UsingQTEMPFiles(new[] { FileA, FileB });

                //ymurata1967 Start
                IBMi.RemoteCommand("RUNSQL SQL('CREATE TABLE QTEMP/" + FileB + " (A GRAPHIC(198) CCSID 1200 NOT NULL)') COMMIT(*NONE)");
                IBMi.RemoteCommand("DSPOBJD OBJ(" + Lib + "/*ALL) OBJTYPE(" + Types + ") OUTPUT(*OUTFILE) OUTFILE(QTEMP/" + FileA + ")");
                IBMi.RemoteCommand("RUNSQL SQL('INSERT INTO QTEMP/" + FileB + " (SELECT TRIM(ODOBNM)||'',''||TRIM(ODOBTP)||'',''||TRIM(ODOBAT)||'',''||TRIM(CHAR(ODOBSZ))||'',''||TRIM(REPLACE(ODOBTX,'','',''''))||'',''||TRIM(ODOBOW)||'',''||TRIM(ODSRCF)||'',''||TRIM(ODSRCL)||'',''||TRIM(ODSRCM) FROM QTEMP/" + FileA + " ORDER BY ODOBNM)') COMMIT(*NONE)");
                IBMi.RemoteCommand("CPYTOIMPF FROMFILE(QTEMP/" + FileB + ") TOSTMF('" + JpUtils.GetDwFileName() + "') MBROPT(*REPLACE) STMFCCSID(943) RCDDLM(*CRLF) DTAFMT(*FIXED) RMVBLANK(*TRAILING)");
                string file = DownloadMember("QTEMP", FileB, FileB, JpUtils.GetDwFileName());
                //ymurata1967 End
                if (file != "")
                {
                    //ymurata1967 ファイルの中身が無い場合はnullを返却
                    string[] lines = File.ReadAllLines(file, Program.Encoding);
                    if (lines.Length == 0)
                    {
                        return null;
                    }

                    foreach (string line in lines)
                    {

                        if (line.Trim() != "")
                        {
                            Object = new ILEObject();
                            //ymurata1967 Start
                            string[] arr = line.Split(',');
                            Object.Library = Lib;
                            Object.Name = arr[0];
                            Object.Type = arr[1];
                            Object.Extension = arr[2];
                            UInt32.TryParse(arr[3], out Object.SizeKB);
                            Object.Text = arr[4];
                            Object.Owner = arr[5];
                            Object.SrcSpf = arr[6];
                            Object.SrcLib = arr[7];
                            Object.SrcMbr = arr[8];
                            //ymurata1967 End
                            Objects.Add(Object);
                        }
                    }
                }
                else
                {
                    return null;
                }

                return Objects.ToArray();
            }
            else
            {
                return null;
            }
        }

        private static readonly string[] IgnorePFs = new[] {
            "EVFTEMP",
            "QSQLTEMP"
        };
        public static ILEObject[] GetSPFList(string Lib)
        {
            List<ILEObject> SPFList = new List<ILEObject>();
            Lib = Lib.ToUpper();
            if (Lib == "*CURLIB") Lib = IBMi.CurrentSystem.GetValue("curlib");
            if (IBMi.IsConnected())
            {
                string FileA = 'S' + Lib, FileB = "D" + Lib;

                if (FileA.Length > 10)
                    FileA = FileA.Substring(0, 10);
                if (FileB.Length > 10)
                    FileB = FileB.Substring(0, 10);

                UsingQTEMPFiles(new[] { FileA, FileB });

                Editor.TheEditor.SetStatus("Fetching source-physical files for " + Lib + "...");

                //ymurata1967 Start
                IBMi.RemoteCommand("RUNSQL SQL('CREATE TABLE QTEMP/" + FileB + " (A GRAPHIC(198) CCSID 1200 NOT NULL)') COMMIT(*NONE)");
                IBMi.RemoteCommand("DSPFD FILE(" + Lib + "/*ALL) TYPE(*ATR) OUTPUT(*OUTFILE) FILEATR(*PF) OUTFILE(QTEMP/" + FileA + ")");
                IBMi.RemoteCommand("RUNSQL SQL('INSERT INTO QTEMP/" + FileB + " (SELECT TRIM(PHFILE)||'',''||TRIM(PHLIB) FROM QTEMP/" + FileA + " WHERE PHDTAT = ''S'' ORDER BY PHFILE)') COMMIT(*NONE)");
                IBMi.RemoteCommand("CPYTOIMPF FROMFILE(QTEMP/" + FileB + ") TOSTMF('" + JpUtils.GetDwFileName() + "') MBROPT(*REPLACE) STMFCCSID(943) RCDDLM(*CRLF) DTAFMT(*FIXED) RMVBLANK(*TRAILING)");
                string file = DownloadMember("QTEMP", FileB, FileB, JpUtils.GetDwFileName());
                //ymurata1967 End
                if (file != "")
                {
                    Boolean validName = true;
                    string Library, Object;
                    ILEObject Obj;

                    //ymurata1967 ファイルの中身が無い場合はnullを返却
                    string[] lines = File.ReadAllLines(file, Program.Encoding);
                    if (lines.Length == 0)
                    {
                        return null;
                    }

                    foreach (string line in lines)
                    {
                        if (line.Trim() != "")
                        {
                            validName = true;
                            //ymurata1967 Start
                            string[] arr = line.Split(',');
                            Object = arr[0];
                            Library = arr[1];
                            //ymurata1967 End
                            Obj = new ILEObject();
                            Obj.Library = Library;
                            Obj.Name = Object;

                            foreach (string Name in IgnorePFs)
                            {
                                if (Obj.Name.StartsWith(Name))
                                    validName = false;
                            }

                            if (validName)
                                SPFList.Add(Obj);
                        }
                    }
                }
                else
                {
                    return null;
                }
                Editor.TheEditor.SetStatus("Fetched source-physical files for " + Lib + ".");
            }
            else
            {
                string DirPath = GetLocalDir(Lib);
                if (Directory.Exists(DirPath)) {
                    foreach (string dir in Directory.GetDirectories(DirPath)) {
                        SPFList.Add(new ILEObject { Library = Lib, Name = Path.GetDirectoryName(dir) });
                    }
                }
                else
                {
                    return null;
                }
            }

            return SPFList.ToArray();
        }

        public static RemoteSource[] GetMemberList(string Lib, string Obj)
        {
            string Line, Object, Name, Desc, Type, RcdLen;
            List<RemoteSource> Members = new List<RemoteSource>();
            RemoteSource NewMember;

            Lib = Lib.ToUpper();
            Obj = Obj.ToUpper();

            if (IBMi.IsConnected())
            {

                if (Lib == "*CURLIB") Lib = IBMi.CurrentSystem.GetValue("curlib");
                Editor.TheEditor.SetStatus("Fetching members for " + Lib + "/" + Obj + "...");

                string TempName = 'M' + Obj;
                if (TempName.Length > 10)
                    TempName = TempName.Substring(0, 10);
                
                UsingQTEMPFiles(new[] { TempName, Obj });

                //ymurata1967 Start
                IBMi.RemoteCommand("RUNSQL SQL('CREATE TABLE QTEMP/" + Obj + " (A GRAPHIC(198) CCSID 1200 NOT NULL)') COMMIT(*NONE)");
                IBMi.RemoteCommand("DSPFD FILE(" + Lib + "/" + Obj + ") TYPE(*MBR) OUTPUT(*OUTFILE) OUTFILE(QTEMP/" + TempName + ")");
                IBMi.RemoteCommand("RUNSQL SQL('INSERT INTO QTEMP/" + Obj + " (SELECT TRIM(MBFILE)||'',''||TRIM(MBNAME)||'',''||TRIM(REPLACE(MBMTXT,'','',''''))||'',''||TRIM(MBSEU2)||'',''||CHAR(TRIM(MBMXRL)) AS MBMXRL FROM QTEMP/" + TempName + " ORDER BY MBNAME)') COMMIT(*NONE)");
                IBMi.RemoteCommand("CPYTOIMPF FROMFILE(QTEMP/" + Obj + ") TOSTMF('" + JpUtils.GetDwFileName() + "') MBROPT(*REPLACE) STMFCCSID(943) RCDDLM(*CRLF) DTAFMT(*FIXED) RMVBLANK(*TRAILING)");
                string file = DownloadMember("QTEMP", Obj, Obj, JpUtils.GetDwFileName());
                //ymurata1967 End

                if (file != "")
                {
                    //ymurata1967 ファイルの中身が無い場合はnullを返却
                    string[] lines = File.ReadAllLines(file, Program.Encoding);
                    if (lines.Length == 0)
                    {
                        return null;
                    }

                    foreach (string RealLine in lines)
                    {
                        if (RealLine.Trim() != "")
                        {
                            //ymurata1967 Start
                            Line = RealLine;
                            string[] arr = Line.Split(',');
                            Object = arr[0];
                            Name = arr[1];
                            Desc = arr[2];
                            Type = arr[3];
                            RcdLen = arr[4];
                            //ymurata1967 End

                            if (Name != "")
                            {
                                NewMember = new RemoteSource("", Lib, Object, Name, Type, true, int.Parse(RcdLen) - 12);
                                NewMember._Text = Desc;

                                Members.Add(NewMember);
                                FileCache.AddMemberCache(Lib + "/" + Object + "." + Name, Type);
                            }
                        }
                    }
                }
                else
                {
                    return null;    //※このルーチンには入りません。
                }

                Editor.TheEditor.SetStatus("Fetched members for " + Lib + " / " + Obj + ".");
            }
            else
            {
                string DirPath = GetLocalDir(Lib, Obj);
                if (Directory.Exists(DirPath))
                {
                    foreach (string file in Directory.GetFiles(DirPath))
                    {
                        Type = Path.GetExtension(file).ToUpper();
                        if (Type.StartsWith(".")) Type = Type.Substring(1);
                        
                        NewMember = new RemoteSource(file, Lib, Obj, Path.GetFileNameWithoutExtension(file), Type);
                        NewMember._Text = "";
                        Members.Add(NewMember);
                    }
                }
                else
                {
                    return null;
                }
            }

            return Members.ToArray();
        }

        public static List<ILEObject[]> GetProgramReferences(string Lib, string Obj = "*ALL")
        {
            List<ILEObject[]> Items = new List<ILEObject[]>();
            string Library, Object, RefObj, RefLib, Type;

            Lib = Lib.ToUpper();
            Obj = Obj.ToUpper();

            if (IBMi.IsConnected())
            {
                if (Lib == "*CURLIB") Lib = IBMi.CurrentSystem.GetValue("curlib");
                Editor.TheEditor.SetStatus("Fetching references for " + Lib + "/" + Obj + "...");
                
                UsingQTEMPFiles(new[] { "REFS", "REFSB" });

                //ymurata1967 Start
                IBMi.RemoteCommand("RUNSQL SQL('CREATE TABLE QTEMP/REFSB (A GRAPHIC(198) CCSID 1200 NOT NULL)') COMMIT(*NONE)");
                IBMi.RemoteCommand("DSPPGMREF PGM(" + Lib + "/" + Obj + ") OUTPUT(*OUTFILE) OUTFILE(QTEMP/REFS)");
                IBMi.RemoteCommand("RUNSQL SQL('INSERT INTO QTEMP/REFSB (SELECT TRIM(WHLIB)||'',''||TRIM(WHPNAM)||'',''||TRIM(WHFNAM)||'',''||TRIM(WHLNAM)||'',''||TRIM(WHOTYP) FROM QTEMP/REFS)') COMMIT(*NONE)");
                IBMi.RemoteCommand("CPYTOIMPF FROMFILE(QTEMP/REFSB) TOSTMF('" + JpUtils.GetDwFileName() + "') MBROPT(*REPLACE) STMFCCSID(943) RCDDLM(*CRLF) DTAFMT(*FIXED) RMVBLANK(*TRAILING)");
                string file = DownloadMember("QTEMP", "REFSB", "REFSB", JpUtils.GetDwFileName());
                //ymurata1967 End

                if (file != "")
                {
                    //ymurata1967 ファイルの中身が無い場合はnullを返却
                    string[] lines = File.ReadAllLines(file, Program.Encoding);
                    if (lines.Length == 0)
                    {
                        return null;
                    }

                    foreach (string line in lines)
                    {
                        if (line.Trim() != "")
                        {
                            //ymurata1967 Start
                            string[] arr = line.Split(',');
                            Library = arr[0];
                            Object = arr[1];
                            RefObj = arr[2];
                            RefLib = arr[3];
                            Type = arr[4];
                            //ymurata1967 End

                            if (Library != "")
                            {
                                Items.Add(new[] { new ILEObject(Library, Object), new ILEObject(RefLib, RefObj, Type) });
                            }
                        }
                    }
                }
                else
                {
                    return null;
                }

                Editor.TheEditor.SetStatus("Fetched references for " + Lib + " / " + Obj + ".");
            }
            else
            {
                Editor.TheEditor.SetStatus("Cannot fetch references when offline.");
                return null;
            }

            return Items;
        }

        public static SpoolFile[] GetSpoolListing(string Lib, string Obj)
        {
            if (IBMi.IsConnected())
            {
                List<SpoolFile> Listing = new List<SpoolFile>();
                List<string> commands = new List<string>();

                string file = "";

                if (Lib != "" && Obj != "")
                {
                    Editor.TheEditor.SetStatus("Fetching spool file listing.. (can take a moment)");

                    UsingQTEMPFiles(new[] { "SPOOL", "SPOOLTMP" });

                    //ymurata1967 Start
                    IBMi.RemoteCommand("CRTPF FILE(QTEMP/SPOOL) RCDLEN(80) IGCDTA(*YES)");
                    IBMi.RemoteCommand("RUNSQL SQL('CREATE TABLE QTEMP/SPOOLTMP AS (SELECT CHAR(SPOOLED_FILE_NAME)||'',''||CHAR(COALESCE(USER_DATA, ''''))||'',''||CHAR(JOB_NAME)||'',''||CHAR(STATUS)||'',''||CHAR(FILE_NUMBER) AS A FROM TABLE(QSYS2.OUTPUT_QUEUE_ENTRIES(''" + Lib + "'', ''" + Obj + "'', ''*NO'')) A WHERE USER_NAME = ''" + IBMi.CurrentSystem.GetValue("username").ToUpper() + "'' ORDER BY CREATE_TIMESTAMP DESC FETCH FIRST 25 ROWS ONLY) WITH DATA') COMMIT(*NONE)");
                    IBMi.RemoteCommand("CPYF FROMFILE(QTEMP/SPOOLTMP) TOFILE(QTEMP/SPOOL) MBROPT(*ADD) FMTOPT(*NOCHK)");
                    IBMi.RemoteCommand("CPYTOSTMF FROMMBR('/QSYS.LIB/QTEMP.LIB/SPOOL.FILE/SPOOL.MBR') TOSTMF('" + JpUtils.GetDwFileName() + "') STMFOPT(*REPLACE) STMFCCSID(943)");
                    file = DownloadMember("QTEMP", "SPOOL", "SPOOL", JpUtils.GetDwFileName());
                    //ymurata1967 End
                    Editor.TheEditor.SetStatus("Finished fetching spool file listing.");
                }

                if (file != "")
                {
                    string SpoolName, UserData, Job, Status, Number;

                    //ymurata1967 ファイルの中身が無い場合はnullを返却
                    string[] lines = File.ReadAllLines(file, Program.Encoding);
                    if (lines.Length == 0)
                    {
                        return null;
                    }

                    foreach (string line in lines)
                    {
                        if (line.Trim() != "")
                        {
                            //ymurata1967 Start
                            string[] arr =line.Split(',');
                            SpoolName = arr[0].Substring(2, 10).Trim(); //なぜかCPYFで先頭に制御コードと＋記号は入るので2byte以降から切り出す
                            UserData = arr[1].Trim();
                            Job = arr[2].Trim();
                            Status = arr[3].Trim();
                            Number = arr[4].Trim();
                            //ymurata1967 End

                            if (SpoolName != "")
                            {
                                Listing.Add(new SpoolFile(SpoolName, UserData, Job, Status, int.Parse(Number)));
                            }
                        }
                    }

                    return (Listing.Count > 0 ? Listing.ToArray() : null);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static Boolean CompileSource(RemoteSource SourceInfo, string TrueCmd = "")
        {
            if (IBMi.IsConnected())
            {
                List<string> commands = new List<string>();
                string type, command, filetemp = GetLocalFile("QTEMP", "JOBLOG", "JOBLOG"), name, library = "";

                if (SourceInfo != null)
                {
                    type = SourceInfo.GetExtension().ToUpper();
                    command = IBMi.CurrentSystem.GetValue("DFT_" + type);
                    if (command.Trim() != "")
                    {
                        if (TrueCmd != "") command = TrueCmd;
                        Editor.TheEditor.SetStatus("Compiling " + SourceInfo.GetName() + " with " + command + "...");

                        if (SourceInfo.GetFS() == FileSystem.IFS)
                            command += "_IFS";

                        command = IBMi.CurrentSystem.GetValue(command);
                        if (command.Trim() != "")
                        {
                            name = SourceInfo.GetName();
                            if (name.Length > 10) name.Substring(0, 10);

                            switch (SourceInfo.GetFS())
                            {
                                case FileSystem.QSYS:
                                    command = command.Replace("&OPENLIB", SourceInfo.GetLibrary());
                                    command = command.Replace("&OPENSPF", SourceInfo.GetObject());
                                    command = command.Replace("&OPENMBR", SourceInfo.GetName());
                                    library = SourceInfo.GetLibrary();
                                    break;
                                case FileSystem.IFS:
                                    command = command.Replace("&FILENAME", name);
                                    command = command.Replace("&FILEPATH", SourceInfo.GetRemoteFile());
                                    command = command.Replace("&BUILDLIB", IBMi.CurrentSystem.GetValue("buildLib"));

                                    library = IBMi.CurrentSystem.GetValue("buildLib");
                                    IBMi.SetWorkingDir(IBMi.CurrentSystem.GetValue("homeDir"));
                                    break;
                            }

                            if (library != "")
                            {
                                command = command.Replace("&CURLIB", IBMi.CurrentSystem.GetValue("curlib"));
                                IBMi.RemoteCommand($"CHGLIBL LIBL({ IBMi.CurrentSystem.GetValue("datalibl").Replace(',', ' ')}) CURLIB({ IBMi.CurrentSystem.GetValue("curlib") })");

                                if (!IBMi.RemoteCommand(command))
                                {
                                    Editor.TheEditor.SetStatus("Compile finished unsuccessfully.");
                                    if (command.ToUpper().Contains("*EVENTF"))
                                    {
                                        Editor.TheEditor.SetStatus("Fetching errors..");
                                        Editor.TheEditor.Invoke((MethodInvoker)delegate
                                        {
                                            Editor.TheEditor.AddTool(new ErrorListing(library, name), WeifenLuo.WinFormsUI.Docking.DockState.DockLeft, true);
                                        });
                                        Editor.TheEditor.SetStatus("Fetched errors.");
                                    }
                                    if (IBMi.CurrentSystem.GetValue("fetchJobLog") == "true")
                                    {
                                        UsingQTEMPFiles(new[] { "JOBLOG" });
                                        IBMi.RemoteCommand("RUNSQL SQL('CREATE TABLE QTEMP/JOBLOG AS (SELECT char(MESSAGE_TEXT) as a FROM TABLE(QSYS2.JOBLOG_INFO(''*'')) A WHERE MESSAGE_TYPE = ''DIAGNOSTIC'') WITH DATA') COMMIT(*NONE)");
                                        IBMi.DownloadFile(filetemp, "/QSYS.lib/QTEMP.lib/JOBLOG.file/JOBLOG.mbr");
                                        //ymurata1967 Start（正しく動かないのでコメントのまま）
                                        //IBMi.RemoteCommand("CRTPF FILE(QTEMP/JOBLOG) RCDLEN(" + JpUtils.GetQtempRcdLen() + ") IGCDTA(*YES)");
                                        //IBMi.RemoteCommand("RUNSQL SQL('INSERT INTO QTEMP/JOBLOG (SELECT CHAR(MESSAGE_TEXT) FROM TABLE(QSYS2.JOBLOG_INFO(''*'')) A WHERE MESSAGE_TYPE = ''DIAGNOSTIC'')') COMMIT(*NONE)");
                                        //IBMi.RemoteCommand("CPYTOSTMF FROMMBR('/QSYS.LIB/QTEMP.LIB/JOBLOG.FILE/JOBLOG.MBR') TOSTMF('" + JpUtils.GetDwFileName() + "') STMFOPT(*REPLACE) STMFCCSID(943)");
                                        //IBMi.DownloadFile(filetemp, JpUtils.GetDwFileName());
                                        //ymurata1967 End
                                    }
                                }
                                else
                                {
                                    Editor.TheEditor.SetStatus("Compile finished successfully.");
                                }
                            }
                            else
                            {
                                Editor.TheEditor.SetStatus("Build library not set. See Connection Settings.");
                            }
                        }
                    }
                }
                else
                {
                    Editor.TheEditor.SetStatus("Only remote members can be compiled.");
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static string GetLocalDir(string Lib)
        {
            string LIBDir = Program.SOURCEDIR + "\\" + GetCurrentSystem() + "\\" + Lib;

            if (!Directory.Exists(LIBDir))
                Directory.CreateDirectory(LIBDir);

            return LIBDir;
        }

        public static string GetLocalDir(string Lib, string Obj)
        {
            string SPFDir = Program.SOURCEDIR + "\\" + GetCurrentSystem() + "\\" + Lib + "\\" + Obj;

            if (!Directory.Exists(SPFDir))
                Directory.CreateDirectory(SPFDir);

            return SPFDir;
        }

        public static string GetLocalSource(string Lib, string Spf, string Mbr)
        {
            string result = "";
            string[] libl;
            string dir;

            if (Lib == "*LIBL")
                libl = IBMi.CurrentSystem.GetValue("datalibl").Split(',');
            else
                libl = new[] { Lib };

            foreach (string lib in libl)
            {
                dir = Path.Combine(Program.SOURCEDIR, GetCurrentSystem(), lib);
                if (Directory.Exists(dir))
                {
                    dir = Path.Combine(dir, Spf);
                    if (Directory.Exists(dir))
                    {
                        foreach (string FilePath in Directory.GetFiles(dir))
                        {
                            if (Path.GetFileNameWithoutExtension(FilePath) == Mbr)
                            {
                                result = File.ReadAllText(FilePath).ToUpper();
                                return result;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static string GetLocalFile(string Lib, string Obj, string Mbr, string Ext = "")
        {
            Lib = Lib.ToUpper();
            Obj = Obj.ToUpper();
            Mbr = Mbr.ToUpper();
            if (Ext == "")
                Ext = "mbr";

            if (Lib == "*CURLIB") Lib = IBMi.CurrentSystem.GetValue("curlib");

            string SPFDir = Program.SOURCEDIR + "\\" + GetCurrentSystem() + "\\" + Lib + "\\" + Obj;

            if (!Directory.Exists(SPFDir))
                Directory.CreateDirectory(SPFDir);

            return SPFDir + "\\" + Mbr.ToUpper() + "." + Ext.ToLower();
        }

        public static string DownloadSpoolFile(string Name, int Number, string Job)
        {
            //CPYSPLF FILE(NAME) JOB(B/A/JOB) TOSTMF('STMF')

            if (IBMi.IsConnected())
            {
                string filetemp = GetLocalFile("SPOOLS", Job.Replace('/', '.'), Name + '-' + Number.ToString(), "SPOOL");
                //ymurata1967 Start
                string remoteTemp = JpUtils.GetTmpDir() + "/" + Name + ".SPOOL";
                Editor.TheEditor.SetStatus("Downloading spool file " + Name + ".."); 
                IBMi.RemoteCommand("CPYSPLF FILE(" + Name + ") JOB(" + Job + ") SPLNBR(" + Number.ToString() + ") TOFILE(*TOSTMF) TOSTMF('" + remoteTemp + "') STMFOPT(*REPLACE)");
                IBMi.RemoteCommand($"CPY OBJ('{remoteTemp}') TOOBJ('{JpUtils.GetDwFileName()}') FROMCCSID(*JOBCCSID) TOCCSID(943) DTAFMT(*TEXT) REPLACE(*YES)");
                //ymurata1967 End

                if (!IBMi.DownloadFile(filetemp, JpUtils.GetDwFileName()))  //ymurata1967
                {
                    Editor.TheEditor.SetStatus("Downloaded spool file " + Name + ".");
                    return filetemp;
                }
                else
                {
                    Editor.TheEditor.SetStatus("Failed downloading spool file " + Name + ".");
                    return "";
                }
            }
            else
            {
                return "";
            }
        }

        public static string DownloadMember(string Lib, string Obj, string Mbr, string DownFileName, string Ext = "")
        {
            if (Lib == "*CURLIB") Lib = IBMi.CurrentSystem.GetValue("curlib");
            string filetemp = GetLocalFile(Lib, Obj, Mbr, Ext);

            if (IBMi.IsConnected())
            {
                if (IBMi.DownloadFile(filetemp, DownFileName) == false) //ymurata1967
                    return filetemp;
                else
                    return "";
            }
            else
            {
                Editor.TheEditor.SetStatus("Fetching existing local member.");
                if (File.Exists(filetemp))
                    return filetemp;
                else
                    return "";
            }
        }

        public static string DownloadFile(string RemoteFile)
        {
            string filetemp = Path.Combine(GetLocalDir("IFS"), Path.GetFileName(RemoteFile));

            if (IBMi.IsConnected())
            {
                if (IBMi.DownloadFile(filetemp, JpUtils.GetDwFileNameIfs()) == false)   //ymurata1967
                    return filetemp;
                else
                    return "";
            }
            else
            {
                Editor.TheEditor.SetStatus("Fetching existing local file.");
                if (File.Exists(filetemp))
                    return filetemp;
                else
                    return "";
            }
        }

        public static bool UploadMember(string Local, string Lib, string Obj, string Mbr)
        {
            Lib = Lib.ToUpper();
            Obj = Obj.ToUpper();
            Mbr = Mbr.ToUpper();
            
            if (IBMi.IsConnected()) 
                return IBMi.UploadFile(Local, "/QSYS.lib/" + Lib + ".lib/" + Obj + ".file/" + Mbr + ".mbr");
            else
            {
                return true;
            }
        }

        public static bool UploadFile(string Local, string Remote)
        {
            if (IBMi.IsConnected())
                return IBMi.UploadFile(Local, Remote);
            else
            {
                Editor.TheEditor.SetStatus("Saving locally only.");
                return true;
            }
        }
    }
}
