using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public enum DialogueState
{
    None,
    Typing,
    LineComplete
}
public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;

    [SerializeField] private RawImage nextIcon;

    [Header("Typing")]
    [SerializeField] private float typingSpeed = 0.03f;

    private DialogueData currentDialogue;

    private int currentIndex;

    private Coroutine typingCoroutine;


    private DialogueState currentState = DialogueState.None;


    private bool skipRequested;

    public bool isDialogueActive;

    public void StartDialogue(string fileName)
    {
        if (isDialogueActive)
            return;

        currentDialogue = DialogueLoader.Load(fileName);

        if (currentDialogue == null)
            return;
        isDialogueActive = true;

        currentIndex = 0;

        ShowLine();
    }

    public void ShowLine()
    {
        nextIcon.gameObject.SetActive(false);

        DialogueLine line = currentDialogue.lines[currentIndex];

        nameText.text = line.speaker;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line.text));
    }

    private IEnumerator TypeText(string text)
    {
        currentState = DialogueState.Typing;

        skipRequested = false;

        dialogueText.text = "";

        foreach (char c in text)
        {
            if (skipRequested)
            {
                dialogueText.text = text;
                break;
            }

            dialogueText.text += c;

            yield return new WaitForSeconds(typingSpeed);
        }

        currentState = DialogueState.LineComplete;

        nextIcon.gameObject.SetActive(true);
    }
    private void Update()
    {
        if (!isDialogueActive)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnPressSpace();
        }
    }

    private void OnPressSpace()
    {
        switch (currentState)
        {
            case DialogueState.Typing:
                skipRequested = true;
                break;
            case DialogueState.LineComplete:
                NextLine();
                break;
        }
    }

    private void NextLine()
    {
        currentIndex++;

        // ������ �ٱ��� �Ϸ�
        if (currentIndex >= currentDialogue.lines.Count)
        {
            EndDialogue();
            return;
        }

        ShowLine();
    }

    private void EndDialogue()
    {
        currentState = DialogueState.None;

        isDialogueActive = false;

        currentDialogue = null;

        dialoguePanel.SetActive(false);

    }
}