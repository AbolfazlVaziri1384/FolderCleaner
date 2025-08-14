using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ClearTool
{
    public partial class MainForm : Form
    {
        public class IniFile
        {
            public string Path;

            [DllImport("kernel32", CharSet = CharSet.Unicode)]
            private static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

            [DllImport("kernel32", CharSet = CharSet.Unicode)]
            private static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

            public IniFile(string IniPath)
            {
                Path = IniPath;
            }

            public void Write(string Key, string Value, string Section = "Folders")
            {
                WritePrivateProfileString(Section, Key, Value, Path);
            }

            public string Read(string Key, string Section = "Folders")
            {
                StringBuilder RetVal = new StringBuilder(255);
                GetPrivateProfileString(Section, Key, "", RetVal, 255, Path);
                return RetVal.ToString();
            }
        }

        IniFile ini;
        string iniPath;

        public MainForm()
        {
            InitializeComponent();
            iniPath = Path.Combine(Application.StartupPath, "folders.ini");
            ini = new IniFile(iniPath);

            dgvAddress.AllowUserToAddRows = false; // only add by button
            dgvAddress.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvAddress.MultiSelect = false;

            LoadSavedFolders();
        }

        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtFolder.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnSaveAddress_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFolderName.Text))
            {
                MessageBox.Show("Enter folder name.");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtFolder.Text))
            {
                MessageBox.Show("Enter folder path.");
                return;
            }

            // If this address was already saved, update it
            foreach (DataGridViewRow row in dgvAddress.Rows)
            {
                if (row.Cells["Address"].Value?.ToString() == txtFolder.Text)
                {
                    row.Cells["Name"].Value = txtFolderName.Text;
                    SaveFoldersToIni();
                    return;
                }
            }

            // If capacity is full
            if (dgvAddress.Rows.Count >= 10)
            {
                MessageBox.Show("The folder list is full. Please delete one.");
                return;
            }

            dgvAddress.Rows.Add(txtFolderName.Text, txtFolder.Text);
            SaveFoldersToIni();
            txtFolderName.Text = string.Empty;
        }

        private void btnDeleteAddress_Click(object sender, EventArgs e)
        {
            if (dgvAddress.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a row.");
                return;
            }
            string msg = "Are you sure you want to delete the selected folder?";
            if (MessageBox.Show(msg, "Confirm delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                dgvAddress.Rows.RemoveAt(dgvAddress.SelectedRows[0].Index);
                SaveFoldersToIni();
            }
        }

        private void LoadSavedFolders()
        {
            dgvAddress.Rows.Clear();

            for (int i = 1; i <= 10; i++)
            {
                string name = ini.Read($"Name{i}");
                string address = ini.Read($"Address{i}");
                if (!string.IsNullOrEmpty(address))
                {
                    dgvAddress.Rows.Add(name, address);
                }
            }
        }

        private void SaveFoldersToIni()
        {
            // First clear all
            for (int i = 1; i <= 10; i++)
            {
                ini.Write($"Name{i}", "");
                ini.Write($"Address{i}", "");
            }

            // Then save again from dgv
            for (int i = 0; i < dgvAddress.Rows.Count; i++)
            {
                var row = dgvAddress.Rows[i];
                ini.Write($"Name{i + 1}", row.Cells["Name"].Value?.ToString());
                ini.Write($"Address{i + 1}", row.Cells["Address"].Value?.ToString());
            }
        }

        private void dgvAddress_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                //  txtFolderName.Text = dgvAddress.Rows[e.RowIndex].Cells["Name"].Value?.ToString();
                txtFolder.Text = dgvAddress.Rows[e.RowIndex].Cells["Address"].Value?.ToString();
            }
        }

        private void btnClean_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFolder.Text) || !Directory.Exists(txtFolder.Text))
            {
                MessageBox.Show("Invalid folder path.");
                return;
            }

            if (!chkBin.Checked && !chkObj.Checked && !chkVs.Checked)
            {
                MessageBox.Show("No option selected.");
                return;
            }

            string msg = "Are you sure you want to delete the selected folders?";
            if (MessageBox.Show(msg, "Confirm delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (chkBin.Checked) DeleteFoldersRecursive(txtFolder.Text, "bin");
                if (chkObj.Checked) DeleteFoldersRecursive(txtFolder.Text, "obj");
                if (chkVs.Checked) DeleteFoldersRecursive(txtFolder.Text, ".vs");

                MessageBox.Show("Deletion completed successfully.");
            }
        }

        private void DeleteFoldersRecursive(string rootPath, string folderName)
        {
            foreach (var dir in Directory.GetDirectories(rootPath, folderName, SearchOption.AllDirectories))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch { }
            }
        }
    }
}
