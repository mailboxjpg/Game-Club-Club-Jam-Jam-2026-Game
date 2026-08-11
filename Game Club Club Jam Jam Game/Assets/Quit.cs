using UnityEngine;

public class Quit : MonoBehaviour
{
    public void _Quit()
    {
        print("Application quit");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
