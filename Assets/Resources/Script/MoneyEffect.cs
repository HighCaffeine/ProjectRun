using System.Collections;
using TMPro;
using UnityEngine;

public class MoneyEffect : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;

    [SerializeField] private RectTransform targetTransform;

    [Header("Roulette")]
    [SerializeField] private float randomInterval = 0.02f;

    [SerializeField] private float stopDelay = 0.3f;


    private Coroutine playRoutine;

    [SerializeField] private Animator animator;
    [SerializeField] private Animator stamp;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    public IEnumerator PlayRoutine(int targetMoney)
    {
        yield return StartCoroutine(MoneyRoutine(targetMoney));
    }

    private IEnumerator MoneyRoutine(int target)
    {
        string targetString = target.ToString().PadLeft(5, '0');

        int length = targetString.Length;

        char[] currentChars = new char[length];

        for (int i = 0; i < length; i++)
        {
            currentChars[i] = '0';
        }

        UpdateMoneyText(currentChars);

        for (int fixedCount = 1; fixedCount <= length; fixedCount++)
        {
            int currentFixedIndex = length - fixedCount;

            float timer = 0f;

            while (timer < stopDelay)
            {
                for (int i = 0; i < length; i++)
                {
                    if (i > currentFixedIndex)
                    {
                        currentChars[i] = targetString[i];
                    }
                    else
                    {
                        currentChars[i] =
                            (char)('0' + Random.Range(0, 10));
                    }
                }

                UpdateMoneyText(currentChars);

                timer += randomInterval;

                yield return new WaitForSeconds(randomInterval);
            }

            currentChars[currentFixedIndex] =
                targetString[currentFixedIndex];

            UpdateMoneyText(currentChars);
        }

        moneyText.text = string.Format("{0:N0}", target);

        yield return new WaitForSeconds(0.3f);

        animator.Play("Punch");
    }

    private void UpdateMoneyText(char[] chars)
    {
        string raw = new string(chars);

        moneyText.text = AddComma(raw);
    }

    private string AddComma(string number)
    {
        int count = 0;

        string result = "";

        for (int i = number.Length - 1; i >= 0; i--)
        {
            result = number[i] + result;

            count++;

            if (count % 3 == 0 && i != 0)
            {
                result = "," + result;
            }
        }

        return result;
    }
}