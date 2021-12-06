using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;

namespace Launcher
{
    public partial class FormUpdate : Form
    {
        public FormUpdate()
        {
            InitializeComponent();
        }

        private void FormUpdate_Load(object sender, EventArgs e)
        {

        }

        public void SearchForUpdate(Form1 sender, bool betatesting)
        {
            string command = "update&betatesting=";
            if (betatesting) command += "1"; else command += "0";
            command += "&hash=";
            string hash = GetHash();
            if (!hash.StartsWith("Error")) command += hash;
            else
            {
                MessageBox.Show(hash);
                return;
            }
            sender.Send(command);
        }

        public string GetHash()
        {
            string result = "";
            MD5 md5 = MD5.Create();
            try
            {
                FileStream stream = File.OpenRead(System.Reflection.Assembly.GetEntryAssembly().Location);
                byte[] hashbytes = md5.ComputeHash(stream);
                result = BitConverter.ToString(hashbytes).Replace("-", "").ToLowerInvariant();
                stream.Close();
            }
            catch (Exception ex)
            {
                result = "Error: " + ex.Message;
            }
            return result;
        }

        public void DoUpdate()
        {
            this.ShowDialog();
        }
    }
}
