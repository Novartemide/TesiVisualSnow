using UnityEngine;

public class FloatersMovement : MonoBehaviour
{
    private Vector3 offset;
    void Update()
    {
        // Movimento fluttuante casuale e lento
        float x = Mathf.Sin(Time.time * 0.5f) * 0.2f;
        float y = Mathf.Cos(Time.time * 0.3f) * 0.2f;
        transform.localPosition += new Vector3(x, y, 0) * Time.deltaTime;
    }
}