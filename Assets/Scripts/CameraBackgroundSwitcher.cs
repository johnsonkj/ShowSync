using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraBackgroundSwitcher : MonoBehaviour
{
    public Animator carAnimator;
    public GameObject explosionObject;

    void Start()
    {

    }

    public void LoadScene(int index)
    {
        SceneManager.LoadScene(index);
    }

    public void OnQuitApplication()
    {
        Application.Quit();
    }

    public void StartCarAnimation()
    {
        if(carAnimator!=null)
        {
            carAnimator.SetTrigger("Start");
        }
       
    }

    public void FingerSnapDetected()
    { 
        if(carAnimator!=null)
        {
            carAnimator.SetTrigger("Explode");
        }

        StartCoroutine(PlayBlastAnimation());
    }

    IEnumerator PlayBlastAnimation()
    {
        yield return new WaitForSeconds(8f);
        explosionObject.SetActive(false);
        explosionObject.SetActive(true);

    }
}
