using UnityEngine;

public class CameraSwitch : MonoBehaviour
{
    public GameObject[] Cameras;

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < Cameras.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                foreach (var camera in Cameras)
                {
                    camera.SetActive(false);
                }

                Cameras[i].SetActive(true);
            }
        }
    }
}
