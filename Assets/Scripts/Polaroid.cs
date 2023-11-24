using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;
using System.IO;
using ViewFinder.Gameplay;

public class Polaroid : MonoBehaviour
{
	[SerializeField] private InputReader inputReader;
    [SerializeField] private Vector3 offsetFromParent;
    [FormerlySerializedAs("renderTexture")] public RenderTexture pictureRenderTexture;
    [SerializeField] private Camera cameraPolaroid;
    [SerializeField] private GameObject picture;
    private Transform parentCamera;
    private Material pictureMaterial;
    private bool snapShotSaved = false;
    private List<GameObject> toBePlaced = new List<GameObject>();
    private Texture2D snappedPictureTexture;

	private Vector3 lookDirection;
	private float distance;

	public LayerMask cameraPlanesLayerMask;
    private List<GameObject> cameraPlanes = new List<GameObject>();
	private Plane[] planes;
	private List<Slicerable> projections;
	private Texture PictureTexture;
	private GameObject PhotoOutputParent;

    private void Awake() {

		inputReader.TriggerLeftHandActivateEvent += TriggerClicked;

		planes = new Plane[6];
        parentCamera = transform.parent;
        snappedPictureTexture = new Texture2D(pictureRenderTexture.width, pictureRenderTexture.height, pictureRenderTexture.graphicsFormat, TextureCreationFlags.None);
        pictureMaterial = picture.GetComponent<Renderer>().material;

		CreateCameraPlanes();
    }

	private void TriggerClicked()
	{
		if (picture.activeSelf)
		{
			if (!snapShotSaved)
			{
				//if (toBePlaced.Count == 0)
				Snapshot();
			}
			else
			{
				Place();
			}
		}
	}

    private void Update() {
		if (Input.GetKeyDown(KeyCode.Return))
		{
			picture.SetActive(!picture.activeSelf);
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			if (picture.activeSelf)
			{
				if (!snapShotSaved)
				{
					if (toBePlaced.Count == 0)
						Snapshot();
				}
				else
				{
					Place();
				}
			}
		}
    }

	private void Place()
	{
		snapShotSaved = false;
		
		PhotoOutputParent.SetActive(true);
		PhotoOutputParent.transform.position = cameraPolaroid.transform.position;
		PhotoOutputParent.transform.rotation = cameraPolaroid.transform.rotation;
		pictureMaterial.mainTexture = pictureRenderTexture;
	}

	private Texture2D toTexture2D(RenderTexture rTex)
	{
		Texture2D tex = new Texture2D(512, 512, TextureFormat.RGB24, false);
		// ReadPixels looks at the active RenderTexture.
		RenderTexture.active = rTex;
		tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
		tex.Apply();
		return tex;
	}

	// https://github.com/neogeek/Unity-Snippets/blob/master/GeometryUtility/CalculateFrustumPlanes.md
	private void CreateCameraPlanes()
	{
		GeometryUtility.CalculateFrustumPlanes(cameraPolaroid, planes);

        GameObject cameraPlanesWrapper = new GameObject("Camera Planes");

        for (int i = 0; i < planes.Length; i += 1) {

            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);

            plane.name = string.Format("Plane {0}", i.ToString());
            plane.layer = (int) Mathf.Log(cameraPlanesLayerMask.value, 2);
            plane.transform.parent = cameraPlanesWrapper.transform;

            plane.GetComponent<Renderer>().enabled = false;

            cameraPlanes.Add(plane);

        }

		for (int i = 0; i < planes.Length; i += 1) {

            cameraPlanes[i].transform.position = -planes[i].normal * planes[i].distance;
            cameraPlanes[i].transform.rotation = Quaternion.FromToRotation(Vector3.up, planes[i].normal);

        }
	}

	private void Snapshot()
	{
		//GeometryUtility.CalculateFrustumPlanes(cameraPolaroid, planes);
		projections = new List<Slicerable>();

		snapShotSaved = true;
		Slicerable[] Projections = FindObjectsOfType<Slicerable>().Where(p => p.isActiveAndEnabled).ToArray();
		Texture2D snappedPictureTexture = toTexture2D(pictureRenderTexture);
		pictureMaterial.mainTexture = snappedPictureTexture;

		foreach (var projection in Projections)
		{
			if (!projection.gameObject.activeInHierarchy)
				continue;
			projection.TryGetComponent<Renderer>(out var renderer);
			if (!renderer)
				continue;
			
			var bounds = renderer.bounds;

			if (GeometryUtility.TestPlanesAABB(planes, bounds))
				projections.Add(projection);
		}

		gameObject.SetActive(true); // TODO forse togliere

		CopyObjects();

	}

	private void CopyObjects()
	{
		PhotoOutputParent = new GameObject("Photo Output");
		PhotoOutputParent.transform.position = cameraPolaroid.transform.position;
		PhotoOutputParent.transform.rotation = cameraPolaroid.transform.rotation;

		foreach (var original in projections)
		{
			var copy = Instantiate(original, PhotoOutputParent.transform, true);
			var renderCopy = copy.GetComponent<Renderer>();
			var renderOriginal = original.GetComponent<Renderer>();
			copy.SetAsCopy();

			renderCopy.lightmapIndex = renderOriginal.lightmapIndex;
			renderCopy.lightmapScaleOffset = renderOriginal.lightmapScaleOffset;
			MeshUtils.CutByPlanes(copy, planes);

			if (copy.GetComponent<MeshFilter>()?.mesh.vertices.Length == 0)
				Destroy(copy);
		}

		PhotoOutputParent.SetActive(false);
	}
}
