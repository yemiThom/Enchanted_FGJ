using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorShift : MonoBehaviour
{
	public Renderer[] targets;
	private Color[] targetStarts;
	public Color[] rainbow;
	public float stepTime = 1;
	private float nextStep;
	private int currentColor = 0;
	private int nextColor = 1;
	public bool cameraToo;


	void Awake()
	{
		if (GameObject.FindWithTag("RainbowManager"))
		{
			if (GameObject.FindWithTag("RainbowManager") != this.gameObject)
			{
				ColorShift rainbowManager = GameObject.FindWithTag("RainbowManager").GetComponent<ColorShift>();
				for (int i = 0; i < targets.Length; i++)
				{
					rainbowManager.AddRenderer(targets[i]);
				}
				Destroy(gameObject);
			}
		}
		else
		{
			nextColor = currentColor + 1;
			nextStep = stepTime + Time.time;
			targetStarts = new Color[targets.Length];
			for (int i = 0; i < targets.Length; i++)
			{
				targetStarts[i] = targets[i].material.GetColor("_Color");
			}
		}
	}


	public void AddRenderer(Renderer newRenderer)
	{
		Renderer[] newTargets = new Renderer[targets.Length + 1];
		Color[] newTargetStarts = new Color[targets.Length + 1];

		newTargets[0] = newRenderer;
		newTargetStarts[0] = newRenderer.material.GetColor("_Color");
		
		for (int i = 1; i < newTargets.Length; i++)
		{
			newTargetStarts[i] = targetStarts[i-1];
			newTargets[i] = targets[i-1];
		}
		targets = newTargets;
		targetStarts = newTargetStarts;
	}

    void Update()
    {
    	
    	if (Time.time > nextStep) //time to switch to the next step in the rainbow
    	{
    		//some serious lag or too-short of steps going on here:
    		if (Time.time - nextStep > stepTime)
    		{
    			Debug.LogError("Clipped through the rainbow.");
    		}

    		nextStep += stepTime;

    		currentColor += 1;
    		nextColor += 1;
    		if (currentColor == rainbow.Length) //make sure loop behaves properly
    		{
    			currentColor = 0;
    		}
    		if (nextColor == rainbow.Length)
    		{
    			nextColor = 0;
    		}
    	}

    	float percent;
		percent = (nextStep - Time.time) / stepTime;
		
		if (percent < 0)
		{
			percent = 0;
			Debug.LogError("Stepped under rainbow.");
		}
		if (percent > 1)
		{
			percent = 1;
			Debug.LogError("Stepped over rainbow.");
		}


    	Color rainbowTarget = Color.Lerp(rainbow[nextColor], rainbow[currentColor], percent);
		
    	
		float rHue, rSat, rVal;
	    Color.RGBToHSV(rainbowTarget, out rHue, out rSat, out rVal);
		
		for (int i = 0; i < targets.Length; i++)
		{
			if (targets[i] != null)
			{
				float tHue, tSat, tVal;
		        Color.RGBToHSV(targetStarts[i], out tHue, out tSat, out tVal);

				Color newColor = Color.HSVToRGB(rHue, (rSat+tSat)/2, tVal);
				newColor.a = targetStarts[i].a;
				targets[i].material.SetColor("_Color", newColor);
			}
		}

		/*
		for (int i = 0; i < targets.Length; i++)
		{
			if (targets[i] != null)
			{
				float average = (targetStarts[i].r + targetStarts[i].b + targetStarts[i].g)/3;
				Color newColor = Color.Lerp(targetStarts[i], rainbowTarget, average);

				newColor.a = targetStarts[i].a;
		    	targets[i].material.SetColor("_Color", newColor);
		    }
		}
		*/


    }
}
