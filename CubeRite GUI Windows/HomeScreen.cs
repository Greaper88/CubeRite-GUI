using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace CubeRite_GUI_Windows

/*
This is the class for the launcher screen. At present, the launcher screen will simply download and update an install of Cuberite.
The server will be installed into a "cuberite" folder inside of the user's home folder located at "C:\Users\{username}\"
It can also launch the server and even has an option to remove all of the server's files from the launcher.
*/
{
    public partial class HomeScreen : Form
    {
        string userHomeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); // Gets the user's home folder.
        public string programDirectory = string.Empty; // Initializing variable for later use.
        public HomeScreen()
        {
            programDirectory = Path.Combine(userHomeDirectory, "cuberite"); // Sets the program directory based on the user's home folder.
            InitializeComponent();
            if (Directory.Exists(programDirectory)) { button2.Enabled = true; }
            if (Directory.Exists(programDirectory)) { button3.Enabled = true; }
        }

        private async void button1_Click(object sender, EventArgs e) // Button action for installing and updating the software.
        {
            button1.Enabled = false;
            // These lines are to determine whether to download the 32 or 64 bit server version.
            string downloadUrl = string.Empty;
            if (Environment.Is64BitOperatingSystem) { downloadUrl = "https://download.cuberite.org/windows-x86_64/Cuberite.zip"; }
            else { downloadUrl = "https://download.cuberite.org/windows-i386/Cuberite.zip"; }
            string downloadPath = Path.Combine(userHomeDirectory, "Cuberite.zip");

            // Download the file.
            HttpResponseMessage response;
            try
            {
                using HttpClient httpClient = new HttpClient();
                {
                    response = await httpClient.GetAsync(downloadUrl);
                    response.EnsureSuccessStatusCode();
                }

                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                {
                    using (FileStream fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await contentStream.CopyToAsync(fileStream);
                    }
                }

                // Check if folder exists, creates it if new installation.
                if (!Directory.Exists(programDirectory))
                {
                    Directory.CreateDirectory(programDirectory);
                }

                // Extract the downloaded file to install the server. Will overwrite existing files.
                System.IO.Compression.ZipFile.ExtractToDirectory(downloadPath, programDirectory);
                MessageBox.Show("Download and extraction complete.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                button2.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            button1.Enabled = true;
            button3.Enabled = true;
        }

        // Loads the sub-form to execute the server.
        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            RunServer execute = new RunServer();
            this.Hide();
            execute.ShowDialog();
            this.Show();
            button2.Enabled = true;
        }

        // Button to remove all server data.
        private void button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;
            DialogResult result = MessageBox.Show("Are you sure you want to remove all server data? \nThis will also delete all player and world data!", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                button2.Enabled = false;
                try
                {
                    Directory.Delete(programDirectory, true);
                    MessageBox.Show("Server data removed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}