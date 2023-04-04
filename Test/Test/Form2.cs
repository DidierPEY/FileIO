using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace llt.Test
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        public void resPathFiles(string[] fichiers)
        {
            this.Text = "Liste des fichiers";
            if (dataGridView1.Columns.Count > 0) dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("fichier", "Fichier");
            foreach (string s in fichiers) dataGridView1.Rows.Add(s);
        }
        public void resPathFiles(System.Collections.Generic.Dictionary<string, DateTime> fichiers)
        {
            this.Text = "Liste des fichiers avec la date";
            if (dataGridView1.Columns.Count > 0) dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("fichier", "Fichier");
            dataGridView1.Columns.Add("dateheure", "Date");
            foreach (System.Collections.Generic.KeyValuePair<string, DateTime> s in fichiers) dataGridView1.Rows.Add(new object[] { s.Key, s.Value });
        }


        private void Form2_SizeChanged(object sender, EventArgs e)
        {
            dataGridView1.Height = this.ClientSize.Height - dataGridView1.Left * 2;
            dataGridView1.Width = this.ClientSize.Width - dataGridView1.Top * 2;
        }
    }
}
