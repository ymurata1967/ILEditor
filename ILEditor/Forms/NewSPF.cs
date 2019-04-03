﻿using ILEditor.Classes;
using ILEditor.UserTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ILEditor.Forms
{
    public partial class NewSPF : Form
    {
        public NewSPF()
        {
            InitializeComponent();
        }

        private void create_Click(object sender, EventArgs e)
        {
            bool isValid = true;

            lib.Text = lib.Text.Trim();
            spf.Text = spf.Text.Trim();

            if (!IBMiUtils.IsValueObjectName(lib.Text))
                isValid = false;
            if (!IBMiUtils.IsValueObjectName(spf.Text))
                isValid = false;

            if (isValid)
            {
                if (IBMi.IsConnected())
                {
                    string cmd = "CRTSRCPF FILE(" + lib.Text + "/" + spf.Text + ") RCDLEN(" + rcdLen.Value.ToString() + ") CCSID(" + ccsid.Text + ") IGCDTA(*YES)"; //ymurata1967 IGCDTA(*YES)追加
                    if (IBMi.RemoteCommand(cmd))
                    {
                        Editor.TheEditor.AddTool(new MemberBrowse(lib.Text, spf.Text), WeifenLuo.WinFormsUI.Docking.DockState.DockRight);
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show(lib.Text.Trim() + "/" + spf.Text.Trim() + " not created.");
                    }
                }
                else
                {
                    Directory.CreateDirectory(IBMiUtils.GetLocalDir(lib.Text, spf.Text));
                    Editor.TheEditor.AddTool(new MemberBrowse(lib.Text, spf.Text), WeifenLuo.WinFormsUI.Docking.DockState.DockRight);
                }
            }
            else
                MessageBox.Show("SPF information not valid.");

        }
    }
}
