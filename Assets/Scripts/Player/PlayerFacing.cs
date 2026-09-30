using UnityEngine;


[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerFacing : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera gameplayCamera;

    private PlayerInputReader _input;
    void Awake()
    {
        _input = GetComponent<PlayerInputReader>();

        if (gameplayCamera == null)
        {
            Debug.LogError(
                $"{nameof(PlayerFacing)} requires a gameplay camera reference.",
                this
            );

            enabled = false;
        }
    }

    void Update()
    {
        FacePointer();   
    }

    private void FacePointer()
    {

        // To anyone who reads this, the concept is fairly simple.
        // By shooting a ray and asking where it intersect with an imaginary plane normalized to the players position,
        // we are able to turn a 2D mouse coordinate on our screen say (1472, 624) to a proper 3D coord.
        Vector2 pointerPosition = _input.PointerPosition;

        // Create the ray
        Ray pointerRay = gameplayCamera.ScreenPointToRay(
            new Vector3(
                pointerPosition.x,
                pointerPosition.y,
                0f
            )
        );

        // Create an imaginary plane on the player
        Plane playerPlane = new Plane(
            Vector3.up,
            transform.position
        );

        if (!playerPlane.Raycast(pointerRay, out float distance))
            return;

        // We get the distance then we substract the vector
        Vector3 pointerWorldPosition = pointerRay.GetPoint(distance);
        Vector3 facingDirection = pointerWorldPosition - transform.position;

        facingDirection.y = 0f;
        if (facingDirection.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(
            facingDirection,
            Vector3.up
        );
    }
}
