using UnityEngine;

public class Spin : MonoBehaviour
{
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
        transform.Rotate(Vector3.up * currentSpinSpeed * Time.deltaTime, Space.Self);
    }
}
