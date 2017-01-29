using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;
using System.Net;
using System.Threading;
using UnityEditor.Utils;

namespace AssetBundles
{
	internal class LaunchAssetBundleServer
	{
		const string kServerPID = "Temp/server.pid";
	
		static void KillRunningAssetBundleServer ()
		{
			// Kill the last time we ran
			try
			{
				int processID;
				string pidFile = File.ReadAllText(kServerPID);
				processID = int.Parse(pidFile);
				Process lastProcess = Process.GetProcessById (processID);
				lastProcess.Kill();
			}
			catch
			{
			}
		}
		
		[MenuItem ("AssetBundles/Launch AssetBundleServer")]
		static void Run ()
		{
			string pathToAssetServer = Path.Combine(Application.dataPath, "AssetBundleManager/Editor/AssetBundleServer.exe");
			string pathToApp = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
	
			KillRunningAssetBundleServer();
			
			BuildScript.WriteServerURL();
			
			string args = Path.Combine (pathToApp, "AssetBundles");
			args = string.Format("\"{0}\" {1}", args, Process.GetCurrentProcess().Id);
			ProcessStartInfo startInfo = ExecuteInternalMono.GetProfileStartInfoForMono(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), "4.0", pathToAssetServer, args, true);
			startInfo.WorkingDirectory = Path.Combine(System.Environment.CurrentDirectory, "AssetBundles");
			startInfo.UseShellExecute = false;
			Process launchProcess = Process.Start(startInfo);
			if (launchProcess == null || launchProcess.HasExited == true)
			{
				//Unable to start process
				UnityEngine.Debug.Log ("Unable Start AssetBundleServer process");
			}
			else
			{
				//We seem to have launched, let's save the PID
				File.WriteAllText(kServerPID, launchProcess.Id.ToString());
			}
		}
	}
}