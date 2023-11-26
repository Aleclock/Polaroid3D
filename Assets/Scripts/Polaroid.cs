using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;
using System.IO;
using EzySlice;

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


	public LayerMask cameraPlanesLayerMask;
	public Material fillMaterial;
    private List<GameObject> cameraPlanes = new List<GameObject>();
	private UnityEngine.Plane[] planes;
	private Texture PictureTexture;
	private GameObject PhotoOutputParent;
	private GameObject[] objsToSnapshot; 

    private void Awake() {

		inputReader.TriggerLeftHandActivateEvent += TriggerClicked;

		planes = new UnityEngine.Plane[6]; // TODO forse non serve
        parentCamera = transform.parent;
        snappedPictureTexture = new Texture2D(pictureRenderTexture.width, pictureRenderTexture.height, pictureRenderTexture.graphicsFormat, TextureCreationFlags.None);
        pictureMaterial = picture.GetComponent<Renderer>().material;

		//CreateCameraPlanes();

		objsToSnapshot = GameObject.FindGameObjectsWithTag("SnapShotObjects");
    }

	private void TriggerClicked()
	{
		if (picture.activeSelf)
		{
			if (!snapShotSaved)
			{
				TakeSnapshot();
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
						TakeSnapshot();
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

        for (int i = 0; i < planes.Length; i += 1) 
		{
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);

            plane.name = string.Format("Plane {0}", i.ToString());
            plane.layer = (int) Mathf.Log(cameraPlanesLayerMask.value, 2);
            plane.transform.parent = cameraPlanesWrapper.transform;

            plane.GetComponent<Renderer>().enabled = false;

            cameraPlanes.Add(plane);
        }

		for (int i = 0; i < planes.Length; i += 1) 
		{
            cameraPlanes[i].transform.position = -planes[i].normal * planes[i].distance;
            cameraPlanes[i].transform.rotation = Quaternion.FromToRotation(Vector3.up, planes[i].normal);
        }
	}

	private void TakeSnapshot()
	{
		CreateCameraPlanes(); // TODO forse posso farlo una volta all'inizio e poi metterlo come figlio della camera
		snapShotSaved = true;

		Texture2D snappedPictureTexture = toTexture2D(pictureRenderTexture);
		pictureMaterial.mainTexture = snappedPictureTexture;

		List<GameObject> goInFrustum = new List<GameObject>();
		
		foreach (GameObject go in objsToSnapshot)
		{
			if (!go.activeInHierarchy)
				continue;

			go.TryGetComponent<Renderer>(out var renderer);
			if (!renderer)
				continue;

			var bounds = renderer.bounds;

			if (GeometryUtility.TestPlanesAABB(planes, bounds))
				goInFrustum.Add(go);
		}
		
		/*
		UnityEngine.Plane[] planesFlipped = getPlanesFlipped(planes);
		var planesPP = GeometryUtility.CalculateFrustumPlanes(cameraPolaroid);
		for (int i=0; i < planesPP.Length; i++)
		{
			planesPP[i].Flip();
		}

		foreach (GameObject gg in objsToSnapshot)
		{
			gg.TryGetComponent<Renderer>(out var rendererOut);
			if (!rendererOut)
				continue;

			var bb = rendererOut.bounds;

			if (GeometryUtility.TestPlanesAABB(planesPP, bb))
				print(gg.name);
		}
		*/
		SliceObjects(goInFrustum);
	}

	private UnityEngine.Plane[] getPlanesFlipped(UnityEngine.Plane[] planes)
	{
		List<UnityEngine.Plane> flippedPlanes = new List<UnityEngine.Plane>();
		foreach (UnityEngine.Plane p in planes)
		{
			flippedPlanes.Add(p.flipped);
		}

		return flippedPlanes.ToArray();
	}

	private void SliceObjects(List<GameObject> gameObjects)
	{
		PhotoOutputParent = new GameObject("Photo Output");
		PhotoOutputParent.transform.position = cameraPolaroid.transform.position;
		PhotoOutputParent.transform.rotation = cameraPolaroid.transform.rotation;

		foreach (GameObject original in gameObjects)
		{
			GameObject copy = original; // TODO set tag SnapShotObjects
			copy.tag = "SnapShotObjects";
			GameObject temporary;
			Mesh meshCopy = new Mesh();

			foreach (GameObject plane in cameraPlanes)
			{
				SlicedHull hull = copy.Slice(plane.transform.position, plane.transform.up); 
				
				if (hull != null)
				{
					meshCopy = hull.upperHull;
					temporary = hull.CreateUpperHull(copy, fillMaterial);
					copy = temporary;
					Destroy(temporary);
				}
			}
			/*
			GameObject gg = new GameObject("miao");
			gg.AddComponent<MeshFilter>();
			gg.AddComponent<MeshRenderer>();
			gg.GetComponent<MeshFilter>().mesh = copy.GetComponent<MeshFilter>().mesh;
			*/
			AddOriginalComponents(original, copy);
			Instantiate(copy, PhotoOutputParent.transform, true);

			// TODO add copy to objsToSnapshot 
		}
		PhotoOutputParent.SetActive(false);
	}

	// https://discussions.unity.com/t/copy-a-component-at-runtime/71172/3
	private void AddOriginalComponents(GameObject original, GameObject destination)
	{
		List<string> typesToIgnore = new List<string>{"Transform", "MeshRenderer", "BoxCollider", "MeshFilter"};
		Component[] components = original.GetComponents(typeof(Component));
		foreach (Component c in components)
		{
			System.Type type = c.GetType();
			if (typesToIgnore.Contains(type.Name))
				continue;

			Debug.Log(type.Name);

			Component copy = destination.AddComponent(type);
			// Copied fields can be restricted with BindingFlags
			System.Reflection.FieldInfo[] fields = type.GetFields(); 
			foreach (System.Reflection.FieldInfo field in fields)
			{
				field.SetValue(copy, field.GetValue(original));
			}
		}
	}
}
