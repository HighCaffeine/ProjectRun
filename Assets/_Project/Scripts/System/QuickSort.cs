using System.Collections.Generic;

public static class QuickSort
{
    public static T[] GetSorting<T>(T[] data) where T : GetIntValue
    {
        QuickSortMethod(ref data, 0, data.Length - 1);

        return data;
    }


    private static void QuickSortMethod<T>(ref T[] data, int left, int right) where T : GetIntValue
    {
        if (left < right)
        {
            int pivot = Partition(ref data, left, right);

            QuickSortMethod(ref data, left, pivot - 1);
            QuickSortMethod(ref data, pivot + 1, right);
        }
    }

    private static int Partition<T>(ref T[] list, int left, int right) where T : GetIntValue
    {
        T pivot;
        int low, high;

        low = left;
        high = right + 1;

        pivot = list[left];

        do 
        {
            do
            {
                low++;
            } while (low <= right && list[low].GetIntForSort() < pivot.GetIntForSort());

            do
            {
                high--;
            } while (high >= left && list[high].GetIntForSort() > pivot.GetIntForSort());

            if (low < high)
            {
                Swap(ref list, low, high);
            }
        } while (low < high);

        Swap(ref list, left, high);

        return high;
    }

    public interface GetIntValue
    {
        public int GetIntForSort();
    }

    public static void Swap<T>(ref T[] list, int a, int b) 
    {
        T temp = list[a];
        list[a] = list[b];
        list[b] = temp;
    }

    public static void Swap<T>(List<T> list, int a, int b) 
    {
        T temp = list[a];
        list[a] = list[b];
        list[b] = temp;
    }
}
