using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.IO;
using Newtonsoft.Json;

public class Texts
{
    public string textTitle;
    public string text;
}

public class WordCountScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public TextMesh Display;
    public TextMesh SubmitDisplay;
    public TextMesh[] Numbers;
    public TextAsset Texts;

    public KMSelectable[] Keyage;
    public KMSelectable Submit;
    public KMSelectable Clear;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    int DisplayNumber;
    string answer = "";
    string keyboardLayout = "QWERTYUIOPASDFGHJKLZXCVBNM";
    string input = "";

    List<Texts> allWritings;

    private void Start()
    {
        allWritings = JsonConvert.DeserializeObject<List<Texts>>(Texts.text);
        _moduleId = _moduleIdCounter++;
        Audio.PlaySoundAtTransform("startupSound", transform);

        foreach (KMSelectable key in Keyage)
        {
            key.OnInteract += delegate () { KeyPress(key); return false; };
        }

        Submit.OnInteract += delegate () { submitPress(Submit); return false; };

        Clear.OnInteract += delegate () { clearPress(Clear); return false; };

        Solution();
    }

    void clearPress(KMSelectable Clear)
    {
        Clear.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Clear.transform);
        if (!_moduleSolved)
        {
            input = "";
            SubmitDisplay.text = "";
            Display.text = DisplayNumber.ToString();
        }
    }

    void submitPress(KMSelectable Submit)
    {
        Submit.AddInteractionPunch();
        if (input == answer)
        {
            Module.HandlePass();
            Audio.PlaySoundAtTransform("solveSound", transform);
            SubmitDisplay.text = "";
            Display.text = "GG!";
            Debug.LogFormat("[Word Count #{0}] {1} was correct!", _moduleId, answer);
        }
        else
        {
            Module.HandleStrike();
            Audio.PlaySoundAtTransform("strikeSound", transform);
            Debug.LogFormat("[Word Count #{0}] {1}", _moduleId, input + " was incorrect.");
            input = "";
            SubmitDisplay.text = "";
            Display.text = DisplayNumber.ToString();
        }
    }
    void KeyPress(KMSelectable key)
    {
        key.AddInteractionPunch();
        Audio.PlaySoundAtTransform("keyboardsound", key.transform);
        for (int t = 0; t < 27; t++)
        {
            if (Keyage[t] == key)
            {
                if (t == 26)
                {
                    hashPress();
                    return;
                }
                input += keyboardLayout[t];
                Display.text = "";
                SubmitDisplay.text = input;
            }
        }
    }

    void hashPress()
    {
        if (keyboardLayout == "QWERTYUIOPASDFGHJKLZXCVBNM")
        {
            keyboardLayout = "0123456789ASDFGHJKLZXCVBNM";
        }
        else
        {
            keyboardLayout = "QWERTYUIOPASDFGHJKLZXCVBNM";
        }
        for (int i = 0; i < 10; i++)
        {
            Numbers[i].text = keyboardLayout[i].ToString();
        }
    }

    //Handles remove symbols and return word array.
    private string[] filterWriting(int writingPosition)
    {
        string writingText = new string((from c in allWritings[writingPosition].text
                                         where char.IsWhiteSpace(c) || char.IsLetterOrDigit(c)
                                         select c).ToArray());
        string[] writingWords = writingText.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        return writingWords;
    }

    private void Solution()
    {
        int writing = (BombInfo.GetSerialNumberLetters().Any(ch => "AEIOU".Contains(ch)) ? 0 : 10) + (BombInfo.GetSerialNumber()[5] - '0');
        Debug.LogFormat("[Word Count #{0}] The selected writing is titled {1}", _moduleId, allWritings[writing].textTitle);

        DisplayNumber = Rnd.Range(1, filterWriting(writing).Count());
        Display.text = DisplayNumber.ToString();
        Debug.LogFormat("[Word Count #{0}] The number on the display is {1}", _moduleId, DisplayNumber);

        answer = filterWriting(writing)[DisplayNumber - 1].ToUpper();
        Debug.LogFormat("[Word Count #{0}] Your word is {1}", _moduleId, answer);

        /* DEBUG
        string test = "";
        string counter = "";

        for (int i = 0; i < 20; i++)
        {
            string[] writingDebug = filterWriting(i);
            counter = "";
            // test += "\n Text " + i + ": \n";
            // foreach (string s in writing) { test += s + "\n"; };
            
            for (int j = 100; j <= 1000; j += 100)
            {
                if (j == 1000)
                {
                    int lastPos = Math.Min(j, writingDebug.Length);
                    counter += " P" + lastPos + ": " + writingDebug[lastPos - 2] + " \"" + writingDebug[lastPos - 1] + "\"";
                }
                else
                    counter += " P" + j + ": " + writingDebug[j - 2] + " \"" + writingDebug[j - 1] + "\" " + writingDebug[j] + "\n";
            }            
            test += "Text " + i + " (Length " + writingDebug.Length + "): \n" + counter + "\n";
        }
        StreamWriter sw = new StreamWriter("D:\\Test.txt");
        sw.WriteLine(test);
        sw.Close();
        */
    }
    //Twitch Plays.
    string TwitchHelpMessage = "Submit the correct answer with !{0} submit <answer>.";

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToUpperInvariant();
        Match m = Regex.Match(command, @"^(?:SUBMIT) +([A-Z0-9]+)$");
        if (!m.Success)
            yield break;
        yield return null;
        foreach (char cmd in m.Groups[1].Value) //execute;
        {
            if (!keyboardLayout.Contains(cmd))
            {
                Keyage[26].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            Keyage[keyboardLayout.IndexOf(cmd)].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        Submit.OnInteract();
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        foreach (char ans in answer) //execute;
        {
            if (!keyboardLayout.Contains(ans))
            {
                Keyage[26].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            Keyage[keyboardLayout.IndexOf(ans)].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        Submit.OnInteract();
    }
}