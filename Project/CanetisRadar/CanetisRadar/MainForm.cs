using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CanetisRadar
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Overlay o = new Overlay();
            o.Show();
            this.Hide();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //toolStripStatusLabel1.Text = "Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //TODO: actial settings window
            Process.Start("C:\\oPH64RlpL.ini");
        }
    }
}
