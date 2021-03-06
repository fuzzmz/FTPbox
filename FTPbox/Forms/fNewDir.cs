﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using FtpLib;
using Tamir.SharpSsh.jsch;
using Starksoft.Net.Ftp;

namespace FTPbox
{
    public partial class fNewDir : Form
    {
        //variables used
        string host;
        string UN;
        string pass;
        int port;
        bool ftporsftp, ftps, ftpes;

        ChannelSftp sftpc;

        FtpClient ftpc;

        string sftproot = "/home";

        public fNewDir()
        {
            InitializeComponent();
        }

        private void bBrowse_Click(object sender, EventArgs e)
        {
            fbd.ShowDialog();
            tPath.Text = fbd.SelectedPath;
        }

        private void bDone_Click(object sender, EventArgs e)
        {
            if (!System.IO.Directory.Exists(tPath.Text))
                System.IO.Directory.CreateDirectory(tPath.Text);

            ((frmMain)this.Tag).UpdatePaths(tFullDir.Text, tPath.Text, tParent.Text);
            
            //FTPbox.Properties.Settings.Default.rPath = tFullDir.Text;
            //FTPbox.Properties.Settings.Default.lPath = tPath.Text;
            //FTPbox.Properties.Settings.Default.ftpParent = tParent.Text;
            //FTPbox.Properties.Settings.Default.Save();
            if (ftporsftp)
            {
                try
                {
                    ftpc.Close();
                }
                catch { }
            }
            else
            {
                try
                {
                    sftpc.quit();
                }
                catch { }
            }
            ((frmMain)this.Tag).ClearLog();
            ((frmMain)this.Tag).UpdateDetails();
            ((frmMain)this.Tag).SetLocalWatcher();
            ((frmMain)this.Tag).gotpaths = true;

            
            this.Close();
        }       

        private void fNewDir_Load(object sender, EventArgs e)
        {
            try
            {
                host = ((frmMain)this.Tag).ftpHost();
                UN = ((frmMain)this.Tag).ftpUser();
                pass = ((frmMain)this.Tag).ftpPass();
                port = ((frmMain)this.Tag).ftpPort();
                ftporsftp = ((frmMain)this.Tag).FTP();
                ftps = ((frmMain)this.Tag).FTPS();
                ftpes = ((frmMain)this.Tag).FTPES();
                ((frmMain)this.Tag).SetParent(host);

                if (ftporsftp)
                {
                    ftpc = new FtpClient(host, port);

                    if (ftps)
                    {
                        if (ftpes)
                            ftpc.SecurityProtocol = FtpSecurityProtocol.Tls1OrSsl3Explicit;
                        else
                            ftpc.SecurityProtocol = FtpSecurityProtocol.Tls1OrSsl3Implicit;
                        ftpc.ValidateServerCertificate += new EventHandler<ValidateServerCertificateEventArgs>(ftp_ValidateServerCertificate);
                    }

                    ftpc.Open(UN, pass);                    
                    Log.Write(l.Info, "Connected: " + ftpc.IsConnected.ToString());
                }
                else
                {
                    sftp_login();
                    sftproot = sftpc.pwd();
                }

                if (((frmMain)this.Tag).ftpParent() == "")
                    tParent.Text = ((frmMain)this.Tag).ftpHost();
                else
                    tParent.Text = ((frmMain)this.Tag).ftpParent();

                tPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\FTPbox";

                Log.Write(l.Debug, ((frmMain)this.Tag).ftpParent() + " " + ((frmMain)this.Tag).ftpHost());

                treeView1.Nodes.Clear();

                TreeNode first = new TreeNode();
                first.Text = "/";
                treeView1.Nodes.Add(first);

                if (ftporsftp)
                {
                    foreach (FtpItem dir in ftpc.GetDirList())
                    {
                        if (dir.Name != "." && dir.Name != ".." && dir.ItemType == FtpItemType.Directory)
                        {
                            TreeNode ParentNode = new TreeNode();
                            ParentNode.Text = dir.Name;
                            treeView1.Nodes.Add(ParentNode);

                            TreeNode ChildNode = new TreeNode();
                            ChildNode.Text = dir.Name;
                            ParentNode.Nodes.Add(ChildNode);
                        }
                    }
                }
                else
                {
                    foreach (ChannelSftp.LsEntry lse in sftpc.ls("."))
                    {
                        SftpATTRS attrs = lse.getAttrs();
                        if (lse.getFilename() != "." && lse.getFilename() != ".." && attrs.getPermissionsString().StartsWith("d"))
                        {
                            TreeNode ParentNode = new TreeNode();
                            ParentNode.Text = lse.getFilename();
                            treeView1.Nodes.Add(ParentNode);

                            TreeNode ChildNode = new TreeNode();
                            ChildNode.Text = lse.getFilename();
                            ParentNode.Nodes.Add(ChildNode);
                        }
                    }
                }
                treeView1.SelectedNode = first;
                Set_Language(((frmMain)this.Tag).lang());
            }
            catch { this.Close(); }

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string path = "/" + e.Node.FullPath.ToString().Replace('\\', '/');
            if (path.EndsWith(".."))
            {
                path = path.Substring(0, path.Length - 2);
            }
            else if (path.EndsWith("."))
            {
                path = path.Substring(0, path.Length - 1);
            }
            else if (path.EndsWith("//"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            tFullDir.Text = path;
            tParent.Text = ((frmMain)this.Tag).ftpParent() + path;
        }

        private void treeView1_AfterExpand(object sender, TreeViewEventArgs e)
        {
            string path = "/" + e.Node.FullPath.ToString().Replace('\\', '/');

            if (e.Node.Nodes.Count > 0)
            {
                int i = e.Node.Index;                

                foreach (TreeNode tn in e.Node.Nodes)
                {
                    try
                    {
                        treeView1.Nodes[i].Nodes.Remove(tn);                  
                    }
                    catch (Exception ex){
                        MessageBox.Show(ex.Message);
                    }
                }
            }

            if (ftporsftp)
            {
                foreach (FtpItem dir in ftpc.GetDirList(path))
                {
                    if (dir.Name != "." && dir.Name != ".." && dir.ItemType == FtpItemType.Directory)
                    {
                        TreeNode ParentNode = new TreeNode();
                        ParentNode.Text = dir.Name;
                        e.Node.Nodes.Add(ParentNode);

                        TreeNode ChildNode = new TreeNode();
                        ChildNode.Text = dir.Name;
                        ParentNode.Nodes.Add(ChildNode);
                    }
                }
            }
            else
            {
                expandSFTP(path, e);
            }
        }

        private void expandSFTP(string path, TreeViewEventArgs e)
        {
            try
            {
                SftpGoToRoot();
                string fpath = sftproot + path;
                Log.Write(l.Debug, fpath);
                sftpc.cd(fpath);
                foreach (ChannelSftp.LsEntry lse in sftpc.ls("."))
                {
                    SftpATTRS attrs = lse.getAttrs();
                    if (lse.getFilename() != "." && lse.getFilename() != ".." && attrs.getPermissionsString().StartsWith("d"))
                    {
                        TreeNode ParentNode = new TreeNode();
                        ParentNode.Text = lse.getFilename();
                        e.Node.Nodes.Add(ParentNode);

                        TreeNode ChildNode = new TreeNode();
                        ChildNode.Text = lse.getFilename();
                        ParentNode.Nodes.Add(ChildNode);
                    }
                }
            }
            catch (Exception ex)
            {
                sftpc.quit();
                sftp_login();
                expandSFTP(path, e);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (tFullDir.Text != "" && tPath.Text != "")
            {
                bDone.Enabled = true;
            }
            else
            {
                bDone.Enabled = false;
            }
        }        

        private void tParent_TextChanged(object sender, EventArgs e)
        {
            //((frmMain)this.Tag).SetParent(tParent.Text);
            //FTPbox.Properties.Settings.Default.ftpParent = tParent.Text;
            //FTPbox.Properties.Settings.Default.Save();
        }

        private void Set_Language(string lan)
        {
            this.Text = ((frmMain)this.Tag).languages.Get(lan + "/paths/add_dir", "Add a new directory");
            labSelect.Text = ((frmMain)this.Tag).languages.Get(lan + "/paths/select_dir", "Select directory") + ":";
            labFullPath.Text = ((frmMain)this.Tag).languages.Get(lan + "/paths/full_path", "Full path") + ":";
            labLocal.Text = ((frmMain)this.Tag).languages.Get(lan + "/paths/local_folder", "Local folder") + ":";
            labParent.Text = ((frmMain)this.Tag).languages.Get(lan + "/main_form/account_full_path", "Account's full path") + ":";
            bBrowse.Text = ((frmMain)this.Tag).languages.Get(lan + "/paths/browse", "Browse");
            bDone.Text = ((frmMain)this.Tag).languages.Get(lan + "/new_account/done", "Done");
        }

        private void sftp_login()
        {
            JSch jsch = new JSch();

            host = ((frmMain)this.Tag).ftpHost();
            UN = ((frmMain)this.Tag).ftpUser();
            port = ((frmMain)this.Tag).ftpPort();

            Session session = jsch.getSession(UN, host, 22);

            // username and password will be given via UserInfo interface.
            UserInfo ui = new MyUserInfo();

            session.setUserInfo(ui);

            session.setPort(port);

            session.connect();

            Channel channel = session.openChannel("sftp");
            channel.connect();
            sftpc = (ChannelSftp)channel;

        }

        public class MyUserInfo : UserInfo
        {
            FTPbox.Classes.Settings sets = new FTPbox.Classes.Settings();

            public String getPassword()
            {
                return Decrypt(sets.Get("Account/Password", ""),
                "removed",
                "removed",
                "SHA1", 2, "OFRna73m*aze01xY", 256);
            }
            public bool promptYesNo(String str)
            {
                DialogResult returnVal = MessageBox.Show(
                    str,
                    "SharpSSH",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                return (returnVal == DialogResult.Yes);
            }

            public String getPassphrase() { return null; }
            public bool promptPassphrase(String message) { return true; }
            public bool promptPassword(String message) { return true; }

            public void showMessage(String message)
            {
                MessageBox.Show(
                    message,
                    "SharpSSH",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Asterisk);
            }

            #region Decrypt Method
            public static string Decrypt(string CipherText, string Password,
              string Salt = "Kosher", string HashAlgorithm = "SHA1",
              int PasswordIterations = 2, string InitialVector = "OFRna73m*aze01xY",
              int KeySize = 256)
            {
                if (string.IsNullOrEmpty(CipherText))
                    return "";
                byte[] InitialVectorBytes = Encoding.ASCII.GetBytes(InitialVector);
                byte[] SaltValueBytes = Encoding.ASCII.GetBytes(Salt);
                byte[] CipherTextBytes = Convert.FromBase64String(CipherText);
                System.Security.Cryptography.PasswordDeriveBytes DerivedPassword = new System.Security.Cryptography.PasswordDeriveBytes(Password, SaltValueBytes, HashAlgorithm, PasswordIterations);
                byte[] KeyBytes = DerivedPassword.GetBytes(KeySize / 8);
                System.Security.Cryptography.RijndaelManaged SymmetricKey = new System.Security.Cryptography.RijndaelManaged();
                SymmetricKey.Mode = System.Security.Cryptography.CipherMode.CBC;
                byte[] PlainTextBytes = new byte[CipherTextBytes.Length];
                int ByteCount = 0;
                using (System.Security.Cryptography.ICryptoTransform Decryptor = SymmetricKey.CreateDecryptor(KeyBytes, InitialVectorBytes))
                {
                    using (System.IO.MemoryStream MemStream = new System.IO.MemoryStream(CipherTextBytes))
                    {
                        using (System.Security.Cryptography.CryptoStream CryptoStream = new System.Security.Cryptography.CryptoStream(MemStream, Decryptor, System.Security.Cryptography.CryptoStreamMode.Read))
                        {

                            ByteCount = CryptoStream.Read(PlainTextBytes, 0, PlainTextBytes.Length);
                            MemStream.Close();
                            CryptoStream.Close();
                        }
                    }
                }
                SymmetricKey.Clear();
                return Encoding.UTF8.GetString(PlainTextBytes, 0, ByteCount);
            }
            #endregion
        }

        public void SftpGoToRoot()
        {
            while (!sftpc.pwd().equals(sftproot))
            {
                try
                {
                    sftpc.cd("..");
                }
                catch (Exception ex) { 
                    Log.Write(l.Error, "errrrror: {0}", ex.Message);
                    sftpc.quit();
                    sftp_login();
                }
            }
        }

        private void treeView1_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            int i = e.Node.Nodes.Count;
            Log.Write(l.Debug, i.ToString());

            Log.Write(l.Debug, e.Node.FullPath);
            
            int ind = treeView1.Nodes.IndexOf(e.Node);            

            while (i != 0)
            {
                //treeView1.Nodes.RemoveAt(i);
                i -= 1;
                Log.Write(l.Debug, i.ToString());
            }            
        }

        private void ftp_ValidateServerCertificate(object sender, ValidateServerCertificateEventArgs e)
        {
            e.IsCertificateValid = true;
        }
    }
}
