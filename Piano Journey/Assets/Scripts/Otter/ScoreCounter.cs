using System.Net.Mime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreCounter : MonoBehaviour
{
    public static float m_ScoreText {get; set;}
    public Text m_Text;
    // Update is called once per frame
    void Update()
    {
        m_Text.text = m_ScoreText.ToString();
    }
}
