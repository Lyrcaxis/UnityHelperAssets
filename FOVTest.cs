using UnityEngine;

public class FOVTest : MonoBehaviour {
	public Transform Target;

	[Space]
	public float AngleX = 45;
	public float AngleY = 25;

	[Space, Header("Debug Visual Stuff")]
	public int Divisions = 50;
	public int PlaneDistance = 10;
	public bool DrawFOV = true;

	void Awake() {
		DrawFOV = false;
	}

	void Update() {
		if (IsWithinScope(Target.position)) { Debug.DrawLine(transform.position, Target.position, Color.green); }
		else { Debug.DrawLine(transform.position, Target.position, Color.red); }

		Debug.DrawRay(transform.position, transform.forward * 100, Color.yellow);
	}

	public bool IsWithinScope(Vector3 objectPosition) {
		var directionToObject = objectPosition - transform.position;

		var planeDistance = Vector3.Dot(transform.forward, directionToObject);

		var planeHeight = planeDistance * 2 * Mathf.Tan(AngleY * Mathf.Deg2Rad);
		var planeWidth = planeHeight * AngleX / AngleY;

		var objectDistanceFromPlanesCenter = new Vector2();
		objectDistanceFromPlanesCenter.x = Vector3.Dot(directionToObject, transform.right);
		objectDistanceFromPlanesCenter.y = Vector3.Dot(directionToObject, transform.up);

		bool passesXTest = Mathf.Abs(objectDistanceFromPlanesCenter.x) <= planeWidth / 2;
		bool passesYTest = Mathf.Abs(objectDistanceFromPlanesCenter.y) <= planeHeight / 2;

		return passesXTest && passesYTest;
	}

	// Visual representation of the Field of View
	void OnDrawGizmos() {
		if (!DrawFOV || Divisions <= 0 || PlaneDistance <= 0 || AngleX <= 0 || AngleY <= 0 ||  AngleY >= 90) { return; }

		// Define the plane
		Vector3 planeCenter = transform.position + PlaneDistance * transform.forward;
		var planeHeight = PlaneDistance * 2 * Mathf.Tan(AngleY * Mathf.Deg2Rad);
		var planeWidth = planeHeight * AngleX / AngleY;

		// Divide the plane and draw rays towards it
		for (float x = -planeWidth / 2; x < planeWidth / 2; x += planeWidth / Divisions) {
			for (float y = -planeHeight / 2; y < planeHeight / 2; y += planeHeight / Divisions) {
				var positionOnPlane = planeCenter;
				positionOnPlane += x * transform.right;
				positionOnPlane += y * transform.up;

				// Make up for floating point errors
				positionOnPlane += 0.000001f * transform.forward;
        
				Gizmos.color = IsWithinScope(positionOnPlane) ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.01f);
				Gizmos.DrawLine(transform.position, positionOnPlane);
			}
		}

	}
}
