using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryPassage : MonoBehaviour
{
	[TextArea] public string[] lines;
	public Option option1;
	public Option option2;
}


[System.Serializable]
public class Option
{
	[TextArea] public string text;
	public StoryPassage goTo;
}