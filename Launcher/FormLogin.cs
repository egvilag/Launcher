using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Agreement.Srp;

namespace Launcher
{
    public partial class FormLogin : Form
    {
        public FormLogin()
        {
            InitializeComponent();
        }

        private void FormLogin_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Application.OpenForms["Form1"].Close();
        }

        private void FormLogin_Shown(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        private void valami()
        {
            byte[] I = Encoding.UTF8.GetBytes(textBox1.Text);           //Username
            byte[] P = Encoding.UTF8.GetBytes(maskedTextBox1.Text);     //Password
            //byte[] s = new byte[64];                                    //Salt
            byte[] colon = BitConverter.GetBytes(':');                  //Colon
            //Random rnd = new Random();
            //rnd.NextBytes(s);

            // (I | ":" | P)
            byte[] I_P = new byte[I.Length + colon.Length + P.Length];
            System.Buffer.BlockCopy(I, 0, I_P, 0, I.Length);
            System.Buffer.BlockCopy(colon, 0, I_P, I.Length, colon.Length);
            System.Buffer.BlockCopy(P, 0, I_P, I.Length + colon.Length, P.Length);

            // H(I| ":" | P)
            SHA256 sha = SHA256.Create();
            byte[] xInternal = sha.ComputeHash(I_P);
            
            // (s | H(I | ":" | P))
            byte[] xSInternal = new byte[CommandProcessor.s.Length + xInternal.Length];
            System.Buffer.BlockCopy(CommandProcessor.s, 0, xSInternal, 0, CommandProcessor.s.Length);
            System.Buffer.BlockCopy(xInternal, 0, xSInternal, CommandProcessor.s.Length, xSInternal.Length);

            // x = H(s | H(I | ":" | P))
            byte[] x = sha.ComputeHash(xSInternal);

            Srp6Client client = new Srp6Client();

        }

        private byte[] HexToBytes(string hex)
        {
            byte[] hexAsBytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
                hexAsBytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return hexAsBytes;
        }

        public BigInteger BytesToBigInt(byte[] bytes)
        {
            return new BigInteger(bytes);
        }

        public BigInteger HexToBigInt(string hex)
        {
            return BigInteger.Parse("0" + hex, NumberStyles.HexNumber);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
