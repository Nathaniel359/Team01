using UnityEngine;
using UnityEngine.UI;

public class StartupGuide : MonoBehaviour
{
    public Sprite page1;
    public Sprite page2;
    public Image guideImage;
    public GameObject pg1Buttons;
    public GameObject pg2Buttons;
    public GameObject mainMenuCanvas;
    public GameObject guideCanvas;

    private int currentPage = 1;

    void Start()
    {
        guideImage.sprite = page1;
        guideImage.gameObject.SetActive(true);
        pg1Buttons.SetActive(true);
        pg2Buttons.SetActive(false);
        mainMenuCanvas.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetButtonDown(InputMappings.ButtonY) || Input.GetKeyDown(KeyCode.Y))
        {
            NextPage();
        }

        if(currentPage == 2 && (Input.GetButtonDown(InputMappings.ButtonX) || Input.GetKeyDown(KeyCode.X)))
        {
            PrevPage();
        }
    }

    public void NextPage()
    {
        if(currentPage == 2)
        {
            guideCanvas.SetActive(false);
            mainMenuCanvas.SetActive(true);
        } else
        {
            guideImage.sprite = page2;
            pg1Buttons.SetActive(false);
            pg2Buttons.SetActive(true);
            currentPage++;
        }
    }

    public void PrevPage()
    {
        guideImage.sprite = page1;
        pg2Buttons.SetActive(false);
        pg1Buttons.SetActive(true);
        currentPage--;
    }
}
