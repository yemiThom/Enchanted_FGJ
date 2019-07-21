using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryManager : MonoBehaviour
{

	public Character jinx;
	public GameObject jinxColorShifter;
	private ColorShift rainbowManager;

	public Character profKaspar;
	public Character profSagar;
	public Character quin;


	public Text mainBox;
	public bool option1On;
	public Button option1Button;
	public Text optionBox1;
	public bool option2On;
	public Button option2Button;
	public Text optionBox2;
	public StoryPassage storyStart;


	public GameObject emptyPuppet;


	public float talkingDepth;
	public float listeningDepth;


	public GameObject textBoxLeft;
	public GameObject textBoxRight;
	public GameObject textBoxInner;
	public GameObject textBoxPopOver;
	
	public StoryPassage currentPassage;
	public int currentLine;
	public StoryPassage prevPassage;
	public bool readingLines;


	public void LoadPassage (StoryPassage goTo)
	{
		currentPassage = goTo;
		option1Button.enabled = false;
		option2Button.enabled = false;
		optionBox1.text = "";
		optionBox2.text = "";
		readingLines = true;
		currentLine = 0;
		mainBox.text = goTo.lines[0];
		if (goTo.option1.goTo)
		{
			option1On = true;
		}
		else
		{
			option1On = false;
			
		}
		if (goTo.option2.goTo)
		{
			option2On = true;
		}
		else
		{
			option2On = false;
			
		}
		
	}



	public Renderer GetRenderer (GameObject getFrom)
	{
		return getFrom.GetComponent<Renderer>();
	}

	// Start is called before the first frame update
	void Awake()
	{
		if (jinx.isMain)
		{
			CreatePuppet(jinx);
			jinx.puppetOn = true;
			jinx.puppet.UpdatePuppet(jinx.position , talkingDepth);


			//Create Rainbow UI effect
			rainbowManager = Instantiate(jinxColorShifter).GetComponent<ColorShift>();
			DontDestroyOnLoad(rainbowManager.gameObject);
			GameObject[] allUIFrames = GameObject.FindGameObjectsWithTag("UIFrame");
			for (int i = 0; i < allUIFrames.Length; i++)
			{
				rainbowManager.AddRenderer(GetRenderer(allUIFrames[i]));
			}

			rainbowManager.AddRenderer(jinx.puppet.puppetUnderlay);

		}
		LoadPassage(storyStart);

	}

		void Start()
		{
			option1Button.onClick.AddListener(OptionOne);
			option2Button.onClick.AddListener(OptionTwo);
		}

	// Update is called once per frame
	void Update()
	{
		if (Input.GetKeyDown("mouse 0") && currentPassage && readingLines)
		{
			if(currentPassage.lines.Length-1 < currentLine)
			{
				currentLine += 1;
				mainBox.text = currentPassage.lines[currentLine];
			}
			else
			{
				mainBox.text = "";
				readingLines = false;
				if (option1On)
				{option1Button.enabled = true;
				optionBox1.text = currentPassage.option1.text;}
				if (option2On)
				{option2Button.enabled = true;
				optionBox2.text = currentPassage.option2.text;}
			}
		}
	}


	public void OptionOne()
	{
		if (option1On)
		{
		LoadPassage(currentPassage.option1.goTo);
	}
	}

	public void OptionTwo()
	{
		if (option2On)
		{
		LoadPassage(currentPassage.option2.goTo);
	}
	}


	public void CreatePuppet(Character forWhom)
	{
		CharacterPuppet newPuppet = Instantiate(emptyPuppet).GetComponent<CharacterPuppet>();
		newPuppet.character = forWhom;
		forWhom.puppet = newPuppet;
	}

}





[System.Serializable]
public class Character
{
	public enum CastName
	{
		Jinx,
		Kaspar,
		Sagar,
		Quin
	}

	public CastName castName;

	public bool isMain = false;
	//[HideInInspector]
	public bool inScene;
	public bool mirrored;
	//[HideInInspector]
	public bool isTalking;
	//[HideInInspector]
	public int currentExpression;
	//[HideInInspector]
	public bool puppetOn;

	[Range(-600, 600)]
	public float position;

	public Expression[] portraits;

	public CharacterPuppet puppet;
}

[System.Serializable]
public class Expression
{
	public string name;
	public Texture portraitR;
	public Texture overlayR;
	public Texture underlayR;

	public Texture portraitL;
	public Texture overlayL;
	public Texture underlayL;
}