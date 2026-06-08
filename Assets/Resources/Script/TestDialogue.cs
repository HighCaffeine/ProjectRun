using UnityEngine;

public class TestDialogue : MonoBehaviour
{
    [SerializeField] private GameObject dialoguePanel;

    [SerializeField] private Animator resultWindow; 
    private void Start()
    {
        dialoguePanel.SetActive(false);

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            dialoguePanel.SetActive(true);
            resultWindow.Play("Open");
           // dialogueManager.StartDialogue("Test");
        }
       
    }
}