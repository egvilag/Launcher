using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;


namespace Launcher
{
    static class CommandProcessor
    {
        public static List<string> allowedParameters = new List<string>()
        {
            "status", "userid", "username", "channelid", "channelname", "groupid", "groupname", "messageid",
            "link", "sign", "msg"
        };

        public static void Process(Form1 f, Socket socket, string command)
        {
            //At least one parameter with valid format
            if ((command.Split('&').Length > 1) && (command.Split('=').Length > 1))
            {
                string commandName = command.Split('&')[0];
                string parameterString = command.Substring(command.IndexOf('&') + 1, command.Length - commandName.Length - 1);
                //string[] parametersArray = new string[2];
                Dictionary<string, string> parameters = new Dictionary<string, string>();

                //Get the first parameter
                string key = parameterString.Split('=')[0];
                if (allowedParameters.Contains(key))
                {
                    parameters.Add(key, parameterString.Split('&')[0].Split('=')[1]);
                }
                else return;

                //Get the second parameter
                string secondParameterString = parameterString.Substring(parameterString.IndexOf('&') + 1, parameterString.Length - parameterString.Split('&')[0].Length - 1);
                key = secondParameterString.Split('=')[0];
                if (allowedParameters.Contains(key))
                {
                    parameters.Add(key, secondParameterString.Substring(secondParameterString.IndexOf('=') + 1, secondParameterString.Length - key.Length - 1));
                }
                else return;

                //Decide what to do
                DialogResult dialogResult;
                switch (commandName)
                {
                    case "update":
                        switch (parameters["status"])
                        {
                            case "0":
                                return;
                            case "1":
                                dialogResult = MessageBox.Show("Új verzió elérhető! Letöltöd most a frissítést?", "Új verzió", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                if (dialogResult == DialogResult.Yes)
                                {
                                    f.formUpdate.DoUpdate(parameters["link"]);
                                }
                                break;
                            case "2":
                                dialogResult = MessageBox.Show("Hibás verziót futtatsz! Letöltöm a javítást.", "Hibás verzió", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                                if (dialogResult == DialogResult.OK)
                                    f.Invoke((MethodInvoker)delegate { f.formUpdate.DoUpdate(parameters["link"]); }); //It's important to use invoke since it's another thread. Note that it's 
                                break;                                                                                //used on the parent form (Form1 f)!
                        }
                        break;
                    case "sendgmessage":
                        f.textBox2.Invoke((MethodInvoker)delegate { f.textBox2.Text += parameters["msg"] + "\r\n"; });
                        break;
                    







                    default:
                        return;
                }
            }
            else return;


        }
    }
}
