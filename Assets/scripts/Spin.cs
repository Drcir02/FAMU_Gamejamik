using UnityEngine;

public class Spin : MonoBehaviour
{
    public enum Axis { X, Y, Z }
    public Axis spinAxis = Axis.Y;
    public float spinRate = 1600f; // degrees per second  

    private float currentSpinSpeed = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        currentSpinSpeed = Mathf.Lerp(currentSpinSpeed, spinRate, Time.deltaTime * 5f);
        
        Vector3 axisVector = Vector3.up;
        switch (spinAxis)
        {
            case Axis.X: axisVector = Vector3.right; break;
            case Axis.Y: axisVector = Vector3.up; break;
            case Axis.Z: axisVector = Vector3.forward; break;
        }

        transform.Rotate(axisVector * currentSpinSpeed * Time.deltaTime, Space.Self);
    }
}
