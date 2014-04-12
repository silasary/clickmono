using ClickMac;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace ClickOnce
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Loading.Log = Log;
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] args = Environment.GetCommandLineArgs();
            if (PreLoading.DoArgs(ref args))
                this.Close();


        }

        private void Log(string s, params object[] args)
        {
            
        }
    }
}
