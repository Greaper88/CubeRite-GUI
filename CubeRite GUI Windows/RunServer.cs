using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace CubeRite_GUI_Windows
{
    public partial class RunServer : Form
    {
        string programDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "cuberite");
        private Process cuberiteProcess;
        private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        StreamWriter sw;

        public RunServer()
        {
            InitializeComponent();
            this.FormClosing += FormClose;
            textBox2.KeyDown += textBox2_KeyDown;
            if (File.Exists(Path.Combine(programDirectory, "settings.ini"))) { button2.Enabled = true; }
            if (File.Exists(Path.Combine(programDirectory, "webadmin.ini"))) { button3.Enabled = true; }
        }
        private void FormClose(object sender, FormClosingEventArgs e)
        {
            this.Name = "Shutting down...";
            try { cuberiteProcess?.Kill(); } catch { };
            this?.Dispose();
        }
        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // Prevent the Enter key from producing a newline in textBox2
                e.SuppressKeyPress = true;

                // Process the command entered in textBox2
                string command = textBox2.Text.Trim();
                sw.WriteLine(command);
                sw.Flush();

                // Clear the textBox2 after processing the command
                textBox2.Clear();
            }
        }
        private async void StartCuberiteServer()
        {
            if (Directory.Exists(programDirectory))
            {
                if (!button2.Enabled) { button2.Enabled = true; }
                if (!button3.Enabled) { button3.Enabled = true; }
                button4.Text = "Restart Server";
                button5.Text = "Stop Server";
                button5.Enabled = true;
                button7.Enabled = true;
                button8.Enabled = true;
                button9.Enabled = true;
                button10.Enabled = true;
                cuberiteProcess = new Process();
                cuberiteProcess.StartInfo.WorkingDirectory = programDirectory;
                cuberiteProcess.StartInfo.FileName = Path.Combine(programDirectory, "cuberite.exe");
                cuberiteProcess.StartInfo.UseShellExecute = false;
                cuberiteProcess.StartInfo.RedirectStandardOutput = true;
                cuberiteProcess.StartInfo.RedirectStandardError = true; // Redirect standard error
                cuberiteProcess.StartInfo.RedirectStandardInput = true; // Enable input redirection
                cuberiteProcess.StartInfo.CreateNoWindow = true; // Hide the external program's window
                cuberiteProcess.OutputDataReceived += new DataReceivedEventHandler(OutputHandler); // Handle standard I/O (STOUT/STIN)
                cuberiteProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorHandler); // Handle standard error

                // Start the process asynchronously
                cuberiteProcess.Start();

                sw = cuberiteProcess.StandardInput;
                Task.Run(() => MonitorProcess());

                // Begin asynchronous reading of standard output and standard error
                cuberiteProcess.BeginOutputReadLine();
                cuberiteProcess.BeginErrorReadLine();

                // Set focus back to the TextBox after starting the process
                textBox1.Focus();

                // Wait asynchronously for the process to exit
                await Task.Run(() => cuberiteProcess.WaitForExit());
            }
            else
            {
                MessageBox.Show("Cuberite directory not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void OutputHandler(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                // Update the TextBox with the output from the process
                AppendText(textBox1, e.Data + Environment.NewLine);
            }
        }
        private void ErrorHandler(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                // Handle standard error output here
                // For example, append it to textBox1 or display in a MessageBox
                textBox1.AppendText(e.Data);
            }
        }
        private void AppendText(TextBox textBox, string text)
        {
            if (textBox.InvokeRequired)
            {
                textBox.Invoke(new Action<TextBox, string>(AppendText), textBox, text);
            }
            else
            {
                textBox.AppendText(text);
            }
        }
        private async Task MonitorProcess()
        {
            while (true)
            {
                // Check if the external process is active
                if (cuberiteProcess == null || cuberiteProcess.HasExited)
                {
                    // Clear the CPU and RAM text boxes when the process terminates
                    ClearCPURAMTextBoxes();
                    return; // Exit the monitoring loop
                }

                // Get CPU and RAM usage
                float cpuUsage = GetProcessCPUUsage(cuberiteProcess);
                float ramUsageMB = GetProcessRAMUsage(cuberiteProcess);

                // Update CPU and RAM text boxes on the main UI thread
                UpdateCPURAMTextBoxes($"{cpuUsage:F2}%", $"{ramUsageMB:F2} MB");

                await Task.Delay(1000); // Adjust delay as needed
            }
        }
        private float GetProcessCPUUsage(Process process)
        {
            if (process == null || process.HasExited)
            {
                return 0.0f;
            }

            using (PerformanceCounter cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName))
            {
                cpuCounter.NextValue(); // Initial call to initialize the counter
                System.Threading.Thread.Sleep(1000); // Wait for a moment for the next value
                return cpuCounter.NextValue();
            }
        }
        private float GetProcessRAMUsage(Process process)
        {
            if (process == null || process.HasExited)
            {
                return 0.0f;
            }

            return (float)(process.WorkingSet64 / (1024 * 1024)); // Convert bytes to megabytes
        }
        private void ClearCPURAMTextBoxes()
        {
            if (textBox4.InvokeRequired)
            {
                textBox4.Invoke((MethodInvoker)(() => textBox4.Text = ""));
            }
            else
            {
                textBox4.Text = "";
            }

            if (textBox6.InvokeRequired)
            {
                textBox6.Invoke((MethodInvoker)(() => textBox6.Text = ""));
            }
            else
            {
                textBox6.Text = "";
            }
        }

        private void UpdateCPURAMTextBoxes(string cputext, string ramtext)
        {
            if (textBox4.InvokeRequired || textBox6.InvokeRequired)
            {
                // Use Invoke to update UI controls on the main UI thread
                textBox4.Invoke((MethodInvoker)(() => textBox4.Text = cputext));
                textBox6.Invoke((MethodInvoker)(() => textBox6.Text = ramtext));
            }
            else
            {
                textBox4.Text = cputext;
                textBox6.Text = ramtext;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start("explorer", programDirectory);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("notepad", Path.Combine(programDirectory, "settings.ini"));
        }
        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start("notepad", Path.Combine(programDirectory, "webadmin.ini"));
        }
        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.Text == "Start Server") { StartCuberiteServer(); }
            else if (button4.Text == "Restart Server") { sw.WriteLine("restart"); sw.Flush(); };
        }
        private async void button5_Click(object sender, EventArgs e)
        {
            button7.Enabled = false;
            button8.Enabled = false;
            button9.Enabled = false;
            button10.Enabled = false;
            if (button5.Text == "Stop Server")
            {
                button4.Enabled = false;
                button5.Text = "Kill Server";
                sw.WriteLine("stop");
                sw.Flush();
                await Task.Delay(7000);
                if (cuberiteProcess == null || cuberiteProcess.HasExited) 
                { 
                    button4.Text = "Start Server";
                    button4.Enabled = true;
                    button5.Enabled = false;
                };
            }
            else if (button5.Text == "Kill Server")
            {
                try
                {
                    cuberiteProcess?.Kill();
                    await Task.Delay(500);
                    if (cuberiteProcess == null || cuberiteProcess.HasExited)
                    {
                        textBox1.AppendText("Server Killed!" + Environment.NewLine);
                        button4.Text = "Start Server";
                        button4.Enabled = true;
                        button5.Enabled = false;
                    };
                }
                catch { };
            }
        }
        private void button6_Click(object sender, EventArgs e)
        {
            Process.Start("explorer", Path.Combine(programDirectory, "Plugins"));
        }

        private void button7_Click(object sender, EventArgs e)
        {
            sw.WriteLine("reload");
            sw.Flush();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            sw.WriteLine("list");
            sw.Flush();
        }
        private void button9_Click(object sender, EventArgs e)
        {
            sw.WriteLine("plugins");
            sw.Flush();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            sw.WriteLine("tps");
            sw.Flush();
        }
    }
}
