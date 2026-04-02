using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Devcat;
using UnityEngine;

public class DataManager : GenericSingleton<DataManager>
{
    public static string ResourcePath = Path.Combine(Application.dataPath, "Resources");
    static Dictionary<string, int> TextToVector(string[] words)
    {
        Dictionary<string, int> vector = new Dictionary<string, int>();

        foreach (string word in words)
        {
            if (vector.ContainsKey(word))
            {
                vector[word]++;
            }
            else
            {
                vector[word] = 1;
            }
        }
        return vector;
    }

    static float CosineSimilarity(Dictionary<string, int> vector1, Dictionary<string, int> vector2)
    {
        var intersection = vector1.Keys.Intersect(vector2.Keys);
        float numerator = intersection.Sum(key => vector1[key] * vector2[key]);

        float sum1 = vector1.Values.Sum(value => Mathf.Pow(value, 2));
        float sum2 = vector2.Values.Sum(value => Mathf.Pow(value, 2));

        float denominator = Mathf.Sqrt(sum1) * Mathf.Sqrt(sum2);

        if (denominator == 0)
        {
            return 0.0f;
        }
        else
        {
            return numerator / denominator;
        }
    }

    static void TEST(string[] word1, string[] word2)
    {
        // 단어를 벡터로 변환
        Dictionary<string, int> vector1 = TextToVector(word1);
        Dictionary<string, int> vector2 = TextToVector(word2);

        // 코사인 유사도 계산
        float similarity = CosineSimilarity(vector1, vector2);

        Debug.Log(similarity);
    }
    private static double GetCosineSimilarity(int[] a, int[] b)
    {
        double dataA = 0;
        double dataB = 0;
        double product = 0;


        for (int k = 0; k < 3; k++)
        {
            dataA += Mathf.Pow(a[k], 2);
            dataB += Mathf.Pow(b[k], 2);
            product += (a[k] * 1.0 * b[k]);
        }

        double dataAB = Mathf.Sqrt((float)(dataA * dataB));
        return product / dataAB;
    }
}
