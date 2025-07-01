using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;



public class JobSelectionUI : MonoBehaviour
{

    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Transform jobListContainer;
    [SerializeField] private GameObject jobPrefab;

    private string[] jobNames = { "Knight", "Wizard", "Archer" };

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < jobNames.Length; i++)
        {
            GameObject go = Instantiate(jobPrefab, jobListContainer);
            JobSelect item = go.GetComponent<JobSelect>();
            item.Initialize(jobNames[i], i, nicknameInput); 
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
