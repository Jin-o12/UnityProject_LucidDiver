using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LogTextBox : MonoBehaviour
{
    [Header("text")]
    [SerializeField] TMP_Text textName;
    [SerializeField] TMP_Text textDialogue;

    public void SetTextBox(string _name, string _dialogue)
    {
        textName.text = _name;
        textDialogue.text = _dialogue;
    }
}
