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
using System.IO.Compression;
using System.Net;

namespace Launcher
{
    public partial class FormUpdate : Form
    {
        const string todoFilename = "todo.lst";
        const string regFilename = "update.reg";

        //Link to update zip
        private string link;

        //A client for downloading the update
        private WebClient webClient;

        //A worker thread to apply the update
        private BackgroundWorker ApplyUpdateWorker;

        public FormUpdate()
        {
            InitializeComponent();
        }

        public void DoUpdate(string updateLink)
        {
            link = updateLink;
            this.Show();
        }

        private void FormUpdate_Shown(object sender, EventArgs e)
        {
            label1.Text = "Frissítés letöltése";
            webClient = new WebClient();
            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadCompleted);
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
            progressBar1.Visible = true;
            webClient.DownloadFileAsync(new Uri(link), Path.GetTempPath() + "/" + Path.GetFileName(link));
        }

        //Show the download state in the progressbar
        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar1.Invoke((MethodInvoker)delegate { progressBar1.Value = e.ProgressPercentage; });
        }

        //When the download completes, proceed with unzipping the archive and process the update list
        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            label1.Text = "Frissítés telepítése";
            progressBar1.Value = 0;
            ApplyUpdateWorker = new BackgroundWorker();
            ApplyUpdateWorker.DoWork += new DoWorkEventHandler(ApplyUpdateWork);
            ApplyUpdateWorker.ProgressChanged += new ProgressChangedEventHandler(ApplyUpdateProgressChanged);
            ApplyUpdateWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ApplyUpdateCompleted);
            ApplyUpdateWorker.WorkerReportsProgress = true;
            ApplyUpdateWorker.WorkerSupportsCancellation = true;
            ApplyUpdateWorker.RunWorkerAsync();
        }
        //The thread for processing the contents of the artchive
        private void ApplyUpdateWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                //First unzip it to a directory
                if (Directory.Exists(Path.GetTempPath() + "/LauncherUpdate"))
                    Directory.Delete(Path.GetTempPath() + "/LauncherUpdate", true);
                ZipFile.ExtractToDirectory(Path.GetTempPath() + "/" + Path.GetFileName(link), Path.GetTempPath() + "/LauncherUpdate");
                ApplyUpdateWorker.ReportProgress(50);

                //Then start to make a list of the contents
                string[] files = Directory.GetFiles(Path.GetTempPath() + "/LauncherUpdate", "*.*");

                //Remove the two update helper files, since we won't copy them to anywhere
                int indexToRemove = Array.IndexOf(files, todoFilename);
                List<string> tempList = new List<string>(files);
                if (indexToRemove > -1) tempList.RemoveAt(indexToRemove);
                files = tempList.ToArray();
                indexToRemove = Array.IndexOf(files, regFilename);
                tempList = new List<string>(files);
                if (indexToRemove > -1) tempList.RemoveAt(indexToRemove);
                files = tempList.ToArray();

                //Copy the files from the root and add an '_update' suffix to the filenames
                foreach (string updateFilename in files)
                {
                    File.Copy(updateFilename, Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "/" + Path.GetFileNameWithoutExtension(updateFilename) 
                        + "_update" + Path.GetExtension(updateFilename));
                }

                //Now look inside the subdirectories too
                string[] directories = Directory.GetDirectories(Path.GetTempPath() + "/LauncherUpdate");
                foreach (string dirName in directories)
                {
                    //If directory doesn't exist in target path, create it
                    if (!Directory.Exists(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "/" +  Path.GetFileName(dirName)))
                        Directory.CreateDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "/" + Path.GetFileName(dirName));
                    
                    //Get the files from the directories and copy them
                    files = Directory.GetFiles(dirName);
                    foreach (string updateFilename in files)
                    {
                        File.Copy(updateFilename, Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "/"  + Path.GetFileName(dirName) + "/" 
                            + Path.GetFileNameWithoutExtension(updateFilename) + "_update" + Path.GetExtension(updateFilename));
                    }
                }
                ApplyUpdateWorker.ReportProgress(75);

                //Make the file modifications instructed in the todo list

                //Make the registry modifications instructed in the update reg file
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate { MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error); });
                ApplyUpdateWorker.CancelAsync();
            }
            ApplyUpdateWorker.ReportProgress(100);
        }

        //Show the apply job state in the progressbar
        private void ApplyUpdateProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Invoke((MethodInvoker)delegate { progressBar1.Value = e.ProgressPercentage; });
        }

        //Updating completed, restart the program
        private void ApplyUpdateCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                this.Invoke((MethodInvoker)delegate { MessageBox.Show(e.Error.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error); });
            this.Invoke((MethodInvoker)delegate { this.Hide(); });
        }

        //Ask the server if any updates are available
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

        //Compute the hash of the current Launcher executable
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

       
    }
}
