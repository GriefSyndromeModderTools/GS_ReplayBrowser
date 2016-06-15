using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GS_ReplayBrowser
{
    public partial class ReplayInfoForm : Form
    {
        internal ReplayInfoForm(ReplayInfo info)
        {
            InitializeComponent();

            textBox1.Text = info.FileName;
            textBox2.Text = "" + info.FileSize_KB + " KB";
            textBox3.Text = info.Time.ToString(@"hh\:mm\:ss\.ff");
            textBox4.Text = info.Messages.ToString();
            textBox5.Text = info.Players.ToString();
            textBox6.Text = info.Lap.ToString();
            textBox7.Text = info.Actors;

            this.Text = "Replay - " + info.FileName;
        }

        private void ReplayInfoForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
    }
}
