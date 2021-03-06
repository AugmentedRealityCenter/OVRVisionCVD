﻿using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

/// <summary>
/// This class provides main interface to the Ovrvision
/// </summary>
public class Ovrvision : MonoBehaviour
{
	//Ovrvision Pro class
	private COvrvisionUnity OvrPro = new COvrvisionUnity();

	//Camera GameObject
	private GameObject CameraLeft;
	private GameObject CameraRight;
	private GameObject CameraPlaneLeft;
	private GameObject CameraPlaneRight;
    private TextMesh PixelColorText;
    private GameObject Snapshot;

    //Camera texture
    private Texture2D CameraTexLeft = null;
	private Texture2D CameraTexRight = null;
	private Vector3 CameraRightGap;
    private Color CenterPixelColor;

	//public propaty
	public int cameraMode = COvrvisionUnity.OV_CAMVR_FULL;
	public bool useOvrvisionAR = false;
	public float ARsize = 0.15f;
	public bool useOvrvisionTrack = false;

	public bool overlaySettings = false;
	public int conf_exposure = 12960;
	public int conf_gain = 8;
	public int conf_blc = 32;
	public int conf_wb_r = 1474;
	public int conf_wb_g = 1024;
	public int conf_wb_b = 1738;
	public bool conf_wb_auto = true;

	public int camViewShader = 0;

	public Vector2 chroma_hue = new Vector2(0.9f,0.2f);
	public Vector2 chroma_saturation = new Vector2(1.0f, 0.0f);
	public Vector2 chroma_brightness = new Vector2(1.0f, 0.0f);
	public Vector2 chroma_y = new Vector2(1.0f, 0.0f);
	public Vector2 chroma_cb = new Vector2(1.0f, 0.0f);
	public Vector2 chroma_cr = new Vector2(0.725f, 0.615f);

	//Ar Macro define
	private const int MARKERGET_MAXNUM10 = 100; //max marker is 10
	private const int MARKERGET_ARG10 = 10;
	private const int MARKERGET_RECONFIGURE_NUM = 10;

	private const float IMAGE_ZOFFSET = 0.02f;

	// ------ Function ------

	// Use this for initialization
	void Awake() {
		//Open camera
		if (OvrPro.Open(cameraMode, ARsize))
		{
			if (overlaySettings)
			{
				OvrPro.SetExposure(conf_exposure);
				OvrPro.SetGain(conf_gain);
				OvrPro.SetBLC(conf_blc);
				OvrPro.SetWhiteBalanceAutoMode(conf_wb_auto);
				if (!conf_wb_auto)
				{
					OvrPro.SetWhiteBalanceR(conf_wb_r);
					OvrPro.SetWhiteBalanceG(conf_wb_g);
					OvrPro.SetWhiteBalanceB(conf_wb_b);
				}
				Thread.Sleep(100);
			}
		} else {
			Debug.LogError ("Ovrvision open error!!");
		}
	}

	// Use this for initialization
	void Start()
	{
		if (!OvrPro.camStatus)
			return;

		// Initialize camera plane object(Left)
		CameraLeft = this.transform.FindChild("LeftCamera").gameObject;
		CameraRight = this.transform.FindChild("RightCamera").gameObject;
		CameraPlaneLeft = CameraLeft.transform.FindChild("LeftImagePlane").gameObject;
		CameraPlaneRight = CameraRight.transform.FindChild("RightImagePlane").gameObject;

        Snapshot = CameraPlaneLeft.transform.FindChild("Snapshot").gameObject;
        PixelColorText = (TextMesh)Snapshot.transform.FindChild("PixelColorText").gameObject.GetComponent(typeof(TextMesh));

        CameraLeft.transform.localPosition = Vector3.zero;
		CameraRight.transform.localPosition = Vector3.zero;
		CameraLeft.transform.localRotation = Quaternion.identity;
		CameraRight.transform.localRotation = Quaternion.identity;

		//Create cam texture
		CameraTexLeft = new Texture2D(OvrPro.imageSizeW, OvrPro.imageSizeH, TextureFormat.BGRA32, false);
		CameraTexRight = new Texture2D(OvrPro.imageSizeW, OvrPro.imageSizeH, TextureFormat.BGRA32, false);
		//Cam setting
		CameraTexLeft.wrapMode = TextureWrapMode.Clamp;
		CameraTexRight.wrapMode = TextureWrapMode.Clamp;

		//Mesh
		Mesh m = CreateCameraPlaneMesh();
		CameraPlaneLeft.GetComponent<MeshFilter>().mesh = m;
		CameraPlaneRight.GetComponent<MeshFilter>().mesh = m;

		//SetShader
		SetShader(camViewShader);

		CameraPlaneLeft.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", CameraTexLeft);
		CameraPlaneRight.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", CameraTexRight);
		CameraPlaneLeft.GetComponent<Renderer>().materials[1].SetTexture("_MainTex", CameraTexLeft);
		CameraPlaneRight.GetComponent<Renderer>().materials[1].SetTexture("_MainTex", CameraTexRight);

		CameraRightGap = OvrPro.HMDCameraRightGap();

		//Plane reset
		CameraPlaneLeft.transform.localScale = new Vector3(OvrPro.aspectW, -1.0f, 1.0f);
		CameraPlaneRight.transform.localScale = new Vector3(OvrPro.aspectW, -1.0f, 1.0f);
		CameraPlaneLeft.transform.localPosition = new Vector3(-0.032f, 0.0f, OvrPro.GetFloatPoint() + IMAGE_ZOFFSET);
		CameraPlaneRight.transform.localPosition = new Vector3(CameraRightGap.x - 0.040f, 0.0f, OvrPro.GetFloatPoint() + IMAGE_ZOFFSET);

		UnityEngine.VR.InputTracking.Recenter();

		if (useOvrvisionTrack)
		{
			OvrPro.useOvrvisionTrack_Calib = true;
			CameraPlaneRight.active = !OvrPro.useOvrvisionTrack_Calib;
		}
	}

	private Mesh CreateCameraPlaneMesh()
	{
		Mesh m = new Mesh();
		m.name = "CameraImagePlane";
		Vector3[] vertices = new Vector3[]
		{
			new Vector3(-0.5f, -0.5f, 0.0f),
			new Vector3( 0.5f,  0.5f, 0.0f),
			new Vector3( 0.5f, -0.5f, 0.0f),
			new Vector3(-0.5f,  0.5f, 0.0f)
		};
		int[] triangles = new int[]
		{
			0, 1, 2,
			1, 0, 3
		};
		Vector2[] uv = new Vector2[]
		{
			new Vector2(0.0f, 0.0f),
			new Vector2(1.0f, 1.0f),
			new Vector2(1.0f, 0.0f),
			new Vector2(0.0f, 1.0f)
		};
		m.vertices = vertices;
		m.subMeshCount = 2;
		m.SetTriangles(triangles, 0);
		m.SetTriangles(triangles, 1);
		m.uv = uv;
		m.RecalculateNormals();

		return m;
	}

    private bool ssDirty = false;

	// Update is called once per frame
	void Update ()
	{
		//camStatus
		if (!OvrPro.camStatus)
			return;

		//Testing
		if (Input.GetKeyDown(KeyCode.Space))
		{
			OvrPro.OvrvisionTrackReset();
		}

		if (Input.GetKeyDown(KeyCode.G))
		{
			useOvrvisionTrack ^= true;
			if (useOvrvisionTrack)
			{
				OvrPro.useOvrvisionTrack_Calib = true;
				CameraPlaneRight.active = !OvrPro.useOvrvisionTrack_Calib;
			}
		}
		if (useOvrvisionTrack)
		{
			if (Input.GetKeyDown(KeyCode.H))
			{
				OvrPro.useOvrvisionTrack_Calib ^= true;
				CameraPlaneRight.active = !OvrPro.useOvrvisionTrack_Calib;
			}
		}

		//get image data
		OvrPro.useOvrvisionAR = useOvrvisionAR;
		OvrPro.useOvrvisionTrack = useOvrvisionTrack;

		OvrPro.UpdateImage(CameraTexLeft.GetNativeTexturePtr(), CameraTexRight.GetNativeTexturePtr());

        if (Input.GetKey("c"))
        {
            Application.CaptureScreenshot("Assets/Resources/UnityScreenshot.png");
            ssDirty = true;
        }

        if (ssDirty && File.Exists("Assets/Resources/UnityScreenshot.png"))
        {
            byte[] file = File.ReadAllBytes("Assets/Resources/UnityScreenshot.png");
            Texture2D texture = new Texture2D(4, 4);
            texture.LoadImage(file);

            Snapshot.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", texture);
            ssDirty = false;

            int count = 0;
            int windowSize = 4;
            int r = 0;
            int g = 0;
            int b = 0;
            for(int y = texture.height/2 - windowSize/2; y < texture.height/2 + windowSize/2; y++)
            {
                for(int x = texture.width / 2 - windowSize / 2; x < texture.width / 2 + windowSize / 2; x++)
                {
                    Color c = texture.GetPixel(x, y);
                    r += (int)(256 * c.r);
                    g += (int)(256 * c.g);
                    b += (int)(256 * c.b);
                    count += 1;
                }
            }
            r /= count;
            g /= count;
            b /= count;
            PixelColorText.text = "(" + r + ", " + g + ", " + b + ")";
        }

		if (useOvrvisionAR) OvrvisionARRender();
		if (useOvrvisionTrack) OvrvisionTrackRender();
	}

	//Ovrvision AR Render to OversitionTracker Objects.
	private int OvrvisionARRender()
	{
		float[] markerGet = new float[MARKERGET_MAXNUM10];
		GCHandle marker = GCHandle.Alloc(markerGet, GCHandleType.Pinned);

		//Get marker data
		int ri = OvrPro.OvrvisionGetAR(marker.AddrOfPinnedObject(), MARKERGET_MAXNUM10);

		OvrvisionTracker[] otobjs = GameObject.FindObjectsOfType(typeof(OvrvisionTracker)) as OvrvisionTracker[];
		foreach (OvrvisionTracker otobj in otobjs)
		{
			otobj.UpdateTransformNone();
			for (int i = 0; i < ri; i++)
			{
				if (otobj.markerID == (int)markerGet[i * MARKERGET_ARG10])
				{
					otobj.UpdateTransform(markerGet, i);
					break;
				}
			}
		}

		marker.Free();

		return ri;
	}

	//Ovrvision Tracking Render
	private int OvrvisionTrackRender()
	{
		float[] markerGet = new float[3];
		GCHandle marker = GCHandle.Alloc(markerGet, GCHandleType.Pinned);
		//Get marker data
		int ri = OvrPro.OvrvisionGetTrackingVec3(marker.AddrOfPinnedObject());
		if (ri == 0)
			return 0;

		Vector3 fgpos = new Vector3(markerGet[0], markerGet[1], markerGet[2]);

		OvrvisionHandTracker[] otobjs = GameObject.FindObjectsOfType(typeof(OvrvisionHandTracker)) as OvrvisionHandTracker[];
		foreach (OvrvisionHandTracker otobj in otobjs)
		{
			otobj.UpdateTransformNone();

			if (fgpos.z <= 0.0f)
				continue;

			otobj.UpdateTransform(fgpos);
		}

		marker.Free();

		return ri;
	}

	// Quit
	void OnDestroy()
	{
		//Close camera
		if(!OvrPro.Close())
			Debug.LogError ("Ovrvision close error!!");
	}

	//proparty
	public bool CameraStatus()
	{
		return OvrPro.camStatus;
	}

	public void UpdateOvrvisionSetting()
	{
		if (!OvrPro.camStatus)
			return;

		//set config
		if (overlaySettings)
		{
			OvrPro.SetExposure(conf_exposure);
			OvrPro.SetGain(conf_gain);
			OvrPro.SetBLC(conf_blc);
			OvrPro.SetWhiteBalanceR(conf_wb_r);
			OvrPro.SetWhiteBalanceG(conf_wb_g);
			OvrPro.SetWhiteBalanceB(conf_wb_b);
			OvrPro.SetWhiteBalanceAutoMode(conf_wb_auto);
		}

		//SetShader
		SetShader(camViewShader);
	}

	private void SetShader(int viewShader)
	{
        if (viewShader == 0)
        {
            //Normal Shader
            CameraPlaneLeft.GetComponent<Renderer>().material.shader = Shader.Find("Custom/cvdNoChange");
            CameraPlaneRight.GetComponent<Renderer>().material.shader = Shader.Find("Custom/cvdNoChange");
        }
        else if (viewShader == 1)
        {
            //Brettel Dichromat Shader
            CameraPlaneLeft.GetComponent<Renderer>().material.shader = Shader.Find("Custom/cvdBrettel");
            CameraPlaneRight.GetComponent<Renderer>().material.shader = Shader.Find("Custom/cvdBrettel");
        }
        else
        {
            //Brettel Dichromat Shader, GIMP anchors
            CameraPlaneLeft.GetComponent<Renderer>().material.shader = Shader.Find("Custom/cvdBrettelGimp");
            CameraPlaneRight.GetComponent<Renderer>().material.shader = Shader.Find("Custom/cvdBrettelGimp");
        }
	}

	// get propaty
	public Texture2D GetCameraTextureLeft()
	{
		return CameraTexLeft;
	}

	public Texture2D GetCameraTextureRight()
	{
		return CameraTexRight;
	}
}
