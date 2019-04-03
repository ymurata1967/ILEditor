using ILEditor.Classes;
using ILEditor.UserTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ILEditor.Forms
{
    public partial class SourceCompareSelect : Form
    {
        public SourceCompareSelect()
        {
            InitializeComponent();

            if (Editor.LastEditing != null)
            {
                RemoteSource src = Editor.LastEditing.Tag as RemoteSource;

                if (src != null)
                {
                    switch (src.GetFS())
                    {
                        case FileSystem.QSYS:
                            newSourceBox.SetSource(src.GetLibrary(), src.GetObject(), src.GetName());
                            oldSourceBox.SetSource("", src.GetObject(), src.GetName());
                            break;
                        case FileSystem.IFS:
                            newSourceBox.SetSource(src.GetRemoteFile());
                            newSourceBox.SetTab(src.GetFS());
                            break;
                    }
                }
            }
        }

        private void compareButton_Click(object sender, EventArgs e)
        {
            if (!newSourceBox.isValid())
            {
                MessageBox.Show("New source information not valid.");
                return;
            }

            if (!oldSourceBox.isValid())
            {
                MessageBox.Show("Old source information not valid.");
                return;
            }

            string NewFile = "", OldFile = "";

            switch (newSourceBox.GetFS())
            {
                case FileSystem.IFS:
                    //ymurata1967 Start Shift-jisに変換する。
                    IBMi.RemoteCommand($"DEL OBJLNK('{JpUtils.GetDwFileNameIfs()}')");
                    IBMi.RemoteCommand($"CPY OBJ('{newSourceBox.GetIFSPath()}') TOOBJ('{JpUtils.GetDwFileNameIfs()}') FROMCCSID(*OBJ) TOCCSID(932) DTAFMT(*TEXT) REPLACE(*YES)");
                    NewFile = IBMiUtils.DownloadFile(newSourceBox.GetIFSPath());
                    //ymurata1967 End
                    break;
                case Classes.FileSystem.QSYS:
                    //ymurata1967 Start ソースを一度IFSにコピーしてからShift-jisに変換する。
                    IBMi.RemoteCommand($"DEL OBJLNK('{JpUtils.GetDwTmpFileNameMbr()}')");
                    IBMi.RemoteCommand($"DEL OBJLNK('{JpUtils.GetDwFileNameMbr()}')");
                    IBMi.RemoteCommand($"CPY OBJ('/QSYS.LIB/{newSourceBox.GetLibrary()}.LIB/{newSourceBox.GetSPF()}.FILE/{newSourceBox.GetMember()}.MBR') TOOBJ('{JpUtils.GetDwTmpFileNameMbr()}') FROMCCSID(*OBJ) TOCCSID(*JOBCCSID) DTAFMT(*TEXT) REPLACE(*YES)");
                    IBMi.RemoteCommand($"CPY OBJ('{JpUtils.GetDwTmpFileNameMbr()}') TOOBJ('{JpUtils.GetDwFileNameMbr()}') FROMCCSID(*JOBCCSID) TOCCSID(943) DTAFMT(*TEXT) REPLACE(*YES)");
                    NewFile = IBMiUtils.DownloadMember(newSourceBox.GetLibrary(), newSourceBox.GetSPF(), newSourceBox.GetMember(), JpUtils.GetDwFileNameMbr());
                    //ymurata1967 End
                    break;
            }

            switch (oldSourceBox.GetFS())
            {
                case FileSystem.IFS:
                    //ymurata1967 Start Shift-jisに変換する。
                    IBMi.RemoteCommand($"DEL OBJLNK('{JpUtils.GetDwFileNameIfs()}')");
                    IBMi.RemoteCommand($"CPY OBJ('{oldSourceBox.GetIFSPath()}') TOOBJ('{JpUtils.GetDwFileNameIfs()}') FROMCCSID(*OBJ) TOCCSID(932) DTAFMT(*TEXT) REPLACE(*YES)");
                    OldFile = IBMiUtils.DownloadFile(oldSourceBox.GetIFSPath());
                    //ymurata1967 End
                    break;
                case Classes.FileSystem.QSYS:
                    //ymurata1967 Start ソースを一度IFSにコピーしてからShift-jisに変換する。
                    IBMi.RemoteCommand($"DEL OBJLNK('{JpUtils.GetDwTmpFileNameMbr()}')");
                    IBMi.RemoteCommand($"DEL OBJLNK('{JpUtils.GetDwFileNameMbr()}')");
                    IBMi.RemoteCommand($"CPY OBJ('/QSYS.LIB/{oldSourceBox.GetLibrary()}.LIB/{oldSourceBox.GetSPF()}.FILE/{oldSourceBox.GetMember()}.MBR') TOOBJ('{JpUtils.GetDwTmpFileNameMbr()}') FROMCCSID(*OBJ) TOCCSID(*JOBCCSID) DTAFMT(*TEXT) REPLACE(*YES)");
                    IBMi.RemoteCommand($"CPY OBJ('{JpUtils.GetDwTmpFileNameMbr()}') TOOBJ('{JpUtils.GetDwFileNameMbr()}') FROMCCSID(*JOBCCSID) TOCCSID(943) DTAFMT(*TEXT) REPLACE(*YES)");
                    OldFile = IBMiUtils.DownloadMember(oldSourceBox.GetLibrary(), oldSourceBox.GetSPF(), oldSourceBox.GetMember(), JpUtils.GetDwFileNameMbr());
                    //ymurata1967 End
                    break;
            }

            if (NewFile == "" || OldFile == "")
            {
                MessageBox.Show("Unable to download members.");
                return;
            }

            Editor.TheEditor.AddTool(new DiffView(NewFile, OldFile));
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
