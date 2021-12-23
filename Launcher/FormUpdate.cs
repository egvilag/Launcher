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
        public string programPath;

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

        public void RemoveOldFiles()
        {
            try
            {
                string[] files = Directory.GetFiles(Path.GetDirectoryName(programPath), "*_update.*");
                string originalFile;
                foreach (string file in files)
                {
                    if ((file.Contains("_update")) && (file.Replace("_update", "") != programPath))
                    {
                        originalFile = file.Replace("_update", "");
                        if ((File.Exists(originalFile)))
                            File.Delete(originalFile);
                        //;
                        File.Move(file, originalFile);
                    }
                }
                if (File.Exists(programPath.Replace(".exe", "_old.exe")))
                    File.Delete(programPath.Replace(".exe", "_old.exe"));
                string[] directories = Directory.GetDirectories(Path.GetDirectoryName(programPath));
                string[] oldFiles;
                foreach (string dirName in directories)
                {
                    //Get the files from the directories and replace them
                    oldFiles = Directory.GetFiles(dirName);
                    foreach (string oldFilename in oldFiles)
                    {
                        if (oldFilename.Contains("_update"))
                        {
                            if (File.Exists(oldFilename.Replace("_update", "")))
                                File.Delete(oldFilename.Replace("_update", ""));
                                //;
                            File.Move(oldFilename, oldFilename.Replace("_update", ""));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.OpenForms["Form1"].Close();
            }
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
            StreamReader todoFile;
            StreamWriter editFile = null;
            try
            {
                //First unzip it to a directory
                if (Directory.Exists(Path.GetTempPath() + "/LauncherUpdate"))
                    Directory.Delete(Path.GetTempPath() + "/LauncherUpdate", true);
                ZipFile.ExtractToDirectory(Path.GetTempPath() + "/" + Path.GetFileName(link), Path.GetTempPath() + "/LauncherUpdate");
                ApplyUpdateWorker.ReportProgress(50);

                //Then start to make a list of the contents
                string updatePath = Path.GetTempPath() + "/LauncherUpdate";
                string[] files = Directory.GetFiles(updatePath, "*.*");

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
                    File.Copy(updateFilename, Path.GetDirectoryName(programPath) + "/" + Path.GetFileNameWithoutExtension(updateFilename) 
                        + "_update" + Path.GetExtension(updateFilename), true);
                }

                //Now look inside the subdirectories too
                string[] directories = Directory.GetDirectories(updatePath);
                foreach (string dirName in directories)
                {
                    //If directory doesn't exist in target path, create it
                    if (!Directory.Exists(Path.GetDirectoryName(programPath) + "/" +  Path.GetFileName(dirName)))
                        Directory.CreateDirectory(Path.GetDirectoryName(programPath) + "/" + Path.GetFileName(dirName));
                    
                    //Get the files from the directories and copy them
                    files = Directory.GetFiles(dirName);
                    foreach (string updateFilename in files)
                    {
                        File.Copy(updateFilename, Path.GetDirectoryName(programPath) + "/"  + Path.GetFileName(dirName) + "/" 
                            + Path.GetFileNameWithoutExtension(updateFilename) + "_update" + Path.GetExtension(updateFilename), true);
                    }
                }
                ApplyUpdateWorker.ReportProgress(75);

                //Make the file modifications instructed in the todo list
                if (File.Exists(updatePath + "/" + todoFilename))
                {
                    todoFile = new StreamReader(updatePath + "/" + todoFilename, Encoding.UTF8);
                    Dictionary<string, List<string>> todoDict = new Dictionary<string, List<string>>(); //[Filename, list of rows to edit]
                    string line;
                    string filename = "";
                    
                    //Get all the needed modifications fromt the list
                    while ((line = todoFile.ReadLine()) != null)
                    {
                        if (!line.StartsWith("#"))  //Line is not commented out
                        {
                            if ((line.StartsWith("[")) && (line.Contains("]"))) //Filename specification in [] tag. Eg: [config.txt] #sth
                            {
                                filename = line.Substring(line.IndexOf("[") + 1, line.IndexOf("]") - 2);
                                todoDict.Add(filename, new List<string>()); ;
                            }
                            else
                                if ((line.Length > 0) && (todoDict.ContainsKey(filename)))
                                todoDict[filename].Add(line);
                        }
                    }
                    todoFile.Close();
                    todoFile.Dispose();
                    
                    //Do the dirty job
                    foreach (KeyValuePair<string, List<string>> kvp in todoDict)
                    {
                        editFile = new StreamWriter(Path.GetDirectoryName(programPath) + "/" + kvp.Key, true, Encoding.UTF8);
                        foreach (string s in kvp.Value)
                        {
                            switch (s[0])
                            {
                                case '+': //Add a line
                                    editFile.WriteLine(s.Substring(1, s.Length - 1));
                                    break;
                                case '*': //Modify a line
                                    
                                    //Not implemented yet
                                    break;
                            }
                        }
                        editFile.Flush();
                        editFile.Close();
                    }
                }
                ApplyUpdateWorker.ReportProgress(85);

                //Make the registry modifications instructed in the update reg file
                if (File.Exists(updatePath + "/" + regFilename))
                {
                    //Not implemented yet
                }
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
            else
            {
                if (File.Exists(programPath)) 
                    File.Move(programPath, programPath.Replace(".exe", "_old.exe"));
                if (File.Exists(programPath.Replace(".exe", "_update.exe")))
                    File.Move(programPath.Replace(".exe", "_update.exe"), programPath);
                using (System.Diagnostics.Process pProcess = new System.Diagnostics.Process())
                {
                    //string binary = Path.GetDirectoryName(programPath) + "/" + Path.GetFileNameWithoutExtension(programPath)
                    //    + "_update" + Path.GetExtension(programPath);
                    string binary = programPath;
                    if (File.Exists(binary))
                    {
                        pProcess.StartInfo.FileName = binary;
                        //pProcess.StartInfo.Arguments = ""; //argument
                        pProcess.StartInfo.UseShellExecute = false;
                        //pProcess.StartInfo.RedirectStandardOutput = true;
                        //pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        //pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
                        pProcess.Start();
                        //string output = pProcess.StandardOutput.ReadToEnd(); //The output result
                        //pProcess.WaitForExit();
                        Application.OpenForms["Form1"].Invoke((MethodInvoker)delegate { Application.OpenForms["Form1"].Close(); });
                    }
                    else
                        this.Invoke((MethodInvoker)delegate { MessageBox.Show("Nem találom a frissített exe fájlt!", "", MessageBoxButtons.OK, MessageBoxIcon.Error); });
                }
            }
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

        //Ask the server if any updates are available (in JSON format)
        public void SearchForUpdateJson(Form1 sender, bool betatesting)
        {
            UpdateJSON updateJSON;
            string hash = GetHash();
            if (!hash.StartsWith("Error"))
                updateJSON = new UpdateJSON(betatesting, hash);
            else
            {
                MessageBox.Show(hash);
                return;
            }
            sender.Send(updateJSON.GetResult());
        }

        //Compute the hash of the current Launcher executable
        public string GetHash()
        {
            string result = "";
            MD5 md5 = MD5.Create();
            try
            {
                FileStream stream = File.OpenRead(programPath);
                byte[] hashBytes = md5.ComputeHash(stream);
                //result = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                result = ByteArrayToString(hashBytes);
                stream.Close();
            }
            catch (Exception ex)
            {
                result = "Error: " + ex.Message;
            }
            return result;
        }

        //Convert byte array to hex
        public string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }


        //Convert hex string to byte array
        public byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}
