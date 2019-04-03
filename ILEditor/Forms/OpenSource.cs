﻿using ILEditor.Classes;
using ILEditor.UserTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ILEditor.Forms
{
    public partial class OpenSource : Form
    {
        public static string Library = "", SPF = "", Stmf = "";

        public OpenSource(int tab = 0)
        {
            InitializeComponent();
            type.Items.AddRange(Editor.LangTypes.Keys.ToArray());

            tabs.SelectedIndex = tab;

            lib.Text = Library;
            spf.Text = SPF;
            stmfPath.Text = Stmf;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Boolean isValid = true;

            switch (tabs.SelectedIndex)
            {
                case 0:
                    if (!IBMiUtils.IsValueObjectName(lib.Text))
                        isValid = false;
                    if (!IBMiUtils.IsValueObjectName(spf.Text))
                        isValid = false;
                    if (!IBMiUtils.IsValueObjectName(mbr.Text))
                        isValid = false;

                    if (isValid)
                    {
                        Editor.OpenSource(new RemoteSource("", lib.Text, spf.Text, mbr.Text, type.Text, true));
                        Library = lib.Text;
                        SPF = spf.Text;
                        this.Close();
                    }
                    else
                        MessageBox.Show("Member information not valid.");
                    break;

                case 1:
                    //ymurata1967 なぜかファイルが存在してもIBMi.FileExists(stmfPath.Text)がfalseを返すので、判定箇所をコメントアウトし、
                    //とりあえず開くようにした。ただし存在しないファイルを指定すると以前のデータをダウンロードされるので注意！！
                    stmfPath.Text = stmfPath.Text.Trim();
                    Editor.OpenSource(new RemoteSource("", stmfPath.Text));
                    Stmf = stmfPath.Text;
                    this.Close();
                    /*
                    if (IBMi.FileExists(stmfPath.Text)) {
                        Editor.OpenSource(new RemoteSource("", stmfPath.Text));
                        Stmf = stmfPath.Text;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Chosen file does not exist.");
                    }
                    */
                    break;
            }
            
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            open.Text = "Open " + tabs.SelectedTab.Text;
        }
    }
}
