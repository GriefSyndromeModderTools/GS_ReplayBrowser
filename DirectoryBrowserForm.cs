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

namespace GS_ReplayBrowser
{
    public partial class DirectoryBrowserForm : Form
    {
        public DirectoryBrowserForm()
        {
            InitializeComponent();
        }

        private string[] _Files;
        private Dictionary<string, ReplayInfo> _Result;

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var dir = folderBrowserDialog1.SelectedPath;

                _Files = Directory.EnumerateFiles(dir, "*.rep", SearchOption.TopDirectoryOnly).ToArray();

                textBox1.Text = dir;

                progressBar1.Value = 0;
                progressBar1.Visible = true;
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var number = _Files.Length;
            _Result = new Dictionary<string, ReplayInfo>();
            var finished = 0;
            var toReport = 10;
            foreach (var f in _Files)
            {
                ++finished;
                try
                {
                    _Result.Add(f, new ReplayInfo(f));
                }
                catch
                {
                }
                if (--toReport <= 0)
                {
                    toReport = 10;

                    backgroundWorker1.ReportProgress((int)(100.0f * finished / number));
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Visible = false;
            listView1.Items.Clear();
            foreach (var entry in _Result)
            {
                var item = new ListViewItem(new string[] { Path.GetFileName(entry.Key), entry.Value.Description }, -1);
                item.Tag = entry.Value;
                listView1.Items.Add(item);
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }
            var sel = (ReplayInfo)listView1.SelectedItems[0].Tag;
            if (sel != null)
            {
                var dialog = new ReplayInfoForm(sel);
                dialog.ShowDialog();
            }
        }
    }
}
