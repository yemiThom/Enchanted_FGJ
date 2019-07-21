using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class InventoryItem : MonoBehaviour
{
	//COMPONENT STORAGE
	private Transform thisTransform;
	private Renderer thisRenderer;

	//CHILD COMPONENT STORAGE
	
	//Pentagonal Frame:
	public GameObject itemFrame;
	private Renderer frameRenderer;
	private Transform frameTransform;
	private Vector3 frameDif;
	private float frameScale;
	public float frameShrunk;


	//Background Ring:
	public GameObject itemRing;
	private Renderer ringRenderer;
	private Transform ringTransform;
	private Vector3 ringDif;
	private float ringScale;

	
	//Aspect Icons:
	public GameObject[] icons;
	private Renderer[] iconRenderers;
	public GameObject[] iconRings;
	private Renderer[] iconRingRenderers;
	private Transform[] iconTransforms;
	public Color iconFaded;
	public Color iconDefault;
	public Color iconHighlight;
	private Vector3[] iconDifs;
	private float iconScale;
	public float iconScaleHover;

	//public GameObject text;


	//PLAYTESTING:
	public Texture placeholderArt;
	public string placeholderName;



	//CRAFTER STORAGE:
	public Craftable craftable;
	private Crafter crafter;
	private Aspect[] allAspects;
	private Vector2[] slotPositions;
	private Vector2[] itemPositions;



	//UI STATES:
	private Vector2 grabbedAt;
	public float grabHeight;

	[HideInInspector] public int barSlot;
	public float barHeight;

	public float slottedHeight;

	[HideInInspector]public bool slotHovering;

	[HideInInspector]public int aspectSlot;
	[HideInInspector]public int aspectHover;
	

	public float hoveringHeight;

	[HideInInspector] public bool isPreview;
	public float previewHeight;



	//OTHER UI STUFF:
	public float grabRadius;
	private Vector2 mousePoint;


	public enum Location
	{
		Bar, // located in the inventory bar
		Slot // located in a crafting slot
	}

	public Location location;


	public enum State
	{
		Sitting, // not being acted upon, default state based on location
		Scrolled,
		Grabbed,
		Hovering,
		Swapping
	}

	public State state;



	//should REALLY get rid of this:
	[HideInInspector] public Vector3 homePosition;


	void Awake ()
	{

		//set up sprite itself first
		thisTransform = transform;
		thisRenderer = GetComponent<Renderer>();

		//set up icons
		iconTransforms = new Transform[icons.Length];
		iconRingRenderers = new Renderer[icons.Length];
		iconRenderers = new Renderer[icons.Length];
		iconDifs = new Vector3[icons.Length];
		for (int i = 0; i < icons.Length; i++)
		{
			iconTransforms[i] = icons[i].transform;
			iconRingRenderers[i] = iconRings[i].GetComponent<Renderer>();
			iconRings[i].transform.SetParent(iconTransforms[i], true);
			iconDifs[i] = new Vector3(iconTransforms[i].position.x - thisTransform.position.x, iconTransforms[i].position.y - thisTransform.position.y, 0);
			iconRenderers[i] = icons[i].GetComponent<Renderer>();
			iconRenderers[i].enabled = true;
		}
		iconScale = iconTransforms[0].localScale.x;


		// set up frame
		frameTransform = itemFrame.transform;
		frameDif = new Vector3(frameTransform.position.x - thisTransform.position.x, frameTransform.position.y - thisTransform.position.y, 0);
		frameScale = frameTransform.localScale.x;
		frameRenderer = itemFrame.GetComponent<Renderer>();
		frameRenderer.enabled = true;


		//set up background ring
		ringTransform = itemRing.transform;
		ringDif = new Vector3(ringTransform.position.x - thisTransform.position.x, ringTransform.position.y - thisTransform.position.y, 0);
		ringScale = ringTransform.localScale.x;
		ringRenderer = itemRing.GetComponent<Renderer>();
		ringRenderer.enabled = false;

		location = Location.Bar;
		state = State.Sitting;
	}

	public void LinkItem (Craftable setCraftable, Aspect[] setAspects, Crafter setCrafter, bool setPreview = false)
	{
		//store crafter vars
		craftable = setCraftable;
		allAspects = setAspects;
		crafter = setCrafter;
		slotPositions = new Vector2[allAspects.Length];
		itemPositions = new Vector2[allAspects.Length];
		for (int i = 0; i < allAspects.Length; i++)
		{
			slotPositions[i] = crafter.transform.TransformPoint(allAspects[i].slotPosition);
			itemPositions[i] = crafter.transform.TransformPoint(allAspects[i].itemPosition);
		}

		
		if (setPreview)
		{
			isPreview = true;
		}

		crafter.colorShift.AddRenderer(ringRenderer);
		crafter.colorShift.AddRenderer(frameRenderer);
		InitializeVisuals();
		SnapVisuals();
	}


	public void InitializeVisuals()
	{

		//update icons
		for (int i = 0; i < iconRenderers.Length; i++)
		{
			iconRenderers[i].material.mainTexture = allAspects[i].sigils[craftable.aspectKey[i]].icon;
		}

		//update image
		if (craftable.image)
		{
			thisRenderer.material.mainTexture = craftable.image;
		}
		else
		{
			thisRenderer.material.mainTexture = placeholderArt;
		}

		//toggle bonded frame indicator
		//bondedFrame.enabled = craftable.bonded;
		

		//itemText = craftable.name;

		//text.GetComponent<TextMeshPro>().SetText(itemText);
	}



	public void Update()
	{
		mousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		
		State currentState = state;
		Location currentLocation = location;

		// Mouse in range for a scrollover?
		if(Vector2.Distance(thisTransform.position, mousePoint) < grabRadius) // YES:
		{
			//Make sure: We're not holding something (or this), and we're not already scrolled over.
			if (!crafter.grabbing && state != State.Scrolled)
			{
				state = State.Scrolled; //okay we scrolled over just now
			}
		}
		else // NO:
		{
			//Make sure: we were scrolled over before, we're not holding anything
			if (!crafter.grabbing && state == State.Scrolled)
			{
				state = State.Sitting; //okay we scrolled out
			}
		}
		

		//Are we being grabbed?		
		if (state != State.Grabbed && state != State.Hovering && state != State.Swapping) // Obviously not if we're already grabbed.
		{
			//If this is scrolled over,
			if (state == State.Scrolled)
			{
				//and we're not grabbing anything else and this isn't the preview
				if (Input.GetKeyDown("mouse 0") && !crafter.grabbing && !isPreview)
				{
					//we've been grabbed
					grabbedAt = new Vector2(thisTransform.position.x, thisTransform.position.y) - mousePoint;
					state = State.Grabbed;
					crafter.grabbing = true;
					crafter.grabbedItem = this;
				}

				//also while we're scrolled over, and not being grabbed, are we being manually removed from a slot?
				if (Input.GetKeyDown("mouse 1") && !crafter.grabbing)
				{
					if (!isPreview)
					{
						if (location == Location.Slot)
						{
							crafter.AddToInventoryBar(0);
							barSlot = 0;
							location = Location.Bar;
							crafter.UpdateBarPosition(this);
							crafter.ClearSlot(aspectSlot);
						}
					}
					else
					{
						crafter.FinalizeCraftSlots();
					}
				}
			}
		}
		else //this is grabbed
		{
			if (Input.GetKey("mouse 0")) //and it's going to be grabbed for as long as they're holding down
			{
				//update position before calculating distance

				MovingVisuals();

				bool slotCheck = false;
				for (int i = 0; i < allAspects.Length; i++)
				{
					if (Vector2.Distance(thisTransform.position, slotPositions[i]) < crafter.slotRadius + grabRadius)
					{
						aspectHover = i;
						if (location != Location.Slot || aspectSlot != aspectHover)
						{
							state = State.Hovering;
							slotCheck = true;
							break;
						}
						
					}
				}

				if (!slotCheck)
				{
					state = State.Grabbed;
				}
			
			}
			else // oops they let go
			{
				crafter.grabbing = false;			
				if (state == State.Hovering)
				{
					if (location == Location.Bar)
					{
						crafter.FillSlot(aspectHover, craftable);
						aspectSlot = aspectHover;
						crafter.RemoveFromInventoryBar(barSlot);
						location = Location.Slot;
						barSlot = -1;
					}
					if (location == Location.Slot)
					{
						crafter.ClearSlot(aspectSlot);
						crafter.FillSlot(aspectHover, craftable);
						aspectSlot = aspectHover;
					}
				}
				state = State.Sitting;	
				thisTransform.position = homePosition;
			}
		}

		if (state != currentState || location != currentLocation)
		{
			SnapVisuals();
		}
	}


	void MovingVisuals()
	{
		// CURRENTLY BEING DRAGGED AROUND THE SCENE
		thisTransform.position = new Vector3(mousePoint.x + grabbedAt.x, mousePoint.y + grabbedAt.y, grabHeight);
		
		// MANUALLY LOCK RELEVANT CHILDREN DEPENDING ON STATE/LOCATION:
		if (state == State.Grabbed)
		{
			if (location == Location.Slot)
			{
				//iconTransforms[aspectSlot].position = new Vector3(itemPositions[aspectSlot].x, itemPositions[aspectSlot].y, iconTransforms[aspectSlot].position.z);
			}
		}

		if (state == State.Hovering)
		{
			frameTransform.position = new Vector3(slotPositions[aspectHover].x + frameDif.x, slotPositions[aspectHover].y + frameDif.y, frameTransform.position.z);
			ringTransform.position = new Vector3(slotPositions[aspectHover].x + ringDif.x, slotPositions[aspectHover].y + ringDif.y, ringTransform.position.z);
			PositionIcons(ringTransform, true);

			if (location == Location.Bar)
			{
			
			}

			if (location == Location.Slot)
			{
				
			}
		}
	}



	void SnapVisuals()
	{
		//THIS (SPRITE)
		//FRAME
		//ICONS
		//RING

		if (!isPreview)
		{
			if (location == Location.Bar)
			{

				if (state == State.Sitting) // SITTING IN INVENTORY
				{
					//thisTransform.position = crafter.UpdateBarPosition(barSlot);

					//FRAME - visible, locked to sprite, smaller
					frameRenderer.enabled = true;
					frameTransform.position = new Vector3(thisTransform.position.x + frameDif.x, thisTransform.position.y + frameDif.y, frameTransform.position.z);
					frameTransform.localScale = new Vector3(frameScale * frameShrunk, frameScale * frameShrunk, 1);
					
					//ICONS - lock to sprite, faded opacity, default scale (locked to Frame)
					ScaleIcons(1f);
					ColorIcons(iconFaded);
					EnableIcons(true);
					PositionIcons(thisTransform, true, frameShrunk);

					//RING - not visible
					ringRenderer.enabled = false;

				}
				if (state == State.Scrolled) // SCROLLED OVER IN THE INVENTORY
				{
					
					//thisTransform.position = crafter.UpdateBarPosition(barSlot);

					//FRAME - visible, normal scale, locked to sprite
					frameRenderer.enabled = true;
					frameTransform.position = new Vector3(thisTransform.position.x + frameDif.x, thisTransform.position.y + frameDif.y, frameTransform.position.z);
					frameTransform.localScale = new Vector3(frameScale, frameScale, 1);
					
					//ICONS - scale with frame, default color, locked to sprite
					ScaleIcons(1f);
					ColorIcons(iconDefault);
					EnableIcons(true);
					PositionIcons(thisTransform, true);

					//RING - not visible
					ringRenderer.enabled = false;

				}
				if (state == State.Grabbed) // CURRENTLY BEING DRAGGED AROUND THE SCENE
				{
					//FRAME - same as bar-scrolled
					frameRenderer.enabled = true;
					frameTransform.position = new Vector3(thisTransform.position.x + frameDif.x, thisTransform.position.y + frameDif.y, frameTransform.position.z);
					frameTransform.localScale = new Vector3(frameScale, frameScale, 1);

					//ICONS - same as bar-scrolled
					ScaleIcons(1f);
					ColorIcons(iconDefault);
					EnableIcons(true);
					PositionIcons(thisTransform, true);


					//RING - visible, locked to sprite
					ringRenderer.enabled = true;
					ringTransform.position = new Vector3(thisTransform.position.x + ringDif.x, thisTransform.position.y + ringDif.y, ringTransform.position.z);
				}
				if (state == State.Hovering) // BEING DRAGGED AROUND, HOVERING OVER A SLOT
				{
					//FRAME - Visible, normal scale, locked to slot position
					frameRenderer.enabled = true;
					frameTransform.localScale = new Vector3(frameScale, frameScale, 1);

					//ICONS - all visible, locked to slot position with frame, relevant icon bright and large, irrelevant icons faded
					ScaleIcons(1f, aspectHover);
					EnableIcons(true);
					ColorIcons(iconFaded, aspectHover);
					iconRenderers[aspectHover].material.SetColor("_Color", iconHighlight);
					iconTransforms[aspectHover].localScale = new Vector3(iconScale*iconScaleHover,iconScale*iconScaleHover,1);

					//RING - visible and locked to slot position
					ringRenderer.enabled = true;
				}
			}

			if (location == Location.Slot)
			{
				if (state == State.Sitting)
				{

					//BRIGHT SIGIL

					//THIS (SPRITE) - set to item position in crafting circle
					thisTransform.transform.position = new Vector3(itemPositions[aspectSlot].x, itemPositions[aspectSlot].y, slottedHeight); // item image snaps to item position
					
					//FRAME - not visible, shrunk scale and locked to item sprite for icon positioning
					frameRenderer.enabled = false;
					frameTransform.localScale = new Vector3(frameScale*frameShrunk, frameScale*frameShrunk, 1);
					frameTransform.position = new Vector3(thisTransform.position.x + frameDif.x, thisTransform.position.y + frameDif.y, frameTransform.position.z);

					//ICONS - all invisible
					EnableIcons(false);
					

					//RING - visible, locked into slot as sigil background
					ringRenderer.enabled = true;
					ringTransform.position = new Vector3(slotPositions[aspectSlot].x, slotPositions[aspectSlot].y, ringTransform.position.z);
				}

				if (state == State.Scrolled)
				{

					//BRIGHT SIGIL


					//THIS (SPRITE) - set to item position in circle
					thisTransform.transform.position = new Vector3(itemPositions[aspectSlot].x, itemPositions[aspectSlot].y, slottedHeight); // item image snaps to item position
					
					//FRAME - visible, shrunk size, locked to sprite
					frameRenderer.enabled = true;
					frameTransform.position = new Vector3(thisTransform.position.x + frameDif.x, thisTransform.position.y + frameDif.y, frameTransform.position.z);
					frameTransform.localScale = new Vector3(frameScale, frameScale, 1);
					
					//ICONS - visible, default size, faded opacity, default opacity for relevant, 
					EnableIcons(true, aspectSlot);
					iconRenderers[aspectSlot].enabled = false;
					ColorIcons(iconDefault, aspectSlot);
					PositionIcons(thisTransform, true);
					ScaleIcons(1f);

					//RING - visible, locked into slot as sigil background
					ringRenderer.enabled = true;
					ringTransform.position = new Vector3(slotPositions[aspectSlot].x, slotPositions[aspectSlot].y, ringTransform.position.z);
				}
				if (state == State.Grabbed)
				{

					//DIM SIGIL

					//FRAME - visible, normal size, locked to sprite
					frameRenderer.enabled = true;
					frameTransform.position = new Vector3(thisTransform.position.x + frameDif.x, thisTransform.position.y + frameDif.y, frameTransform.position.z);
					frameTransform.localScale = new Vector3(frameScale, frameScale, 1);

					//ICONS - irrelevant: default scale, default color, locked to sprite
					ScaleIcons(1f);
					ColorIcons(iconDefault);
					EnableIcons(true);
					PositionIcons(thisTransform, true);

					//RING - visible, locked to sprite
					ringTransform.position = new Vector3(thisTransform.position.x + ringDif.x, thisTransform.position.y + ringDif.y, ringTransform.position.z);
					ringRenderer.enabled = true;
				}
				if (state == State.Hovering)
				{

					//DIM SIGIL

					//FRAME - Visible, normal scale, locked to slot position
					frameRenderer.enabled = true;
					frameTransform.localScale = new Vector3(frameScale, frameScale, 1);

					//ICONS - all visible, locked to slot position with frame, hover icon bright and large, home slot icon bright and large and locked to home item slot, irrelevant icons faded
					EnableIcons(true);
					ScaleIcons(1f, aspectHover);
					ColorIcons(iconFaded, aspectHover);
					iconRenderers[aspectHover].material.SetColor("_Color", iconHighlight);
					iconTransforms[aspectHover].localScale = new Vector3(iconScale*iconScaleHover,iconScale*iconScaleHover,1);

					//RING - visible and locked to slot position
					ringRenderer.enabled = true;
				}
			}
		}
	}


	public void PositionIcons(Transform target, bool useDifs, float scaleDifs = 1, int skip = -1)
	{
		for (int j = 0; j < icons.Length; j++)
		{	
			if (j != skip)
			{
				if (useDifs)
				{
					iconTransforms[j].position = new Vector3(target.position.x + (iconDifs[j].x * scaleDifs), target.position.y + (iconDifs[j].y * scaleDifs), iconTransforms[j].position.z);
				}
				else
				{
					iconTransforms[j].position = new Vector3(target.position.x, target.position.y, iconTransforms[j].position.z);
				}
			}
		}
	}

	public void EnableIcons(bool setting, int skip = -1)
	{

		for (int p = 0; p < icons.Length; p++)
		{
			if (p != skip)
			{
				iconRenderers[p].enabled = setting;
			}
		}
	}

	public void ColorIcons(Color target, int skip = -1)
	{
		for (int p = 0; p < icons.Length; p++)
		{
			if (p != skip)
			{
				iconRenderers[p].material.SetColor("_Color", target);
			}
		}
	}

	public void ScaleIcons(float target, int skip = -1)
	{
		for (int p = 0; p < icons.Length; p++)
		{
			if (p != skip)
			{
				iconTransforms[p].localScale = new Vector3(iconScale*target,iconScale*target,1);
			}
		}
	}


}
