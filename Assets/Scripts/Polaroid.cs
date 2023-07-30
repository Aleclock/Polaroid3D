using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;

public class Polaroid : MonoBehaviour
{
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

    private void Start() {
        parentCamera = transform.parent;
        snappedPictureTexture = new Texture2D(pictureRenderTexture.width, pictureRenderTexture.height, pictureRenderTexture.graphicsFormat, TextureCreationFlags.None);
        pictureMaterial = picture.GetComponent<Renderer>().material;
    }

    private void Update() {
		if (Input.GetKeyDown(KeyCode.Space))
		{
			if (picture.activeSelf)
			{
				if (!snapShotSaved)
				{
					Snapshot();
				}
				else
				{
					Place();
				}
			}
			else
				picture.SetActive(true);
		}
    }

	private void Place()
    {
		picture.SetActive(false);
		snapShotSaved = false;

		foreach (GameObject obj in toBePlaced)
		{
			obj.SetActive(true);
			Vector3 difference = new Vector3(
				picture.transform.position.x - obj.transform.position.x,
				picture.transform.position.y - obj.transform.position.y,
				picture.transform.position.z - obj.transform.position.z);

			Vector3 direction = new Vector3(
				obj.transform.position.x - cameraPolaroid.transform.position.x,
				obj.transform.position.y - cameraPolaroid.transform.position.y,
				obj.transform.position.z - cameraPolaroid.transform.position.z
			);
			
			Vector3 look = cameraPolaroid.transform.TransformDirection(Vector3.forward);
			look = Quaternion.Euler(lookDirection) * look;
			//look.x *= direction.x;
			//look.y *= direction.y;
			//look.z *= direction.z;

			//transform.position = transform.position + Camera.main.transform.forward * distance * Time.deltaTime;


			obj.transform.position += cameraPolaroid.transform.position + look * distance; // + difference;

		}

		toBePlaced.Clear();
		pictureMaterial.SetTexture("_UnlitColorMap", pictureRenderTexture);
    }

    private void Snapshot()
    {
        toBePlaced.Clear();
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cameraPolaroid);
        var staticObjects = GameObject.FindGameObjectsWithTag("StaticObject");
	    var dynamicObjects = GameObject.FindGameObjectsWithTag("DynamicObject");

        foreach (GameObject staticObject in staticObjects)
        {
            var mesh = staticObject.GetComponent<MeshFilter>().mesh;
            var meshVertices = mesh.vertices.ToList();
            var meshTriangles = mesh.triangles;

            var matchingVertices = GetVerticesInsideViewFrustum(staticObject, in meshVertices, planes);
            if (matchingVertices.Count == 0)  continue;

            List<int> newTriangles = GetTrianglesInsideViewFrustrum(staticObject.transform, ref meshVertices, ref matchingVertices, in meshTriangles, in planes);
            RemapVerticesAndTrianglesToNewMesh(in matchingVertices, in meshVertices, ref newTriangles, out var newVertices);
            PrepareCreateNewObject(staticObject, newVertices, newTriangles);
        }

        snapShotSaved = true;
        Graphics.CopyTexture(pictureRenderTexture, snappedPictureTexture);
        pictureMaterial.SetTexture("_UnlitColorMap", snappedPictureTexture);

		Vector3 look = cameraPolaroid.transform.TransformDirection(Vector3.forward);

		Vector3 direction = new Vector3(
			toBePlaced[0].transform.position.x - cameraPolaroid.transform.position.x,
			toBePlaced[0].transform.position.y - cameraPolaroid.transform.position.y,
			toBePlaced[0].transform.position.z - cameraPolaroid.transform.position.z
		);

		direction = toBePlaced[0].transform.position - cameraPolaroid.transform.position;
		//Debug.Log(Vector3.SignedAngle(look, direction, Vector3.left) + " - " + Vector3.SignedAngle(look, direction, Vector3.up));
		lookDirection = Quaternion.FromToRotation(look, direction).eulerAngles;
		distance = Vector3.Distance(cameraPolaroid.transform.position, toBePlaced[0].transform.position);
    }

    private List<int> GetTrianglesInsideViewFrustrum(Transform gameObjectTransform, ref List<Vector3> meshVertices, ref List<int> matchingVertices, in int[] meshTriangles, in Plane[] planes)
	{
		// iterate the triangle list (using i += 3) checking if
		// each vertex of the triangle is inside the frustum (using previously calculated matching vertices)
		var newTriangles = new List<int>();
		for (var i = 0; i < meshTriangles.Length; i += 3)
		{
			var contain = new[] {false, false, false}.ToList();
			for (var j = 0; j < 3; j++)
			{
				if (matchingVertices.Contains(meshTriangles[i + j]))
					contain[j] = true;
			}

			var count = contain.Count(x => x);
			if (count == 3)
			{
				newTriangles.AddRange(new[] {meshTriangles[i], meshTriangles[i + 1], meshTriangles[i + 2]});
			}

			if (count == 2)
			{
				var outsiderIndex = contain.IndexOf(false);
				var insiderIndex = contain.IndexOf(true);
				var insiderIndex2 = contain.LastIndexOf(true);
				var outsider = meshVertices[meshTriangles[i + outsiderIndex]];
				var insider = meshVertices[meshTriangles[i + insiderIndex]];
				var insider2 = meshVertices[meshTriangles[i + insiderIndex2]];

				Ray ray1 = new Ray(gameObjectTransform.TransformPoint(insider),
					gameObjectTransform.TransformPoint(outsider) - gameObjectTransform.TransformPoint(insider));
				Ray ray2 = new Ray(gameObjectTransform.TransformPoint(insider2),
					gameObjectTransform.TransformPoint(outsider) - gameObjectTransform.TransformPoint(insider2));
				foreach (var plane in planes)
				{
					if (!plane.GetSide(outsider))
					{
						if (plane.Raycast(ray1, out var enter) && plane.Raycast(ray2, out var enter2))
						{
							var point = gameObjectTransform.InverseTransformPoint(ray1.GetPoint(enter));
							var point2 = gameObjectTransform.InverseTransformPoint(ray2.GetPoint(enter2));
							newTriangles.AddRange(
								GetClockwiseRotation(
									new[] {outsiderIndex, insiderIndex, insiderIndex2},
									new[] {meshVertices.Count, meshTriangles[i + insiderIndex], meshTriangles[i + insiderIndex2]}
								)
							);
							matchingVertices.Add(meshVertices.Count);
							meshVertices.Add(point);
							newTriangles.AddRange(
								GetClockwiseRotation(
									new[] {outsiderIndex, insiderIndex, insiderIndex2},
									new[] {meshVertices.Count - 1, meshTriangles[i + insiderIndex2], meshVertices.Count}
								)
							);
							matchingVertices.Add(meshVertices.Count);
							meshVertices.Add(point2);
						}
					}
				}
			}
			if (count == 1)
			{
				var insiderIndex = contain.IndexOf(true);
				var outsiderIndex = contain.IndexOf(false);
				var outsiderIndex2 = contain.LastIndexOf(false);
				var insider = meshVertices[meshTriangles[i + insiderIndex]];
				var outsider = meshVertices[meshTriangles[i + outsiderIndex]];
				var outsider2 = meshVertices[meshTriangles[i + outsiderIndex2]];

				Ray ray1 = new Ray(gameObjectTransform.TransformPoint(insider),
					gameObjectTransform.TransformPoint(outsider) - gameObjectTransform.TransformPoint(insider));
				Ray ray2 = new Ray(gameObjectTransform.TransformPoint(insider),
					gameObjectTransform.TransformPoint(outsider2) - gameObjectTransform.TransformPoint(insider));
				foreach (var plane in planes)
				{
					if (!plane.GetSide(outsider))
					{
						if (plane.Raycast(ray1, out var enter) && plane.Raycast(ray2, out var enter2))
						{
							var point = gameObjectTransform.InverseTransformPoint(ray1.GetPoint(enter));
							var point2 = gameObjectTransform.InverseTransformPoint(ray2.GetPoint(enter2));
							newTriangles.AddRange(
								GetClockwiseRotation(
									new[] {insiderIndex, outsiderIndex, outsiderIndex2},
									new[] {meshTriangles[i + insiderIndex], meshVertices.Count, meshVertices.Count + 1}
								)
							);
							matchingVertices.AddRange(new []{meshVertices.Count, meshVertices.Count + 1});
							meshVertices.Add(point);
							meshVertices.Add(point2);
						}
					}
				}
			}
		}
		return newTriangles;
	}

    private void RemapVerticesAndTrianglesToNewMesh(in List<int> matchingVertices, in List<Vector3> meshVertices, ref List<int> newTriangles, out List<Vector3> newVertices)
	{
		newVertices = new List<Vector3>();
		foreach (var i in matchingVertices)
		{
			newVertices.Add(meshVertices[i]);
		}
		
		var dictionary = new Dictionary<int, int>();
		for (int i = 0; i < matchingVertices.Count; i++)
		{
			dictionary.Add(matchingVertices[i], i);
		}
	
		for (int i = 0; i < newTriangles.Count; i++)
		{
			newTriangles[i] = dictionary[newTriangles[i]];
		}
	}

	private void PrepareCreateNewObject(GameObject obj, List<Vector3> newVertices, List<int> newTriangles)
	{
		var newObject = Instantiate(obj, parent: parentCamera, position: obj.transform.position, rotation: obj.transform.rotation);
		newObject.transform.localPosition += offsetFromParent;
		newObject.SetActive(false);

		var newMesh = new Mesh();

		newMesh.SetVertices(newVertices);
		newMesh.SetTriangles(newTriangles.ToArray(), 0);
		newObject.GetComponent<MeshFilter>().mesh = newMesh;
		newMesh.RecalculateNormals();
		
		toBePlaced.Add(newObject);
		Debug.Log(toBePlaced.Count);
	}

    private int[] GetClockwiseRotation(int[] index, int[] triangles)
	{ 
		if (index[0] > index[1] && index[0] < index[2])
		{
			return new[] {triangles[1], triangles[0], triangles[2]};
		}
		return new[] {triangles[0], triangles[1], triangles[2]};
	}

    private List<int> GetVerticesInsideViewFrustum(GameObject obj, in List<Vector3> meshVertices, Plane[] planes)
    {
        var matchingVertices = new List<int>();
        for (var i = 0; i < meshVertices.Count; i++)
        {
            Vector3 vertex = meshVertices[i];
            if (IsInsideFrustum(obj.transform.TransformPoint(vertex), planes))
                matchingVertices.Add(i); 
        }
        return matchingVertices;
    }

    private bool IsInsideFrustum(Vector3 point, IEnumerable<Plane> planes)
	{
		return planes.All(plane => plane.GetSide(point));
	}

	/*
	private void OnDrawGizmos() {
		Gizmos.color = Color.blue;
		Vector3 look = cameraPolaroid.transform.TransformDirection(Vector3.forward);
		Gizmos.DrawLine(cameraPolaroid.transform.position, cameraPolaroid.transform.position + (look * 2));
	}
	*/
}
