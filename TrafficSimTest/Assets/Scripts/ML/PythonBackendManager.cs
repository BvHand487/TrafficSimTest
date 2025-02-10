using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class PythonBackendManager : SingletonMonobehaviour<PythonBackendManager>
{
    [SerializeField] private string pythonVersion = "3.10.11";
    [SerializeField] private string venvPath;
    [SerializeField] private string configPath;
    [SerializeField] private string resultsPath;
    [SerializeField] private int communicationPort = 5005;  // mlagents default port


    private string pythonExecutable;
    private string mlagentsExecutable;
    private string device;

    private Process mlagentsProcess;


    public override void Awake()
    {
        base.Awake();

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            pythonExecutable = Path.Combine(venvPath, "Scripts", "python.exe");
        else
            pythonExecutable = Path.Combine(venvPath, "bin", "python");

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            mlagentsExecutable = Path.Combine(venvPath, "Scripts", "mlagents-learn.exe");
        else
            mlagentsExecutable = Path.Combine(venvPath, "bin", "mlagents-learn");
    }

    public void Start()
    {
        VerifyPythonVersion();
        VerifyPackageInstallation();
        Initialize();
    }

    private string RunMLAgentsProcess(string args)
    {
        ProcessStartInfo processStart = new ProcessStartInfo
        {
            FileName = mlagentsExecutable,
            Arguments = args,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(processStart))
            using (StreamReader reader = process.StandardOutput)
                return reader.ReadToEnd();
    }

    private string RunPythonProcess(string args)
    {
        ProcessStartInfo processStart = new ProcessStartInfo
        {
            FileName = pythonExecutable,
            Arguments = args,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(processStart))
            using (StreamReader reader = process.StandardOutput)
                return reader.ReadToEnd();
    }

    // Required python version >=3.10.1, <= 3.10.12
    public void VerifyPythonVersion()
    {
        string output = RunPythonProcess("--version");

        if (output.Contains(pythonVersion))
        {
            UnityEngine.Debug.Log($"{this.GetType().Name}: Succefully located python installation: {output.Replace(Environment.NewLine, string.Empty)}");
        }
        else
        {
            UnityEngine.Debug.LogError($"{this.GetType().Name}: Verify that python's version is {pythonExecutable} (>=3.10.1, <= 3.10.12).");
        }
    }

    // Required packages = mlagents
    public void VerifyPackageInstallation()
    {
        VerifyMLAgents();
    }

    private bool VerifyMLAgents()
    {
        string script = "";
        string output = "";

        script = $"import importlib.util; " +
                        $"print('Installed' if importlib.util.find_spec('mlagents') else 'Not Installed')";

        output = RunPythonProcess($"-c \"{script}\"");

        if (output.Contains("Installed"))
        {
            UnityEngine.Debug.Log($"{this.GetType().Name}: Succefully located ML-Agents installation.");
        }
        else
        {
            UnityEngine.Debug.LogError($"{this.GetType().Name}: Couldn't find ML-Agents installation");
        }

        output = RunMLAgentsProcess("--help");

        if (output.StartsWith("usage"))
        {
            UnityEngine.Debug.Log($"{this.GetType().Name}: Succefully verified ML-Agents installation.");
            return true;
        }
        else
        {
            UnityEngine.Debug.LogError($"{this.GetType().Name}: Couldn't verify ML-Agents installation");
            return false;
        }
    }

    // get available device
    private string GetAvailableDevice()
    {
        string script =
            $"import torch\n" +
            $"print('cuda' if torch.cuda.is_available() else 'cpu')";

        string output = RunPythonProcess($"-c \"{script}\"");

        return output.Trim();
    }

    public void Initialize()
    {
        device = GetAvailableDevice();
        UnityEngine.Debug.Log($"{this.GetType().Name}: Available device found: \'{device}\'.");

        // ...
    }

    public string StartMLAgents(string runId = null)
    {
        if (string.IsNullOrEmpty(runId))
            runId = Guid.NewGuid().ToString();

        if (mlagentsProcess != null && !mlagentsProcess.HasExited)
        {
            UnityEngine.Debug.LogError($"{this.GetType().Name}: Attempted to start training when ML-Agents is already running.");
            return null;
        }

        ProcessStartInfo mlagentsStart = new ProcessStartInfo
        {
            FileName = mlagentsExecutable,
            Arguments = $"\"{configPath}\" --run-id={runId} --results-dir={resultsPath} --base-port={communicationPort} --torch-device={device}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        mlagentsProcess = new Process { StartInfo = mlagentsStart };

        mlagentsProcess.OutputDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data))
                UnityEngine.Debug.Log($"{this.GetType().Name}: {args.Data}");
        };

        mlagentsProcess.ErrorDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data))
                UnityEngine.Debug.LogError($"{this.GetType().Name}: {args.Data}");
        };

        mlagentsProcess.Start();
        mlagentsProcess.BeginOutputReadLine();
        mlagentsProcess.BeginErrorReadLine();

        UnityEngine.Debug.Log($"{this.GetType().Name}: ML-Agents training started.");

        return runId;
    }

    public void StopMLAgents()
    {
        if (IsMLAgentsRunning())
        {
            mlagentsProcess.Kill();
            UnityEngine.Debug.Log($"{this.GetType().Name}: ML-Agents training stopped.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"{this.GetType().Name}: Attempted to stop training when ML-Agents isn't running.");
        }
    }

    public bool IsMLAgentsRunning()
    {
        return mlagentsProcess != null && !mlagentsProcess.HasExited;
    }
}
