using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraBackgroundSwitcher : MonoBehaviour
{
    public Animator carAnimator;
    public GameObject explosionObject;
    public GameObject RestartPanel;
    public PythonBoolReceiver streamReciever;
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
            carAnimator.SetBool("Start",true);
        }
       
    }

    public void FingerSnapDetected()
    { 
        if(carAnimator!=null)
        {
            SetAnimationBoolsFalse();
            carAnimator.SetBool("Explode", true);
        }

        StartCoroutine(PlayBlastAnimation());
    }

    IEnumerator PlayBlastAnimation()
    {
        yield return new WaitForSeconds(4.5f);
        explosionObject.SetActive(false);
        explosionObject.SetActive(true);
        yield return new WaitForSeconds(3f);
        RestartPanel.SetActive(true);
        SetAnimationBoolsFalse();
    }

    public void SetAnimationBoolsFalse()
    {
         carAnimator.SetBool("Explode", false);
         carAnimator.SetBool("Restart",false);
    }

    public void OnRestartButtonClicked()
    { 
        carAnimator.SetBool("Restart",true);
        streamReciever.animationTriggered = false;
    }

}
