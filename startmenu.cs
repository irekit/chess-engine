using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class startmenu : MonoBehaviour
{
    void Update()
    {
        persist.samples = (int)Mathf.Round(GetComponent<Slider>().value * 600 + 100);
    }
    public void PlayAsWhite()
    {
        persist.flipped = false;
        SceneManager.LoadScene("game");
    }
    public void PlayAsBlack()
    {
        persist.flipped = true;
        SceneManager.LoadScene("game");
    }
}
