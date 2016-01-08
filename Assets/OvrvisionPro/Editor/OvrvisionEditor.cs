using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

/// <summary>
/// Ovrvision Custom Editor
/// </summary>
[CustomEditor( typeof(Ovrvision) )]
public class OvrvisionEditor : Editor {

	private const int OV_SET_AUTOMODE = (-1);

	public override void OnInspectorGUI() {
		Ovrvision obj = target as Ovrvision;

		string[] cammode = { "2560x1920@15", "1920x1080@30", "1280x960@45", "960x950@60:Default", "1280x800@60", "640x480@90", "320x240@120" };
		EditorGUILayout.LabelField( "Ovrvision Status" );
		if (obj.CameraStatus())
		{
			string text = "Opened : ";
			text += cammode[obj.cameraMode];
			EditorGUILayout.HelpBox(text, MessageType.Info, true);
		}
		else
		{
			obj.cameraMode = EditorGUILayout.Popup("Camera Mode", obj.cameraMode, cammode);
			EditorGUILayout.HelpBox("Closed", MessageType.Error, true);
		}

		EditorGUILayout.Space();

		obj.overlaySettings = EditorGUILayout.Toggle("Overlay Ovrvision settings", obj.overlaySettings);
		if (obj.overlaySettings)
		{
			obj.conf_exposure = EditorGUILayout.IntSlider("Exposure", obj.conf_exposure, 0, 32767);
			obj.conf_gain = EditorGUILayout.IntSlider("Gain", obj.conf_gain, 1, 47);
			obj.conf_blc = EditorGUILayout.IntSlider("BLC", obj.conf_blc, 0, 1023);

			obj.conf_wb_auto = EditorGUILayout.Toggle("WhiteBalance Auto", obj.conf_wb_auto);
			if (!obj.conf_wb_auto) {
				obj.conf_wb_r = EditorGUILayout.IntSlider("WhiteBalance R", obj.conf_wb_r, 0, 4095);
				obj.conf_wb_g = EditorGUILayout.IntSlider("WhiteBalance G", obj.conf_wb_g, 0, 4095);
				obj.conf_wb_b = EditorGUILayout.IntSlider("WhiteBalance B", obj.conf_wb_b, 0, 4095);
			}
			if (GUILayout.Button("Default config", GUILayout.Width(200)))
			{
				obj.conf_exposure = 12960;
				obj.conf_gain = 8;
				obj.conf_blc = 32;
				obj.conf_wb_r = 1474;
				obj.conf_wb_g = 1024;
				obj.conf_wb_b = 1738;
				obj.conf_wb_auto = true;
			}
		}

		EditorGUILayout.Space();

		//Ovrvision AR
		obj.useOvrvisionAR = EditorGUILayout.Toggle("Use the OvrvisionAR", obj.useOvrvisionAR);
		obj.ARsize = EditorGUILayout.FloatField("AR Marker size(meter)", obj.ARsize);

		EditorGUILayout.Space();

		obj.useOvrvisionTrack = EditorGUILayout.Toggle("Use the OvrvisionTrack", obj.useOvrvisionTrack);

		EditorGUILayout.Space();

		string[] planeshader = { "Normal Shader", "Brettel Dichromat Shader" };
		obj.camViewShader = EditorGUILayout.Popup("Camera Overlay", obj.camViewShader, planeshader);

		//changed param
		if (GUI.changed) {
			obj.UpdateOvrvisionSetting();
		}
		EditorUtility.SetDirty( target );	//editor set
	}

	/* not use
	[PostProcessBuild]
	public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
	{
		// Copy of "arcl" files
		string sourceFile = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "arcl");
		string targetFile = Path.Combine(Path.GetDirectoryName(pathToBuiltProject), "arcl");
		CopyDirectory(sourceFile, targetFile);
	}

	public static void CopyDirectory(string stSourcePath, string stDestPath)
	{
		if (!System.IO.Directory.Exists(stDestPath))
		{
			System.IO.Directory.CreateDirectory(stDestPath);
			System.IO.File.SetAttributes(stDestPath, System.IO.File.GetAttributes(stSourcePath));
		}
		foreach (string stCopyFrom in System.IO.Directory.GetFiles(stSourcePath))
		{
			string stCopyTo = System.IO.Path.Combine(stDestPath, System.IO.Path.GetFileName(stCopyFrom));
			System.IO.File.Copy(stCopyFrom, stCopyTo, true);
		}
		foreach (string stCopyFrom in System.IO.Directory.GetDirectories(stSourcePath))
		{
			string stCopyTo = System.IO.Path.Combine(stDestPath, System.IO.Path.GetFileName(stCopyFrom));
			CopyDirectory(stCopyFrom, stCopyTo);
		}
	}
	*/
}