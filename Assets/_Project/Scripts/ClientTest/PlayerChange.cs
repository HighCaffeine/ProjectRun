using NUnit.Framework.Internal.Commands;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class PlayerChange : MonoBehaviour
{
    [SerializeField]
    private List<PlayerActor> players;
    private int currentIndex = 0;

    public Transform camPivot;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        players[1].SetLocal(false);
    }

    // Update is called once per frame
    void Update()
    {  
       if(Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchPlayer();
        }
    }

    public void SwitchPlayer()
    {
        players[currentIndex].SetLocal(false);

        currentIndex = (currentIndex + 1) % players.Count;

        SetLocalPlayer(currentIndex);
    }

    void SetLocalPlayer(int index)
    {
        camPivot.SetParent(players[index].transform);
        camPivot.localPosition = Vector3.zero;
        players[index].SetLocal(true);

    }


}
