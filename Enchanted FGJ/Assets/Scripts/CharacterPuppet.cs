using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterPuppet : MonoBehaviour
{
	public Character character;
	public Transform puppetTransform;
	public Renderer puppetRenderer;
	public Renderer puppetOverlay;
	public Renderer puppetUnderlay;

	public void UpdatePuppet(float horizontal, float layer)
	{
		//new Vector3(puppetTransform.position.y, puppetTransform.position.y, puppetTransform.position.x);

		puppetTransform.position = new Vector3(horizontal, 0, layer);
		puppetRenderer.enabled = character.puppetOn;
		UpdateLayers();
	}


	void UpdateLayers()
	{
		if (!character.mirrored)
		{
			if (character.portraits[character.currentExpression].overlayL)
			{
				puppetOverlay.enabled = true;
				puppetOverlay.material.mainTexture = character.portraits[character.currentExpression].overlayL;
			}
			else
			{
				puppetOverlay.enabled = false;
			}
			if (character.portraits[character.currentExpression].underlayL)
			{
				puppetUnderlay.enabled = true;
				puppetUnderlay.material.mainTexture = character.portraits[character.currentExpression].underlayL;
			}
			else
			{
				puppetUnderlay.enabled = false;
			}
		}
		else
		{
			if (character.portraits[character.currentExpression].overlayR)
			{
				puppetOverlay.enabled = true;
				puppetOverlay.material.mainTexture = character.portraits[character.currentExpression].overlayR;
			}
			else
			{
				puppetOverlay.enabled = false;	
			}
			if (character.portraits[character.currentExpression].underlayR)
			{
				puppetUnderlay.enabled = true;
				puppetUnderlay.material.mainTexture = character.portraits[character.currentExpression].underlayR;
			}
			else
			{
				puppetUnderlay.enabled = false;	
			}
		}
	}

}


